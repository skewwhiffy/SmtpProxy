using System;

namespace SmtpProxy
{
    public class MessageHandler
    {
        public const string HeloPrefix = "HELO";
        public const string EhloPrefix = "EHLO";
        public const string MailFromPrefix = "MAIL FROM:";
        public const string MailToPrefix = "MAIL TO:";
        public const string DataMarker = "DATA";
        public const string QuitMarker = "QUIT";

        public MessageHandlerResponse Handle(MessageHandlerStatus currentStatus, string message)
        {
            if (currentStatus == MessageHandlerStatus.Open)
            {
                if (message.StartsWith(HeloPrefix) || message.StartsWith(EhloPrefix))
                {
                    string prefix = message.StartsWith(HeloPrefix) ? HeloPrefix : EhloPrefix;
                    string name = message.Substring(prefix.Length).Trim();
                    string greeting = string.Format("Hello {0}, how very nice to meet you", name);
                    return new MessageHandlerResponse(250, greeting, MessageHandlerStatus.Greeting);
                }
            }

            if (currentStatus == MessageHandlerStatus.Greeting)
            {
                if (message.StartsWith(MailFromPrefix))
                {
                    return new MessageHandlerResponse(250, "OK", MessageHandlerStatus.MailFrom);
                }
            }

            if (currentStatus == MessageHandlerStatus.MailFrom || currentStatus == MessageHandlerStatus.Recipient)
            {
                if (message.StartsWith(MailToPrefix))
                {
                    return new MessageHandlerResponse(250, "OK", MessageHandlerStatus.Recipient);
                }
            }

            if (currentStatus == MessageHandlerStatus.Recipient)
            {
                if (message.StartsWith(DataMarker))
                {
                    return new MessageHandlerResponse(354, "End data with /r/n./r/n", MessageHandlerStatus.Data);
                }
            }

            if (currentStatus == MessageHandlerStatus.Data)
            {
                return message == "." ? new MessageHandlerResponse(250, "OK, received", MessageHandlerStatus.EndData) : null;
            }

            if (currentStatus == MessageHandlerStatus.EndData)
            {
                if (message == QuitMarker)
                {
                    return new MessageHandlerResponse(221, "Bye", MessageHandlerStatus.Closed);
                }
            }

            throw new InvalidOperationException("Unrecognised message");
        }
    }
}