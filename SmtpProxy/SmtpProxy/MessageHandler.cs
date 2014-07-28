using System;

namespace SmtpProxy
{
    public class MessageHandler
    {
        public const string HeloPrefix = "HELO";
        public const string MailFromPrefix = "MAIL FROM:";
        public const string MailToPrefix = "MAIL TO:";
        public const string DataMarker = "DATA";
        public const string QuitMarker = "QUIT";

        private readonly Action<MessageHandlerResponse> _responseAction;

        public MessageHandler(Action<MessageHandlerResponse> responseAction)
        {
            _responseAction = responseAction;
            _responseAction(new MessageHandlerResponse(220, "SMTP Proxy here"));
            Status = MessageHandlerStatus.Open;
        }

        public bool Send(string message)
        {
            if (Status == MessageHandlerStatus.Data)
            {
                if (message == ".")
                {
                    Respond(250, "OK, received", MessageHandlerStatus.EndData);
                }
                return false;
            }
            if (Status == MessageHandlerStatus.EndData && message == "QUIT")
            {
                Respond(221, "Bye", MessageHandlerStatus.Closed);
                return true;
            }
            if (Status == MessageHandlerStatus.Open && message.StartsWith(HeloPrefix))
            {
                string name = message.Substring(HeloPrefix.Length).Trim();
                string greeting = string.Format("Hello {0}, how very nice to meet you", name);
                Respond(250, greeting, MessageHandlerStatus.Greeting);
                return false;
            }
            if (Status == MessageHandlerStatus.Greeting && message.StartsWith(MailFromPrefix))
            {
                Respond(250, "OK", MessageHandlerStatus.MailFrom);
                return false;
            }
            if ((Status == MessageHandlerStatus.MailFrom || Status == MessageHandlerStatus.Recipient)
                && message.StartsWith(MailToPrefix))
            {
                Respond(250, "OK", MessageHandlerStatus.Recipient);
                return false;
            }
            if (Status == MessageHandlerStatus.Recipient && message.StartsWith(DataMarker))
            {
                Respond(354, "End data with /r/n./r/n", MessageHandlerStatus.Data);
                return false;
            }
            Status = MessageHandlerStatus.Closed;
            throw new InvalidOperationException("Unrecognised message");
        }

        private void Respond(int code, string message, MessageHandlerStatus? newStatus = null)
        {
            _responseAction(new MessageHandlerResponse(code, message));
            if (newStatus.HasValue)
            {
                Status = newStatus.Value;
            }
        }

        public MessageHandlerStatus Status { get; private set; }
    }
}