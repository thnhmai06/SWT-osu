// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Storyboards;

namespace osu.Game.Tests.Beatmaps.Formats
{
    [TestFixture]
    public class DecoderTest
    {
        private static LineBufferedReader reader(string content) => new LineBufferedReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));

        // CF: Decoder.GetDecoder | ArgumentNullException.ThrowIfNull(stream) | throws ArgumentNullException
        [Test]
        public void TestNullStream()
        {
            Assert.That(() => Decoder.GetDecoder<Beatmap>(null!), Throws.InstanceOf<ArgumentNullException>());
        }

        // CF: Decoder.GetDecoder | !decoders.TryGetValue(typeof(T), out typedDecoders) | TRUE
        [Test]
        public void TestUnknownDecoderType()
        {
            using (var r = reader("osu file format v14"))
                Assert.That(() => Decoder.GetDecoder<DecoderTest>(r), Throws.InstanceOf<IOException>().With.Message.Contain("Unknown decoder type"));
        }

        // CF: Decoder.GetDecoder | line == null | TRUE
        // CF: Decoder.GetDecoder | while (line != null && line.Length == 0) | loop 0
        [TestCase("")]
        [TestCase("\n\n   \n")]
        public void TestNoContent(string content)
        {
            using (var r = reader(content))
                Assert.That(() => Decoder.GetDecoder<Beatmap>(r), Throws.InstanceOf<IOException>().With.Message.Contain("no content"));
        }

        // CF: Decoder.GetDecoder | line == null | FALSE
        // CF: Decoder.GetDecoder | while (line != null && line.Length == 0) | loop n
        // CF: Decoder.GetDecoder | decoder != null | TRUE
        [Test]
        public void TestLeadingBlankLinesSkippedAndMagicNotConsumed()
        {
            using (var r = reader("\n\n  osu file format v14\n[General]"))
            {
                Assert.That(Decoder.GetDecoder<Storyboard>(r), Is.InstanceOf<LegacyStoryboardDecoder>());
                Assert.That(r.PeekLine()!.Trim(), Is.EqualTo("osu file format v14"));
            }
        }

        // CF: Decoder.GetDecoder | decoder != null | FALSE
        // CF: Decoder.GetDecoder | !fallback_decoders.TryGetValue(typeof(T), out fallbackDecoder) | FALSE
        // CF: Decoder.GetDecoder | !fallback_decoders.TryGetValue(typeof(T), out fallbackDecoder) | infeasible: every registered type also registers a fallback decoder
        [Test]
        public void TestUnknownMagicUsesFallback()
        {
            using (var r = reader("something unrecognised"))
                Assert.That(Decoder.GetDecoder<Storyboard>(r), Is.InstanceOf<Decoder<Storyboard>>());
        }

        // CF: Decoder.GetDecoder | line.StartsWith(d.Key) | TRUE
        [Test]
        public void TestJsonMagic()
        {
            using (var r = reader("{\"a\":1}"))
                Assert.That(Decoder.GetDecoder<Beatmap>(r), Is.InstanceOf<JsonBeatmapDecoder>());
        }

        // CF: Decoder.RegisterDependencies | rulesets ?? throw | throws ArgumentNullException
        [Test]
        public void TestRegisterDependenciesNull()
        {
            Assert.That(() => Decoder.RegisterDependencies(null!), Throws.InstanceOf<ArgumentNullException>());
        }

        // CF: Decoder.Decode | stream == primaryStream | TRUE
        // CF: Decoder.Decode | stream == primaryStream | FALSE
        // CF: Decoder.Decode | foreach stream | loop n
        [Test]
        public void TestDecodeFlagsPrimaryStream()
        {
            var decoder = new RecordingDecoder();
            var primary = reader("a");
            var other = reader("b");

            var output = decoder.Decode(primary, other);

            Assert.That((string)output, Is.EqualTo("a:True,b:False"));
        }

        private class RecordingDecoder : Decoder<RecordingOutput>
        {
            protected override void ParseStreamInto(LineBufferedReader stream, bool isPrimaryStream, RecordingOutput output)
                => output.Parts.Add($"{stream.ReadLine()}:{isPrimaryStream}");
        }

        private class RecordingOutput
        {
            public readonly System.Collections.Generic.List<string> Parts = new System.Collections.Generic.List<string>();

            public static implicit operator string(RecordingOutput o) => string.Join(",", o.Parts);
        }
    }
}
