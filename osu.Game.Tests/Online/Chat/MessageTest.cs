// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Game.Online.Chat;

namespace osu.Game.Tests.Online.Chat
{
    [TestFixture]
    public class MessageTest
    {
        // CF: Message.SenderId | Sender?.Id ?? 0 | null
        [Test]
        public void TestSenderIdWithoutSender() => Assert.That(new Message().SenderId, Is.EqualTo(0));

        // CF: Message.SenderId | Sender?.Id ?? 0 | non-null
        // CF: Message.SenderId | set creates sender | no throw
        [Test]
        public void TestSenderIdSetCreatesSender()
        {
            var m = new Message { SenderId = 9 };

            Assert.That(m.Sender.Id, Is.EqualTo(9));
            Assert.That(m.SenderId, Is.EqualTo(9));
        }

        // CF: Message.CompareTo | Id.HasValue && other.Id.HasValue | TRUE
        [Test]
        public void TestCompareByIdWhenBothHaveIds() => Assert.That(new Message(1).CompareTo(new Message(2)), Is.LessThan(0));

        // CF: Message.CompareTo | Id.HasValue && other.Id.HasValue | FALSE
        // CF: Message.CompareTo | timestampComparison != 0 | TRUE
        [Test]
        public void TestCompareByTimestampWithoutIds()
            => Assert.That(new Message(null) { Timestamp = DateTimeOffset.UnixEpoch }.CompareTo(new Message(5) { Timestamp = DateTimeOffset.UnixEpoch.AddSeconds(1) }), Is.LessThan(0));

        // CF: Message.CompareTo | timestampComparison != 0 | FALSE
        [Test]
        public void TestCompareByConstructionOrderWhenTimestampsEqual()
        {
            var first = new Message(null);
            var second = new Message(null);

            Assert.That(first.CompareTo(second), Is.LessThan(0));
            Assert.That(second.CompareTo(first), Is.GreaterThan(0));
        }

        // CF: Message.Equals | other == null | FALSE
        [Test]
        public void TestEqualsNull() => Assert.That(new Message(1).Equals(null), Is.False);

        // CF: Message.Equals | ReferenceEquals(this, other) | TRUE
        [Test]
        public void TestEqualsSelf()
        {
            var m = new Message(null);
            Assert.That(m.Equals(m), Is.True);
        }

        // CF: Message.Equals | Id.HasValue && Id == other.Id | TRUE
        // CF: Message.Equals | Id.HasValue && Id == other.Id | FALSE
        [Test]
        public void TestEqualsById()
        {
            Assert.That(new Message(1).Equals(new Message(1)), Is.True);
            Assert.That(new Message(1).GetHashCode(), Is.EqualTo(new Message(1).GetHashCode()));
            Assert.That(new Message(1).Equals(new Message(2)), Is.False);
        }

        // CF: Message.Equals | Id.HasValue | FALSE
        [Test]
        public void TestEqualsWithoutIdIsFalse() => Assert.That(new Message(null).Equals(new Message(null)), Is.False);

        // CF: Message.ToString | Id?.ToString() ?? "null" | non-null
        // CF: Message.ToString | Id?.ToString() ?? "null" | null
        [Test]
        public void TestToString()
        {
            Assert.That(new Message(4) { Content = "hi" }.ToString(), Does.StartWith("(4) "));
            Assert.That(new Message(null) { Content = "hi" }.ToString(), Does.StartWith("(null) "));
        }

        // CF: Message.ctor | json deserialisation | no throw
        [Test]
        public void TestJsonDeserialisation()
        {
            var m = JsonConvert.DeserializeObject<Message>("{\"channel_id\":3,\"is_action\":true,\"content\":\"c\",\"sender_id\":8,\"uuid\":\"u\"}")!;

            Assert.That(m.ChannelId, Is.EqualTo(3));
            Assert.That(m.IsAction, Is.True);
            Assert.That(m.SenderId, Is.EqualTo(8));
            Assert.That(m.Uuid, Is.EqualTo("u"));
        }
    }
}
