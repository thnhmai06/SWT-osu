// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;

namespace osu.Game.Tests.Online.Chat
{
    [TestFixture]
    public class ChannelTest
    {
        private Channel channel = null!;

        [SetUp]
        public void SetUp() => channel = new Channel { Name = "#test" };

        private static Message msg(long id, int sender = 1, string? uuid = null) => new Message(id) { Sender = new APIUser { Id = sender }, Uuid = uuid ?? $"m{id}", Timestamp = DateTimeOffset.UnixEpoch.AddSeconds(id) };

        // CF: Channel.userIds | foreach id | loop n
        [Test]
        public void TestJsonUsersPopulated()
        {
            var c = JsonConvert.DeserializeObject<Channel>("{\"users\":[3,4],\"name\":\"pm\"}")!;

            Assert.That(c.Users.Select(u => u.Id), Is.EqualTo(new[] { 3, 4 }));
        }

        // CF: Channel.userIds | foreach id | loop 0
        [Test]
        public void TestJsonNoUsers() => Assert.That(JsonConvert.DeserializeObject<Channel>("{\"users\":[]}")!.Users, Is.Empty);

        // CF: Channel.ctor(APIUser) | PM channel created | no throw
        [Test]
        public void TestPrivateMessageConstructor()
        {
            var c = new Channel(new APIUser { Id = 2, Username = "bob" });

            Assert.That(c.Type, Is.EqualTo(ChannelType.PM));
            Assert.That(c.Name, Is.EqualTo("bob"));
            Assert.That(c.Users.Single().Id, Is.EqualTo(2));
            Assert.That(c.ToString(), Is.EqualTo("bob"));
        }

        // CF: Channel.AddLocalEcho | NewMessagesArrived?.Invoke | non-null
        [Test]
        public void TestAddLocalEchoRaisesEvent()
        {
            var raised = new List<Message>();
            channel.NewMessagesArrived += m => raised.AddRange(m);
            var echo = new LocalEchoMessage();

            channel.AddLocalEcho(echo);

            Assert.That(channel.Messages, Does.Contain(echo));
            Assert.That(raised, Is.EqualTo(new[] { echo }));
        }

        // CF: Channel.AddLocalEcho | NewMessagesArrived?.Invoke | null
        [Test]
        public void TestAddLocalEchoWithoutSubscribers() => Assert.DoesNotThrow(() => channel.AddLocalEcho(new LocalEchoMessage()));

        // CF: Channel.AddNewMessages | foreach messages | loop n
        // CF: Channel.AddNewMessages | localEcho != null | FALSE
        // CF: Channel.AddNewMessages | messages.Length == 0 | FALSE
        // CF: Channel.AddNewMessages | LastMessageId == null | TRUE
        // CF: Channel.AddNewMessages | NewMessagesArrived?.Invoke | non-null
        [Test]
        public void TestAddNewMessagesSetsLastMessageId()
        {
            int raised = 0;
            channel.NewMessagesArrived += m => raised += m.Count();

            channel.AddNewMessages(msg(1), msg(3), msg(2));

            Assert.That(channel.Messages.Select(m => m.Id), Is.EqualTo(new long?[] { 1, 2, 3 }));
            Assert.That(channel.LastMessageId, Is.EqualTo(3));
            Assert.That(raised, Is.EqualTo(3));
        }

        // CF: Channel.AddNewMessages | LastMessageId == null | FALSE
        // CF: Channel.AddNewMessages | maxMessageId > LastMessageId | TRUE
        [Test]
        public void TestAddNewMessagesAdvancesLastMessageId()
        {
            channel.LastMessageId = 2;
            channel.AddNewMessages(msg(5));

            Assert.That(channel.LastMessageId, Is.EqualTo(5));
        }

        // CF: Channel.AddNewMessages | maxMessageId > LastMessageId | FALSE
        [Test]
        public void TestAddNewMessagesKeepsHigherLastMessageId()
        {
            channel.LastMessageId = 10;
            channel.AddNewMessages(msg(5));

            Assert.That(channel.LastMessageId, Is.EqualTo(10));
        }

        // CF: Channel.AddNewMessages | messages.Length == 0 | TRUE
        // CF: Channel.AddNewMessages | foreach messages | loop 0
        [Test]
        public void TestAddNewMessagesDuplicatesIgnored()
        {
            int raised = 0;
            channel.AddNewMessages(msg(1));
            channel.NewMessagesArrived += _ => raised++;

            channel.AddNewMessages(msg(1));
            channel.AddNewMessages();

            Assert.That(channel.Messages, Has.Count.EqualTo(1));
            Assert.That(raised, Is.EqualTo(0));
        }

        // CF: Channel.AddNewMessages | NewMessagesArrived?.Invoke | null
        [Test]
        public void TestAddNewMessagesWithoutSubscribers() => Assert.DoesNotThrow(() => channel.AddNewMessages(msg(1)));

        // CF: Channel.AddNewMessages | localEcho != null | TRUE
        // CF: Channel.ReplaceMessage | final == null | FALSE
        // CF: Channel.ReplaceMessage | final.Id > LastMessageId | TRUE
        // CF: Channel.ReplaceMessage | PendingMessageResolved?.Invoke | non-null
        [Test]
        public void TestAddNewMessagesResolvesLocalEcho()
        {
            var echo = new LocalEchoMessage { Uuid = "u" };
            (LocalEchoMessage, Message)? resolved = null;
            channel.PendingMessageResolved += (e, f) => resolved = (e, f);
            channel.LastMessageId = 3;
            channel.AddLocalEcho(echo);

            var final = msg(7, uuid: "u");
            channel.AddNewMessages(final);

            Assert.That(resolved, Is.EqualTo((echo, final)));
            Assert.That(channel.Messages, Is.EqualTo(new[] { final }));
            Assert.That(channel.LastMessageId, Is.EqualTo(7));
        }

        // CF: Channel.ReplaceMessage | !pendingMessages.Remove(echo) | TRUE
        // CF: Channel.ReplaceMessage | echo not pending | throws InvalidOperationException
        [Test]
        public void TestReplaceUnknownEchoThrows() => Assert.Throws<InvalidOperationException>(() => channel.ReplaceMessage(new LocalEchoMessage(), msg(1)));

        // CF: Channel.ReplaceMessage | !pendingMessages.Remove(echo) | FALSE
        // CF: Channel.ReplaceMessage | final == null | TRUE
        // CF: Channel.MessageRemoved | MessageRemoved?.Invoke | non-null
        [Test]
        public void TestReplaceWithNullRemovesEcho()
        {
            var echo = new LocalEchoMessage();
            Message? removed = null;
            channel.MessageRemoved += m => removed = m;
            channel.AddLocalEcho(echo);

            channel.ReplaceMessage(echo, null!);

            Assert.That(removed, Is.SameAs(echo));
            Assert.That(channel.Messages, Is.Empty);
        }

        // CF: Channel.ReplaceMessage | final == null | TRUE
        // CF: Channel.ReplaceMessage | MessageRemoved?.Invoke | null
        [Test]
        public void TestReplaceWithNullWithoutSubscribers()
        {
            var echo = new LocalEchoMessage();
            channel.AddLocalEcho(echo);

            Assert.DoesNotThrow(() => channel.ReplaceMessage(echo, null!));
        }

        // CF: Channel.ReplaceMessage | Messages.Contains(final) | TRUE
        // CF: Channel.ReplaceMessage | final already present | throws InvalidOperationException
        [Test]
        public void TestReplaceWithExistingMessageThrows()
        {
            var echo = new LocalEchoMessage();
            channel.AddLocalEcho(echo);
            channel.AddNewMessages(msg(1));

            Assert.Throws<InvalidOperationException>(() => channel.ReplaceMessage(echo, msg(1)));
        }

        // CF: Channel.ReplaceMessage | final.Id > LastMessageId | FALSE
        // CF: Channel.ReplaceMessage | PendingMessageResolved?.Invoke | null
        // CF: Channel.ReplaceMessage | Messages.Contains(final) | FALSE
        [Test]
        public void TestReplaceKeepsHigherLastMessageId()
        {
            var echo = new LocalEchoMessage();
            channel.LastMessageId = 9;
            channel.AddLocalEcho(echo);

            channel.ReplaceMessage(echo, msg(3));

            Assert.That(channel.LastMessageId, Is.EqualTo(9));
        }

        // CF: Channel.purgeOldMessages | messageCount > MAX_HISTORY | TRUE
        [Test]
        public void TestOldMessagesPurged()
        {
            channel.AddNewMessages(Enumerable.Range(1, Channel.MAX_HISTORY + 5).Select(i => msg(i)).ToArray());

            Assert.That(channel.Messages, Has.Count.EqualTo(Channel.MAX_HISTORY));
            Assert.That(channel.Messages.First().Id, Is.EqualTo(6));
        }

        // CF: Channel.purgeOldMessages | messageCount > MAX_HISTORY | FALSE
        [Test]
        public void TestMessagesAtLimitNotPurged()
        {
            channel.AddNewMessages(Enumerable.Range(1, Channel.MAX_HISTORY).Select(i => msg(i)).ToArray());

            Assert.That(channel.Messages, Has.Count.EqualTo(Channel.MAX_HISTORY));
        }

        // CF: Channel.purgeOldMessages | pending echoes are not counted | TRUE
        [Test]
        public void TestLocalEchoesNotCountedForPurge()
        {
            channel.AddLocalEcho(new LocalEchoMessage());
            channel.AddNewMessages(Enumerable.Range(1, Channel.MAX_HISTORY).Select(i => msg(i)).ToArray());

            Assert.That(channel.Messages, Has.Count.EqualTo(Channel.MAX_HISTORY + 1));
        }

        // CF: Channel.RemoveMessagesFromUser | message.SenderId == userId | TRUE
        // CF: Channel.RemoveMessagesFromUser | message.SenderId == userId | FALSE
        // CF: Channel.RemoveMessagesFromUser | for messages | loop n
        // CF: Channel.RemoveMessagesFromUser | MessageRemoved?.Invoke | non-null
        [Test]
        public void TestRemoveMessagesFromUser()
        {
            var removed = new List<long?>();
            channel.MessageRemoved += m => removed.Add(m.Id);
            channel.AddNewMessages(msg(1, 5), msg(2, 5), msg(3, 6), msg(4, 5));

            channel.RemoveMessagesFromUser(5);

            Assert.That(channel.Messages.Select(m => m.Id), Is.EqualTo(new long?[] { 3 }));
            Assert.That(removed, Is.EqualTo(new long?[] { 1, 2, 4 }));
        }

        // CF: Channel.RemoveMessagesFromUser | for messages | loop 0
        // CF: Channel.RemoveMessagesFromUser | MessageRemoved?.Invoke | null
        [Test]
        public void TestRemoveMessagesFromUserEmptyChannel() => Assert.DoesNotThrow(() => channel.RemoveMessagesFromUser(1));

        // CF: Channel.RemoveMessagesFromUser | MessageRemoved?.Invoke | null
        [Test]
        public void TestRemoveMessagesWithoutSubscribers()
        {
            channel.AddNewMessages(msg(1, 5));
            channel.RemoveMessagesFromUser(5);

            Assert.That(channel.Messages, Is.Empty);
        }

        // CF: Channel.UnreadMessages | LastReadId < m.Id | TRUE
        // CF: Channel.UnreadMessages | LastReadId < m.Id | FALSE
        [Test]
        public void TestUnreadMessages()
        {
            channel.AddNewMessages(msg(1), msg(2), msg(3));
            channel.LastReadId = 2;

            Assert.That(channel.UnreadMessages.Select(m => m.Id), Is.EqualTo(new long?[] { 3 }));
        }
    }
}
