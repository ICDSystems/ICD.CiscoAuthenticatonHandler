using System;

namespace ICD.CiscoAuthenticationHandler.Utils
{
    public sealed class StringEventArgs : EventArgs
    {
        private readonly string m_Data;

        public string Data { get { return m_Data; } }

        public StringEventArgs(string data)
        {
            m_Data = data;
        }
    }
}