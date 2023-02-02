using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICD.CiscoAuthenticationHandler.AuthOptions;
using ICD.CiscoAuthenticationHandler.Utils;
using Crestron.SimplSharp;
using JetBrains.Annotations;

namespace ICD.CiscoAuthenticationHandler
{

    public delegate void SPlusStringOutput(SimplSharpString data);

    public delegate void SPlusUshortOutput(ushort data);

    public delegate void SPlusBoolOutput(ushort data);

    public delegate void SPlusListStringOutput(ushort index, SimplSharpString data);

    public enum eAuthenticationRequest
    {
        None,
        HostPinOrGuest,
        HostPinOrGuestPin,
        AnyHostPinOrGuestPin,
        PanelistPin,
        PanelistPinOrAttendeePin,
        GuestPin
    }

    [PublicAPI("S+")]
    public sealed class AuthenticationHandler
    {

        private const long PASSWORD_INCORRECT_TIMEOUT = 6 * 1000; // 6 seconds

        private const string PROMPT_PASSWORD_INCORRECT = "Pin Incorrect, Please Try Again";

        private const string PROMPT_SUBMITTING_PASSWORD = "Submitting Pin";

        private const string RESPONSE_START_AUTHENTICATION = "*s Conference Call ";

        private const string RESPONSE_RESPONSE_RESULT_ERROR = "*r CallAuthenticationResponseResult (status=Error)";

        private const string RESPONSE_STATUS_START_FORMAT = "*s Call {0} Status:";

        private const string RESPONSE_GHOST_START_FORMAT = "*s Call {0} (ghost=True)";

        private const string RESPONSE_GHOST_CONFERENCE_START_FORMAT = "*s Conference Call {0} (ghost=True)";
        private const string RESPONSE_STATUS_DISCONNECTING = "Disconnecting";
        private const string RESPONSE_STATUS_IDLE = "Idle";

        #region Static fields

        /// <summary>
        /// Regex to parse the cisco authenticaton request string for callId and request type
        /// </summary>
        private static readonly Regex s_AuthenticationRequestRegex;

        /// <summary>
        /// Regex to parse the cisco call status string for callId and status
        /// </summary>
        private static readonly Regex s_CallStatusRegex;

        #endregion

        /// <summary>
        /// CallId that need authentication
        /// </summary>
        private int m_ActiveCallId;

        /// <summary>
        /// Currently selected auth option
        /// </summary>
        [CanBeNull] private IAuthOption m_AuthOptionSelected;

        /// <summary>
        /// All auth options avaliable currently
        /// May be empty
        /// </summary>
        private readonly List<IAuthOption> m_AuthOptions; 

        /// <summary>
        /// Builds password entered by the user
        /// </summary>
        private readonly StringBuilder m_KeypadText;

        /// <summary>
        /// Wait a period after we send a password, and if no response, assume it was incorrect
        /// </summary>
        private readonly CTimer m_PasswordIncorrectTimeout;

        /// <summary>
        /// This is set to true while waiting for a password check to complete, which disables the keypad and join button
        /// </summary>
        private bool m_PasswordChecking;

        /// <summary>
        /// Breaks up cisco responses into individual responses
        /// </summary>
        private readonly DelimiterSerialBuffer m_Buffer;

        #region S+ Callbacks
        /// <summary>
        /// Data to be sent to the VTC unit
        /// </summary>
        [PublicAPI("S+")]
        public SPlusStringOutput VtcTx { get; set; }

        
        /// <summary>
        /// Set when the call requires authentication
        /// </summary>
        [PublicAPI("S+")]
        public SPlusBoolOutput AuthenticationRequired { get; set; }
        
        
        /// <summary>
        /// Keypad text to display on the UI
        /// </summary>
        [PublicAPI("S+")]
        public SPlusStringOutput KeypadTextFeedback { get; set; }

        /// <summary>
        /// If the keypad has any values in the buffer
        /// Should enable backspace/clear buttons on UI
        /// </summary>
        [PublicAPI("S+")]
        public SPlusBoolOutput KeypadHasValue { get; set; }

        /// <summary>
        /// If the keypad should be enabled on the UI or not
        /// </summary>
        [PublicAPI("S+")]
        public SPlusBoolOutput EnableKeypad { get; set; }

        /// <summary>
        /// If the join button should be enabled on the UI or not
        /// </summary>
        [PublicAPI("S+")]
        public SPlusBoolOutput EnableJoinButton { get; set; }

        /// <summary>
        /// If the auth option list should be shown on the UI or not
        /// Shows if the list count is greater than one
        /// </summary>
        [PublicAPI("S+")]
        public SPlusBoolOutput ShowOptionList { get; set; }

        /// <summary>
        /// How many auth option list items to show
        /// </summary>
        [PublicAPI("S+")]
        public SPlusUshortOutput OptionListCount { get; set; }

        /// <summary>
        /// What name to use for each of the auth option list buttons
        /// 0-based index
        /// </summary>
        [PublicAPI("S+")]
        public SPlusListStringOutput OptionListItem { get; set; }

        /// <summary>
        /// What item to show as selected on the auth option list
        /// 0-based index
        /// </summary>
        [PublicAPI("S+")]
        public SPlusUshortOutput OptionListSelected { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public AuthenticationHandler()
        {
            
            m_AuthOptions = new List<IAuthOption>();
            m_Buffer = new DelimiterSerialBuffer('\x0d');
            m_KeypadText = new StringBuilder();
            m_PasswordIncorrectTimeout = new CTimer(PasswordIncorrectTimeoutCallback, Timeout.Infinite);

            m_Buffer.OnCompletedSerial += BufferOnCompletedSerial;
        }

        /// <summary>
        /// Static Constructor
        /// </summary>
        static AuthenticationHandler()
        {
            s_AuthenticationRequestRegex = new Regex(@"^\*s Conference Call (\d+) AuthenticationRequest:(.*)");
            s_CallStatusRegex = new Regex(@"^\*s Call (\d+) Status:(.+)");
        }

        #endregion


        #region VTC Rx Callbacks
        /// <summary>
        /// Called by the buffer for each complete response from the cisco
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void BufferOnCompletedSerial(object sender, StringEventArgs args)
        {
            string response = args.Data.Trim();

            // Build strings with call id for quicer comparisons
            string callStatusStart = string.Format(RESPONSE_STATUS_START_FORMAT, m_ActiveCallId);
            string callGhostStart = string.Format(RESPONSE_GHOST_START_FORMAT, m_ActiveCallId);
            string callConferenceGhostStart = string.Format(RESPONSE_GHOST_CONFERENCE_START_FORMAT, m_ActiveCallId);

            
            if (response.StartsWith(callGhostStart) || response.StartsWith(callConferenceGhostStart))
            {
                // For some reason, some calls end and report as "ghost" calls, without having a status update event
                CallEnded();
            }
            else if (response.StartsWith(RESPONSE_RESPONSE_RESULT_ERROR))
            {
                // If we get an error result, assume the call ended
                // todo: better handling here of errors that aren't a result of the call ending?
                CallEnded();
            }
            else if (response.StartsWith(callStatusStart))
            {
                // Parse the call status, to check if it's disconected or idle
                ParseCallStatusResponse(response);
            }
            else if (response.StartsWith(RESPONSE_START_AUTHENTICATION))
            {
                // Check for a possible authentication request and handle it
                ParseAuthenticationResponse(response);
            }

            
        }

        /// <summary>
        /// Parse authentication request feedback using regex
        /// to extract the callId and authentication request type.
        /// </summary>
        /// <param name="response"></param>
        private void ParseAuthenticationResponse(string response)
        {
            Match match = s_AuthenticationRequestRegex.Match(response);

            if (!match.Success)
                return;

            int callId = int.Parse(match.Groups[1].Value);

            string autheticationTypeString = match.Groups[2].Value.Trim();

            eAuthenticationRequest authenticationType;

            try
            {
                authenticationType =
                    (eAuthenticationRequest)Enum.Parse(typeof(eAuthenticationRequest), autheticationTypeString, true);
            }
            catch (Exception)
            {
                ErrorLog.Error("Could not parse authentication request type: {0}", autheticationTypeString);
                return;
            }

            m_PasswordChecking = false;

            if (authenticationType == eAuthenticationRequest.None)
                AuthenticationRequestNone();
            else
            {
                m_PasswordIncorrectTimeout.Stop();
                m_ActiveCallId = callId;
                AuthOptionsSetRange(GetAuthOptionsForRequest(authenticationType));
            }
        }

        /// <summary>
        /// Parse the call status feedback using regex
        /// to extract the callId and the status.
        /// Check if the authentication request call is ending.
        /// </summary>
        /// <param name="response"></param>
        private void ParseCallStatusResponse(string response)
        {
            Match match = s_CallStatusRegex.Match(response);

            if (!match.Success)
                return;

            int callId = int.Parse(match.Groups[1].Value);

            string status = match.Groups[2].Value.Trim();

            if (callId != m_ActiveCallId)
                return;

            if (string.Equals(status, RESPONSE_STATUS_DISCONNECTING, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, RESPONSE_STATUS_IDLE, StringComparison.OrdinalIgnoreCase))
            {
                CallEnded();
            }
        }

        #endregion

        #region S+ API

        #region VTC

        /// <summary>
        /// Called by S+ to send received data into the module
        /// </summary>
        /// <param name="data"></param>
        [PublicAPI("S+")]
        public void VtcRx(SimplSharpString data)
        {
            m_Buffer.Enqueue(data.ToString());
        }

        /// <summary>
        /// Send the subscribe string to the VTC
        /// Call this once the VTC is connected
        /// </summary>
        [PublicAPI("S+")]
        public void SendSubscribe()
        {
            VtcTx.SafeRaise("xFeedback Register Status/Conference/Call/AuthenticationRequest\x0d");
        }

        #endregion

        #region Keypad

        /// <summary>
        /// Adds a key to the keypad
        /// 1 = '0', 2 = '1' ... 10 = '*', 11 = '#'
        /// </summary>
        /// <param name="index"></param>
        [PublicAPI("S+")]
        public void KeypadPressKey(ushort index)
        {
            // subtract 1 from index cause S+ is 1-based indexing :(
            index--;

            if (index <= 9)
                m_KeypadText.Append(index);
            else if (index == 10)
                m_KeypadText.Append('*');
            else if (index == 11)
                m_KeypadText.Append('#');
            else
                throw new ArgumentOutOfRangeException("index");

            UpdateKeypadText();
        }

        /// <summary>
        /// Clears the keypad
        /// </summary>
        [PublicAPI("S+")]
        public void KeypadClear()
        {
            ClearKeypadText();
        }

        /// <summary>
        /// Removes the most recent character from the keypad
        /// </summary>
        [PublicAPI("S+")]
        public void KeypadBackspace()
        {
            if (m_KeypadText.Length == 0)
                return;

            m_KeypadText.RemoveLast();
            UpdateKeypadText();
        }

        /// <summary>
        /// Call to send the keypad text to the VTC unit as the code
        /// </summary>
        [PublicAPI("S+")]
        public void KeypadSubmit()
        {
            if (m_AuthOptionSelected == null)
                return;

            string pin = m_KeypadText.ToString();
            m_PasswordChecking = true;
            ClearKeypadText(PROMPT_SUBMITTING_PASSWORD);


            VtcTx.SafeRaise(m_AuthOptionSelected.GetCommand(m_ActiveCallId, pin));
            m_PasswordIncorrectTimeout.Reset(PASSWORD_INCORRECT_TIMEOUT);
        }

        #endregion

        #region AuthOption

        /// <summary>
        /// 0-based indexing
        /// </summary>
        /// <param name="index"></param>
        [PublicAPI("S+")]
        public void SelectOption(ushort index)
        {
            if (index >= m_AuthOptions.Count)
                return;

            var authOption = m_AuthOptions[index];

            m_AuthOptionSelected = authOption;

            OptionListSelected.SafeRaise(index);

            // Clear the code for not required options, update the prompt if no code
            if (authOption.PasswordRequirement == ePasswordRequirement.NotRequired || m_KeypadText.Length == 0)
            {
                ClearKeypadText();
            }

            UpdateEnableStates();
        }

        #endregion

        #endregion

        #region VTC Response Callbacks

        /// <summary>
        /// When the authentication request is None
        /// </summary>
        private void AuthenticationRequestNone()
        {
            m_PasswordIncorrectTimeout.Stop();
            m_PasswordChecking = false;
            AuthOptionsClear();
            m_ActiveCallId = 0;
        }

        /// <summary>
        /// Call when the call referenced in m_ActiveCall goes to disconnecting/disconnected/idle state
        /// </summary>
        private void CallEnded()
        {
            AuthenticationRequestNone();
        }

        private static IEnumerable<IAuthOption> GetAuthOptionsForRequest(eAuthenticationRequest request)
        {
            switch (request)
            {
                case eAuthenticationRequest.None:
                    yield break;
                case eAuthenticationRequest.PanelistPin:
                    yield return PanelistPinAuthOption.GetInstance();
                    break;
                case eAuthenticationRequest.PanelistPinOrAttendeePin:
                    yield return PanelistPinAuthOption.GetInstance();
                    yield return GuestPinAuthOption.GetInstance();
                    break;
                case eAuthenticationRequest.HostPinOrGuest:
                    yield return HostPinAuthOption.GetInstance();
                    yield return GuestAuthOption.GetInstance();
                    break;
                case eAuthenticationRequest.HostPinOrGuestPin:
                    yield return HostPinAuthOption.GetInstance();
                    yield return GuestPinAuthOption.GetInstance();
                    break;
                case eAuthenticationRequest.AnyHostPinOrGuestPin:
                    yield return PinRequiredAuthOption.GetInstance();
                    break;
                case eAuthenticationRequest.GuestPin:
                    yield return GuestPinAuthOption.GetInstance();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("request");

            }
        }

        #endregion

        #region AuthOption List

        private void AuthOptionsClear()
        {
            m_AuthOptions.Clear();
            m_AuthOptionSelected = null;
            m_PasswordChecking = false;
            ClearKeypadText();
            AuthenticationRequired.SafeRaise(false);
        }

        private void AuthOptionsSetRange(IEnumerable<IAuthOption> authOptions)
        {

            m_AuthOptionSelected = null;
            ClearKeypadText();

            // If null, clear
            if (m_AuthOptions == null)
            {
                AuthOptionsClear();
                return;
            }

            List<IAuthOption> authOptionsList = authOptions.ToList();

            // If empty list, clear
            if (authOptionsList.Count == 0)
            {
                AuthOptionsClear();
                return;
            }

            //Update list to S+
            m_AuthOptions.AddRange(authOptionsList);

            for (int i = 0; i < authOptionsList.Count; i++)
            {
                OptionListItem.SafeRaise(i, authOptionsList[i].Name);
            }

            // Select the first option by default, and update to S+
            SelectOption(0);

            OptionListCount.SafeRaise(authOptionsList.Count);

            // Show the list if there's more than the 1 option
            ShowOptionList.SafeRaise(authOptionsList.Count > 1);

            AuthenticationRequired.SafeRaise(true);
        }

        /// <summary>
        /// Update enable states on the keypad/join button
        /// </summary>
        private void UpdateEnableStates()
        {
            EnableKeypad.SafeRaise(!m_PasswordChecking && m_AuthOptionSelected != null &&
                                   m_AuthOptionSelected.PasswordRequirement != ePasswordRequirement.NotRequired);

            EnableJoinButton.SafeRaise(!m_PasswordChecking && m_AuthOptionSelected != null &&
                                       (m_AuthOptionSelected.PasswordRequirement != ePasswordRequirement.Required ||
                                        m_KeypadText.Length > 0));
        }

        #endregion

        #region Keypad Actions

        /// <summary>
        /// Gets the best keypad text
        /// If the keypad builder has a value, returns that
        /// If there is an auth option selected, returns the prompt
        /// Returns null if neither is true
        /// </summary>
        /// <returns></returns>
        [CanBeNull]
        private string GetBestKeypadText()
        {
            if (m_KeypadText.Length > 0)
                return m_KeypadText.ToString();
            if (m_AuthOptionSelected != null)
                return m_AuthOptionSelected.Prompt;
            
            return null;
        }

        /// <summary>
        /// Updates the keypad feedback in S+ with the current value of the stringbuilder
        /// </summary>
        private void UpdateKeypadText()
        {
            UpdateKeypadText(GetBestKeypadText());
        }

        /// <summary>
        /// Updates the keypad feedback in S+ with the current value of the stringbuilder
        /// </summary>
        private void UpdateKeypadText([CanBeNull] string text)
        {
            KeypadTextFeedback.SafeRaise(text);
            KeypadHasValue.SafeRaise(m_KeypadText.Length > 0);
            UpdateEnableStates();
        }

        private void ClearKeypadText()
        {
            m_KeypadText.Clear();
            UpdateKeypadText();
        }

        private void ClearKeypadText([CanBeNull] string prompt)
        {
            m_KeypadText.Clear();
            UpdateKeypadText(prompt);
        }

        #endregion

        #region Password Timer Callback

        /// <summary>
        /// Raised by timer when we thing the entered password is incorrect
        /// </summary>
        /// <param name="userSpecific"></param>
        private void PasswordIncorrectTimeoutCallback(object userSpecific)
        {
            m_PasswordChecking = false;
            ClearKeypadText(PROMPT_PASSWORD_INCORRECT);
        }

        #endregion

    }

}