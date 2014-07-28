namespace SmtpProxy
{
    public class MessageHandlerResponse
    {
        public MessageHandlerResponse(int code, string message)
        {
            Message = message;
            Code = code;
        }

        public int Code { get; private set; }

        public string Message { get; private set; }
    }
}