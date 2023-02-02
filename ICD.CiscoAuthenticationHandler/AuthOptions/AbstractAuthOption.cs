using System.Text;

namespace ICD.CiscoAuthenticationHandler.AuthOptions
{
    public abstract class AbstractAuthOption : IAuthOption
    {
        /// <summary>
        /// If a password/pin is required for this method
        /// </summary>
        public abstract ePasswordRequirement PasswordRequirement { get; }

        /// <summary>
        /// Name of the method
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Prompt to display to the user
        /// </summary>
        public abstract string Prompt { get; }

        /// <summary>
        /// Command to get the string to send to the codec
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="pin"></param>
        /// <returns></returns>
        public abstract string GetCommand(int callId, string pin);

        /// <summary>
        /// Common method to build the command with the role and pin
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="role"></param>
        /// <param name="pin"></param>
        /// <returns></returns>
        protected string BuildCommand(int callId, string role, string pin)
        {

            StringBuilder command = new StringBuilder();

            command.AppendFormat("xCommand Conference Call AuthenticationResponse CallId: {0}",callId);

            // Include a role if entered
            if (!string.IsNullOrEmpty(role))
            {
                command.AppendFormat(" ParticipantRole: {0}", role);
            }

            // Include a pin if entered
            if (!string.IsNullOrEmpty(pin))
            {
                //Verify the code ends in #, if not add #
                if (pin[pin.Length - 1] != '#')
                    pin += '#';

                command.AppendFormat(" Pin: {0}", pin);
            }

            return command.ToString();
        }
    }
}