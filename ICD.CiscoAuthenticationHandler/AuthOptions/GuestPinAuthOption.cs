using System;

namespace ICD.CiscoAuthenticationHandler.AuthOptions
{
    /// <summary>
    /// Guest with pin auth option singelton
    /// </summary>
    public sealed class GuestPinAuthOption : AbstractAuthOption
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static readonly GuestPinAuthOption s_Instance;

        /// <summary>
        /// If a password/pin is required for this method
        /// </summary>
        public override ePasswordRequirement PasswordRequirement { get { return ePasswordRequirement.Required;} }

        /// <summary>
        /// Name of the method
        /// </summary>
        public override string Name { get { return "Guest"; } }

        /// <summary>
        /// Prompt to display to the user
        /// </summary>
        public override string Prompt { get { return "Enter Guest Pin"; } }

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

            return BuildCommand(callId, "Guest", pin);
        }

        /// <summary>
        /// Private constructor prevents instantiation by other code
        /// </summary>
        private GuestPinAuthOption()
        {
        }

        /// <summary>
        /// Static constructor to instantiate instance
        /// </summary>
        static GuestPinAuthOption()
        {
            s_Instance = new GuestPinAuthOption();
        }

        /// <summary>
        /// Get the instance of the singleton
        /// </summary>
        /// <returns></returns>
        public static GuestPinAuthOption GetInstance()
        {
            return s_Instance;
        }
    }
}