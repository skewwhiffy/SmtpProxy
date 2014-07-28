using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace SmtpProxy.Test
{
    [TestFixture]
    public class MessageHandlerTest
    {
        private const string ClientName = "your.tester";
        private const string HeloMessage = MessageHandler.HeloPrefix + " " + ClientName;
        private const string FromEmailAddress = "from@test.smtp.com";
        private const string MailFromMessage = MessageHandler.MailFromPrefix + "<" + FromEmailAddress + ">";

        private MessageHandler _handler;
        private Queue<MessageHandlerResponse> _messages;
        private List<string> _mailToMessages;

        [SetUp]
        public void BeforeEach()
        {
            _messages = new Queue<MessageHandlerResponse>();
            _handler = new MessageHandler(_messages.Enqueue);
            _mailToMessages = Enumerable
                .Range(0, 10)
                .Select(i => string.Format("{0}<to{1}@test.smtp.com>", MessageHandler.MailToPrefix, i))
                .ToList();
        }

        [Test]
        public void HandlerSendsGreeting()
        {
            var response = GetSingleResponseFromQueue();
            Assert.AreEqual(220, response.Code);
            Assert.AreEqual(MessageHandlerStatus.Open, _handler.Status);
        }

        [Test]
        public void HandlerGreets()
        {
            _messages.Dequeue();
            _handler.Send(HeloMessage);
            var response = GetSingleResponseFromQueue();
            Assert.AreEqual(250, response.Code);
            Assert.IsTrue(response.Message.Contains(ClientName));
            Assert.AreEqual(MessageHandlerStatus.Greeting, _handler.Status);
        }

        [Test]
        public void HandlerOkaysFromAndTo()
        {
            DequeueAndSend(HeloMessage);
            DequeueAndSend(MailFromMessage);
            var response = GetSingleResponseFromQueue();
            Assert.AreEqual(250, response.Code);
            Assert.AreEqual(MessageHandlerStatus.MailFrom, _handler.Status);
            _mailToMessages.ForEach(m => _handler.Send(m));
            Assert.AreEqual(_mailToMessages.Count, _messages.Count);
            Assert.IsTrue(_messages.All(m => m.Code == 250));
            Assert.AreEqual(MessageHandlerStatus.Recipient, _handler.Status);
        }

        [Test]
        public void HandlerReceivesDataOkay()
        {
            DequeueAndSend(HeloMessage);
            DequeueAndSend(MailFromMessage);
            DequeueAndSend(_mailToMessages[0]);
            DequeueAndSend(MessageHandler.DataMarker);
            Assert.AreEqual(MessageHandlerStatus.Data, _handler.Status);
            var response = GetSingleResponseFromQueue();
            Assert.AreEqual(354, response.Code);
        }

        [Test]
        public void HandlerReceivesDataStartingWithCodesOkay()
        {
            DequeueAndSend(HeloMessage);
            DequeueAndSend(MailFromMessage);
            DequeueAndSend(_mailToMessages[0]);
            DequeueAndSend(MessageHandler.DataMarker);
            DequeueAndSend(HeloMessage);
            _handler.Send(MailFromMessage);
            _mailToMessages.ForEach(m => _handler.Send(m));
            _handler.Send(MessageHandler.QuitMarker);
            Assert.IsEmpty(_messages);
        }

        [Test]
        public void HandlerEndsDataAndMessageOkay()
        {
            DequeueAndSend(HeloMessage);
            DequeueAndSend(MailFromMessage);
            DequeueAndSend(_mailToMessages[0]);
            DequeueAndSend(MessageHandler.DataMarker);
            DequeueAndSend(".");
            var response = GetSingleResponseFromQueue();
            Assert.AreEqual(250, response.Code);
            Assert.IsTrue(_handler.Send(MessageHandler.QuitMarker));
            response = GetSingleResponseFromQueue();
            Assert.AreEqual(221, response.Code);
        }

        private void DequeueAndSend(string message)
        {
            _messages.Dequeue();
            _handler.Send(message);
        }

        private MessageHandlerResponse GetSingleResponseFromQueue()
        {
            Assert.AreEqual(1, _messages.Count);
            return _messages.Dequeue();
        }
    }
}