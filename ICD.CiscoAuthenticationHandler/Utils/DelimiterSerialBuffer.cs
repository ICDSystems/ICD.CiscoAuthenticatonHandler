using System;
using System.Collections.Generic;
using System.Text;
using Crestron.SimplSharp;

namespace ICD.CiscoAuthenticationHandler.Utils
{
    public sealed class DelimiterSerialBuffer
    {
        /// <summary>
        /// Raised when a complete message has been buffered.
        /// </summary>
        public event EventHandler<StringEventArgs> OnCompletedSerial;

        private readonly Queue<string> m_Queue;
        private readonly CCriticalSection m_QueueSection;
        private readonly CEvent m_ParsingEvent;
        private bool m_Parsing;

        /// <summary>
        /// Sets while clearing, to tell the parse method to bail out early
        /// </summary>
        private bool m_Clearing;

        private bool Parsing
        {
            get { return m_Parsing; }
            set
            {
                if (m_Parsing == value)
                    return;

                m_Parsing = value;

                if (value)
                    m_ParsingEvent.Reset();
                else
                    m_ParsingEvent.Set();
            }
        }

        #region Methods

        /// <summary>
        /// Enqueues the serial data.
        /// </summary>
        /// <param name="data"></param>
        public void Enqueue(string data)
        {
            m_QueueSection.Execute(() => m_Queue.Enqueue(data));
            Parse();
        }

        /// <summary>
        /// Clears all queued data in the buffer.
        /// </summary>
        public void Clear()
        {
            m_Clearing = true;

            m_ParsingEvent.Wait();

            try
            {
                m_QueueSection.Execute(() => m_Queue.Clear());
                m_RxData.Clear();
            }
            finally
            {
                m_Clearing = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Works through the queued messages to chunk up complete messages.
        /// </summary>
        private void Parse()
        {
            m_QueueSection.Enter();
            try
            {
                if (Parsing)
                    return;
                Parsing = true;
            }
            finally
            {
                m_QueueSection.Leave();
            }

            try
            {
                string data = null;
                while (!m_Clearing && m_QueueSection.Execute(() => m_Queue.Dequeue(out data)))
                    foreach (string serial in Process(data))
                        OnCompletedSerial.Raise(this, serial);
            }
            finally
            {
                m_QueueSection.Execute(() => Parsing = false);
            }
        }

        #endregion

        		private readonly StringBuilder m_RxData;

		private readonly char m_Delimiter;
		private readonly bool m_PassEmptyResponse;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="delimiter"></param>
		public DelimiterSerialBuffer(byte delimiter)
			: this((char)delimiter, false)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="delimiter"></param>
		/// <param name="passEmptyResponse"></param>
		public DelimiterSerialBuffer(byte delimiter, bool passEmptyResponse)
			: this((char)delimiter, passEmptyResponse)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DelimiterSerialBuffer(char delimiter)
			: this(delimiter, false)
		{
		}
		/// <summary>
		/// Constructor.
		/// </summary>
		public DelimiterSerialBuffer(char delimiter, bool passEmptyResponse)
		{
            m_Queue = new Queue<string>();
            m_QueueSection = new CCriticalSection();
            m_ParsingEvent = new CEvent(false, false);

            m_RxData = new StringBuilder();

			m_Delimiter = delimiter;
			m_PassEmptyResponse = passEmptyResponse;

		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Override to process the given item for chunking.
		/// </summary>
		/// <param name="data"></param>
		private IEnumerable<string> Process(string data)
		{
			while (true)
			{
				int index = data.IndexOf(m_Delimiter);

				if (index < 0)
				{
					m_RxData.Append(data);
					break;
				}

				m_RxData.Append(data.Substring(0, index));
				data = data.Substring(index + 1);

				string output = m_RxData.Pop();
				if (m_PassEmptyResponse || !string.IsNullOrEmpty(output))
					yield return output;
			}
		}

		#endregion
    }
}