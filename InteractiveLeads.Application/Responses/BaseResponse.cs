using InteractiveLeads.Application.Responses.Messages;

namespace InteractiveLeads.Application.Responses
{

    /// <summary>
    /// Standard response wrapper for API operations without data payload.
    /// </summary>
    /// <remarks>
    /// Provides factory methods to create success and failure responses with optional messages.
    /// Used to maintain a consistent API response structure across all endpoints.
    /// </remarks>
    public class BaseResponse : IResponse
    {
        /// <summary>
        /// Gets or sets the list of messages associated with the response.
        /// </summary>
        public List<Message> Messages { get; set; } = [];

        public bool HasAnyErrorMessage
        {
            get
            {
                return HasMessageType(MessageType.Error)
                    || HasMessageType(MessageType.Exception)
                    || HasMessageType(MessageType.Fatal);
            }
        }

        /// <summary>
        /// Validates if has any error message
        /// </summary>
        public bool HasErrorMessage { get { return HasMessageType(MessageType.Error); } }

        /// <summary>
        /// Validates if has any exception message
        /// </summary>
        public bool HasExceptionMessage { get { return HasMessageType(MessageType.Exception); } }

        /// <summary>
        /// Validate if has any fatal message
        /// </summary>
        public bool HasFatalMessage { get { return HasMessageType(MessageType.Fatal); } }

        /// <summary>
        /// Validates if has any info message
        /// </summary>
        public bool HasInfoMessage { get { return HasMessageType(MessageType.Info); } }

        /// <summary>
        /// Validates if has any info message
        /// </summary>
        public bool HasSuccessMessage { get { return HasMessageType(MessageType.Success); } }

        /// <summary>
        /// Validate if has any warning message
        /// </summary>
        public bool HasWarningMessage { get { return HasMessageType(MessageType.Warning); } }

        /// <summary>
        /// Initializes a new instance of the ResponseWrapper class.
        /// </summary>
        public BaseResponse()
        {
        }

        public BaseResponse AddErrorMessage(string message, string code = "")
        {
            Messages.Add(new Message() { Text = message, Code = code, Type = MessageType.Error });
            return this;
        }

        public BaseResponse AddSuccessMessage(string message, string code = "")
        {
            Messages.Add(new Message() { Text = message, Code = code, Type = MessageType.Success });
            return this;
        }

        public BaseResponse AddInfoMessage(string message, string code = "")
        {
            Messages.Add(new Message() { Text = message, Code = code, Type = MessageType.Info });
            return this;
        }

        public BaseResponse AddWarningMessage(string message, string code = "")
        {
            Messages.Add(new Message() { Text = message, Code = code, Type = MessageType.Warning });
            return this;
        }

        private bool HasMessageType(MessageType messageType)
        {
            return Messages.Any(message => message.Type == messageType);
        }
    }
}
