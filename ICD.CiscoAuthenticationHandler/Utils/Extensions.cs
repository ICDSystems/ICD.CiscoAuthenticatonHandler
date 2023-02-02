using System;
using System.Collections.Generic;
using System.Text;
using Crestron.SimplSharp;
using JetBrains.Annotations;

namespace ICD.CiscoAuthenticationHandler.Utils
{
    public static class Extensions
    {
        public static void SafeRaise([CanBeNull] this SPlusStringOutput extends, string data)
        {
            if (extends == null)
                return;

            // S+ doesn't like null strings, so convert it to an empty string if it's null
            string safeData = data ?? string.Empty;

            extends(new SimplSharpString(safeData));
        }

        public static void SafeRaise([CanBeNull] this SPlusListStringOutput extends, ushort index, string data)
        {
            if (extends == null)
                return;

            // S+ doesn't like null strings, so convert it to an empty string if it's null
            string safeData = data ?? string.Empty;

            extends(index, new SimplSharpString(safeData));
        }

        public static void SafeRaise([CanBeNull] this SPlusListStringOutput extends, int index, string data)
        {
            extends.SafeRaise((ushort)index, data);
        }

        public static void SafeRaise([CanBeNull] this SPlusUshortOutput extends, ushort data)
        {
            if (extends != null)
                extends(data);
        }

        public static void SafeRaise([CanBeNull] this SPlusUshortOutput extends, int data)
        {
            extends.SafeRaise((ushort)data);
        }

    public static void SafeRaise([CanBeNull] this SPlusBoolOutput extends, bool data)
        {
            if (extends != null)
                extends((ushort)(data ? 1 : 0));
        }

        /// <summary>
        /// Remove last character in the string builder
        /// </summary>
        /// <param name="extends"></param>
        public static void RemoveLast([NotNull] this StringBuilder extends)
        {
            extends.RemoveLast(1);
        }

        /// <summary>
        /// Remove the specificed number of characters from the end of the string builder
        /// </summary>
        /// <param name="extends"></param>
        /// <param name="numberOfCharacters"></param>
        public static void RemoveLast([NotNull] this StringBuilder extends, int numberOfCharacters)
        {
            if (extends == null)
                throw new ArgumentNullException("extends");

            extends.Remove(extends.Length - numberOfCharacters, numberOfCharacters);
        }

        /// <summary>
        /// Enters the critical section, executes the callback and leaves the section.
        /// </summary>
        /// <param name="extends"></param>
        /// <param name="callback"></param>
        public static void Execute([NotNull] this CCriticalSection extends, [NotNull] Action callback)
        {
            if (extends == null)
                throw new ArgumentNullException("extends");
            if (callback == null)
                throw new ArgumentNullException("callback");

            extends.Enter();

            try
            {
                callback();
            }
            finally
            {
                extends.Leave();
            }
        }

        /// <summary>
        /// Enters the critical section, executes the callback and leaves the section.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="extends"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static T Execute<T>([NotNull] this CCriticalSection extends, [NotNull] Func<T> callback)
        {
            if (extends == null)
                throw new ArgumentNullException("extends");
            if (callback == null)
                throw new ArgumentNullException("callback");

            extends.Enter();

            try
            {
                return callback();
            }
            finally
            {
                extends.Leave();
            }
        }

        public static void Raise([CanBeNull] this EventHandler<StringEventArgs> extends, object sender, string data)
        {
            if (extends != null)
                extends(sender, new StringEventArgs(data));
        }

        /// <summary>
        /// Empties the StringBuilder.
        /// </summary>
        /// <param name="extends"></param>
        public static void Clear([NotNull] this StringBuilder extends)
        {
            if (extends == null)
                throw new ArgumentNullException("extends");

            extends.Remove(0, extends.Length);
        }

        /// <summary>
        /// Returns the current string value of the StringBuilder and clears the
        ///	StringBuilder for further use.
        /// </summary>
        /// <param name="extends"></param>
        /// <returns></returns>
        public static string Pop([NotNull] this StringBuilder extends)
        {
            if (extends == null)
                throw new ArgumentNullException("extends");

            string output = extends.ToString();
            extends.Clear();
            return output;
        }

        /// <summary>
        /// Dequeues the next item in the queue. Returns false if the queue is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="extends"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool Dequeue<T>([NotNull] this Queue<T> extends, out T item)
        {
            if (extends == null)
                throw new ArgumentNullException("extends");

            item = default(T);

            if (extends.Count == 0)
                return false;

            item = extends.Dequeue();
            return true;
        }
    }
}