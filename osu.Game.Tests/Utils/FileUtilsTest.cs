// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Utils;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class FileUtilsTest
    {
        // CF: FileUtils.AttemptOperation | action succeeds | no throw
        [Test]
        public void TestSuccessfulAction()
        {
            int calls = 0;
            Assert.That(FileUtils.AttemptOperation(() => calls++), Is.True);
            Assert.That(calls, Is.EqualTo(1));
        }

        // CF: FileUtils.AttemptOperation | attempts-- == 0 | FALSE
        // CF: FileUtils.AttemptOperation | action succeeds after retry | no throw
        [Test]
        public void TestRetriesAfterFailure()
        {
            int calls = 0;

            Assert.That(FileUtils.AttemptOperation(() =>
            {
                if (calls++ == 0)
                    throw new InvalidOperationException();
            }, 3), Is.True);

            Assert.That(calls, Is.EqualTo(2));
        }

        // CF: FileUtils.AttemptOperation | attempts-- == 0 | TRUE
        // CF: FileUtils.AttemptOperation | throwOnFailure | TRUE
        [Test]
        public void TestThrowsWhenAttemptsExhausted()
        {
            int calls = 0;
            Assert.Throws<InvalidOperationException>(() => FileUtils.AttemptOperation(() =>
            {
                calls++;
                throw new InvalidOperationException();
            }, 0));
            Assert.That(calls, Is.EqualTo(1));
        }

        // CF: FileUtils.AttemptOperation | throwOnFailure | FALSE
        [Test]
        public void TestReturnsFalseWhenNotThrowing()
        {
            Assert.That(FileUtils.AttemptOperation(() => throw new InvalidOperationException(), 0, false), Is.False);
        }

        // CF: FileUtils.AttemptOperation<T> | action succeeds | no throw
        [Test]
        public void TestGenericSuccessful()
        {
            int state = 0;
            Assert.That(FileUtils.AttemptOperation(s => state = s, 5), Is.True);
            Assert.That(state, Is.EqualTo(5));
        }

        // CF: FileUtils.AttemptOperation<T> | attempts-- == 0 | FALSE
        [Test]
        public void TestGenericRetries()
        {
            int calls = 0;
            Assert.That(FileUtils.AttemptOperation(_ =>
            {
                if (calls++ == 0)
                    throw new InvalidOperationException();
            }, 0, 3), Is.True);
        }

        // CF: FileUtils.AttemptOperation<T> | attempts-- == 0 | TRUE
        // CF: FileUtils.AttemptOperation<T> | throwOnFailure | TRUE
        [Test]
        public void TestGenericThrows()
        {
            Assert.Throws<InvalidOperationException>(() => FileUtils.AttemptOperation<int>(_ => throw new InvalidOperationException(), 0, 0));
        }

        // CF: FileUtils.AttemptOperation<T> | throwOnFailure | FALSE
        [Test]
        public void TestGenericReturnsFalse()
        {
            Assert.That(FileUtils.AttemptOperation<int>(_ => throw new InvalidOperationException(), 0, 0, false), Is.False);
        }
    }
}
