namespace InteractiveLeads.Application.Responses.Messages
{
    /// <summary>
    /// Represents a structured message with text, code, and type.
    /// </summary>
    public sealed class Message
    {
        /// <summary>
        /// The message text.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The message code for internationalization.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// The message type (Error, Success, Info, Warning).
        /// </summary>
        public MessageType Type { get; set; } = MessageType.Info;
    }
}
