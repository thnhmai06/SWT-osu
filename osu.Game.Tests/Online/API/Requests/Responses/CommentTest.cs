// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Tests.Online.API.Requests.Responses
{
    [TestFixture]
    public class CommentTest
    {
        // CF: Comment.IsTopLevel | !ParentId.HasValue | TRUE
        [Test]
        public void TestTopLevel() => Assert.That(new Comment().IsTopLevel, Is.True);

        // CF: Comment.IsTopLevel | !ParentId.HasValue | FALSE
        [Test]
        public void TestReplyIsNotTopLevel() => Assert.That(new Comment { ParentId = 1 }.IsTopLevel, Is.False);

        // CF: Comment.IsDeleted | DeletedAt.HasValue | TRUE
        [Test]
        public void TestDeleted() => Assert.That(new Comment { DeletedAt = DateTimeOffset.UnixEpoch }.IsDeleted, Is.True);

        // CF: Comment.IsDeleted | DeletedAt.HasValue | FALSE
        [Test]
        public void TestNotDeleted() => Assert.That(new Comment().IsDeleted, Is.False);

        // CF: Comment.HasMessage | !string.IsNullOrEmpty(Message) | TRUE
        [Test]
        public void TestHasMessage() => Assert.That(new Comment { Message = "hi" }.HasMessage, Is.True);

        // CF: Comment.HasMessage | !string.IsNullOrEmpty(Message) | FALSE
        [Test]
        public void TestHasNoMessage()
        {
            Assert.That(new Comment { Message = string.Empty }.HasMessage, Is.False);
            Assert.That(new Comment { Message = null! }.HasMessage, Is.False);
        }
    }
}
