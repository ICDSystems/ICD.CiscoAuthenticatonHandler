namespace ICD.CiscoAuthenticationHandler.AuthOptions
{
    /// <summary>
    /// Guest without pin auth option singleton
    /// </summary>
    public sealed class GuestAuthOption : AbstractAuthOption
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static readonly GuestAuthOption s_Instance;

        /// <summary>
        /// If a password/pin is required for this method
        /// </summary>
        public override ePasswordRequirement PasswordRequirement { get { return ePasswordRequirement.NotRequired; } }

        /// <summary>
        /// Name of the method
        /// </summary>
        public override string Name { get { return "Guest"; } }

        /// <summary>
        /// Prompt to display to the user
        /// </summary>
        public override string Prompt { get { return "No Pin Needed for Guest"; } }

        /// <summary>
        /// Command to get the string to send to the codec
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="pin"></param>
        /// <returns></returns>
        public override string GetCommand(int callId, string pin)
        {
            return BuildCommand(callId, "Guest", null);
        }

        /// <summary>
        /// Private constructor prevents instantiation by other code
        /// </summary>
        private GuestAuthOption()
        {
        }

        /// <summary>
        /// Static constructor to instantiate instance
        /// </summary>
        static GuestAuthOption()
        {
            s_Instance = new GuestAuthOption();
        }

        /// <summary>
        /// Get the instance of the singleton
        /// </summary>
        /// <returns></returns>
        public static GuestAuthOption GetInstance()
        {
            return s_Instance;
        }
}
}