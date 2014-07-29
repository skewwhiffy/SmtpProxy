namespace SmtpProxy
{
    public class MessageHandlerResponse
    {
        public MessageHandlerResponse(
            int code,
            string message,
            MessageHandlerStatus newStatus)
        {
            Message = message;
            NewStatus = newStatus;
            Code = code;
        }

        public int Code { get; private set; }

        public string Message { get; private set; }

        public MessageHandlerStatus NewStatus { get; private set; }
    }
}