using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmtpProxy.Test
{
    [TestFixture]
    public class MessageHandlerTest
    {
        #region Setup

        private const string ClientName = "your.tester";
        private const string HeloMessage = MessageHandler.HeloPrefix + " " + ClientName;
        private const string EhloMessage = MessageHandler.EhloPrefix + " " + ClientName;
        private const string FromEmailAddress = "from@test.smtp.com";
        private const string MailFromMessage = MessageHandler.MailFromPrefix + "<" + FromEmailAddress + ">";

        private MessageHandler _handler;
        private List<string> _mailToMessages;

        private List<MessageHandlerStatus> _allStatuses;

        [TestFixtureSetUp]
        public void BeforeAll()
        {
            _allStatuses = Enum
                .GetValues(typeof(MessageHandlerStatus))
                .Cast<MessageHandlerStatus>()
                .ToList();
        }

        [SetUp]
        public void BeforeEach()
        {
            _handler = new MessageHandler();
            _mailToMessages = Enumerable
                .Range(0, 10)
                .Select(i => string.Format("{0}<to{1}@test.smtp.com>", MessageHandler.MailToPrefix, i))
                .ToList();
        }

        #endregion Setup

        #region Greeting

        [Test]
        [TestCase(HeloMessage)]
        [TestCase(EhloMessage)]
        public void HandlerGreetsFor(string message)
        {
            var response = HandleAndCheck(MessageHandlerStatus.Open, message);
            Assert.AreEqual(250, response.Code);
            Assert.IsTrue(response.Message.Contains(ClientName));
            Assert.AreEqual(MessageHandlerStatus.Greeting, response.NewStatus);
        }

        #endregion Greeting

        #region To and from

        [Test]
        public void HandlerOkaysFrom()
        {
            var response = HandleAndCheck(MessageHandlerStatus.Greeting, MailFromMessage);
            Assert.AreEqual(250, response.Code);
            Assert.AreEqual(MessageHandlerStatus.MailFrom, response.NewStatus);
        }

        [Test]
        [TestCase(MessageHandlerStatus.Recipient)]
        [TestCase(MessageHandlerStatus.MailFrom)]
        public void HandlerOkaysToRecipients(MessageHandlerStatus startStatus)
        {
            _mailToMessages.ForEach(m =>
            {
                var response = _handler.Handle(startStatus, m);
                Assert.AreEqual(250, response.Code);
                Assert.AreEqual(MessageHandlerStatus.Recipient, response.NewStatus);
            });
        }

        [Test]
        public void HandlerThrowsOnRecipientMessageExceptInValidStatuses()
        {
            var statusesToConsider = _allStatuses
                .FindAll(s => s != MessageHandlerStatus.Data)
                .FindAll(s => s != MessageHandlerStatus.Recipient)
                .FindAll(s => s != MessageHandlerStatus.MailFrom);
            _mailToMessages.ForEach(m =>
                statusesToConsider.ForEach(s =>
            {
                try
                {
                    _handler.Handle(s, m);
                    Assert.Fail();
                }
                catch (InvalidOperationException)
                {
                }
            }));
        }

        #endregion To and from

        #region Data

        [Test]
        public void HandlerReceivesDataOkay()
        {
            var response = HandleAndCheck(MessageHandlerStatus.Recipient, MessageHandler.DataMarker);
            Assert.AreEqual(MessageHandlerStatus.Data, response.NewStatus);
            Assert.AreEqual(354, response.Code);
        }

        [Test]
        public void HandlerReceivesDataStartingWithCodesOkay()
        {
            var messages = _mailToMessages;
            messages.AddRange(new[] { HeloMessage, MailFromMessage, MessageHandler.QuitMarker });
            messages.ForEach(m => Assert.IsNull(_handler.Handle(MessageHandlerStatus.Data, m)));
        }

        [Test]
        public void HandlerEndsDataOkay()
        {
            var response = HandleAndCheck(MessageHandlerStatus.Data, ".");
            Assert.AreEqual(250, response.Code);
            Assert.AreEqual(MessageHandlerStatus.EndData, response.NewStatus);
        }

        [Test]
        public void HandlerEndsMessageOkay()
        {
            var response = HandleAndCheck(MessageHandlerStatus.EndData, MessageHandler.QuitMarker);
            Assert.AreEqual(221, response.Code);
            Assert.AreEqual(MessageHandlerStatus.Closed, response.NewStatus);
        }

        #endregion Data

        private MessageHandlerResponse HandleAndCheck(
            MessageHandlerStatus status,
            string message)
        {
            _allStatuses
                .FindAll(s => s != status && s != MessageHandlerStatus.Data)
                .ForEach(s =>
                {
                    try
                    {
                        _handler.Handle(s, message);
                        Assert.Fail("Expected exception when status {0}, message '{1}'", s, message);
                    }
                    catch (InvalidOperationException)
                    {
                    }
                });
            return _handler.Handle(status, message);
        }
    }
}