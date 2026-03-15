namespace InteractiveLeads.Application.Responses.Messages
{
    /// <summary>
    /// Enumeration of message types for categorizing different types of feedback.
    /// </summary>
    public enum MessageType
    {
        /// 
        /// <summary> None, default 
        /// </summary>
        None,

        /// 
        /// <summary> Success Message 
        /// </summary>
        Success,

        /// <summary> 
        /// Informational Message 
        /// </summary>
        Info,

        /// <summary> 
        /// Warning message 
        /// </summary>
        Warning,

        /// <summary> 
        /// Error message  
        /// </summary>
        Error,

        /// <summary> 
        /// Fatal message, it is also considered a System error. 
        /// </summary>
        Fatal,

        /// <summary> 
        /// Exception message, it is also considered a System error. 
        /// </summary>
        Exception,

        /// <summary> 
        /// Validation message  
        /// </summary>
        Validation
    }
}
