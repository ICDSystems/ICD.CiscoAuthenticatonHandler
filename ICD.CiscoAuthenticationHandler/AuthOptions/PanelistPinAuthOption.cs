using System;

namespace ICD.CiscoAuthenticationHandler.AuthOptions
{
    /// <summary>
    /// Panelist with pin auth option singleton
    /// </summary>
    public sealed class PanelistPinAuthOption : AbstractAuthOption
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static readonly PanelistPinAuthOption s_Instance;

        /// <summary>
        /// If a password/pin is required for this method
        /// </summary>
        public override ePasswordRequirement PasswordRequirement { get { return ePasswordRequirement.Required; } }

        /// <summary>
        /// Name of the method
        /// </summary>
        public override string Name { get { return "Panelist"; } }

        /// <summary>
        /// Prompt to display to the user
        /// </summary>
        public override string Prompt { get { return "Enter Panelist Pin"; } }

        /// <summary>
        /// Command to get the string to send to the codec
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="pin"></param>
        /// <returns></returns>
        public override string GetCommand(int callId, string pin)
        {
            if (String.IsNullOrEmpty(pin))
                throw new InvalidOperationException("This method requires a password");

            return BuildCommand(callId, "Panelist", pin);
        }

        /// <summary>
        /// Private constructor prevents instantiation by other code
        /// </summary>
        private PanelistPinAuthOption()
        {
        }

        /// <summary>
        /// Static constructor to instantiate instance
        /// </summary>
        static PanelistPinAuthOption()
        {
            s_Instance = new PanelistPinAuthOption();
        }

        /// <summary>
        /// Get the instance of the singleton
        /// </summary>
        /// <returns></returns>
        public static PanelistPinAuthOption GetInstance()
        {
            return s_Instance;
        }
    }
}