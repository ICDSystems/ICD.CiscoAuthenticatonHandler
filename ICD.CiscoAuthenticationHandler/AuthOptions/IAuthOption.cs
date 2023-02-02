using JetBrains.Annotations;

namespace ICD.CiscoAuthenticationHandler.AuthOptions
{
    public enum ePasswordRequirement
    {
        NotRequired,
        Optional,
        Required
    }

    public interface IAuthOption
    {

        /// <summary>
        /// If a password/pin is required for this method
        /// </summary>
        ePasswordRequirement PasswordRequirement { get; }

        /// <summary>
        /// Name of the method
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Prompt to display to the user
        /// </summary>
        string Prompt { get; }

        /// <summary>
        /// Command to get the string to send to the codec
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="pin"></param>
        /// <returns></returns>
        string GetCommand(int callId, [CanBeNull] string pin);
    }
}