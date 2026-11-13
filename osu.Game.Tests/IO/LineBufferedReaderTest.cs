// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Text;
using NUnit.Framework;
using osu.Game.IO;

namespace osu.Game.Tests.IO
{
    [TestFixture]
    public class LineBufferedReaderTest
    {
        private static LineBufferedReader create(string text) => new LineBufferedReader(new MemoryStream(Encoding.UTF8.GetBytes(text)));

        // CF: LineBufferedReader.PeekLine | peekedLine == null | TRUE
        // CF: LineBufferedReader.PeekLine | peekedLine == null | FALSE
        [Test]
        public void TestPeekIsRepeatableAndDoesNotConsume()
        {
            using var reader = create("a\nb");

            Assert.That(reader.PeekLine(), Is.EqualTo("a"));
            Assert.That(reader.PeekLine(), Is.EqualTo("a"));
            Assert.That(reader.ReadLine(), Is.EqualTo("a"));
            Assert.That(reader.ReadLine(), Is.EqualTo("b"));
        }

        // CF: LineBufferedReader.ReadLine | peekedLine == null | TRUE
        // CF: LineBufferedReader.ReadLine | end of stream | null
        [Test]
        public void TestReadLineWithoutPeekThenEnd()
        {
            using var reader = create("a");

            Assert.That(reader.ReadLine(), Is.EqualTo("a"));
            Assert.That(reader.ReadLine(), Is.Null);
        }

        // CF: LineBufferedReader.ReadToEnd | peekedLine == null | TRUE
        [Test]
        public void TestReadToEndWithoutPeek()
        {
            using var reader = create("a\nb");

            Assert.That(reader.ReadToEnd(), Is.EqualTo("a\nb"));
        }

        // CF: LineBufferedReader.ReadToEnd | peekedLine == null | FALSE
        [Test]
        public void TestReadToEndIncludesPeekedLine()
        {
            using var reader = create("a\nb");
            reader.PeekLine();

            Assert.That(reader.ReadToEnd().Replace("\r\n", "\n"), Is.EqualTo("a\nb"));
        }

        // CF: LineBufferedReader.Dispose | disposes underlying stream | no throw
        [Test]
        public void TestDisposeClosesStream()
        {
            var stream = new MemoryStream();
            var reader = new LineBufferedReader(stream);
            reader.Dispose();

            Assert.That(stream.CanRead, Is.False);
        }
    }
}
