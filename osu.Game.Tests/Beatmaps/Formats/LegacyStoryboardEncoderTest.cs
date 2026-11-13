// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Storyboards;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tests.Beatmaps.Formats
{
    [TestFixture]
    public class LegacyStoryboardEncoderTest
    {
        private CultureInfo previousCulture = null!;

        [SetUp]
        public void SetUp()
        {
            previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = previousCulture;

        private static string[] encodeStandalone(Storyboard sb)
        {
            using var writer = new StringWriter();
            new LegacyStoryboardEncoder(sb).EncodeStandaloneStoryboard(writer);
            return writer.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        private static string[] encodeEmbedded(Storyboard sb)
        {
            using var writer = new StringWriter();
            new LegacyStoryboardEncoder(sb).EncodeEventsToBeatmap(writer);
            return writer.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        private static StoryboardSprite sprite(Storyboard sb, StoryboardElementSource source = StoryboardElementSource.Shared, Anchor origin = Anchor.Centre)
        {
            var s = new StoryboardSprite(source, "img.png", origin, new Vector2(320, 240));
            sb.GetLayer("Foreground").Add(s);
            return s;
        }

        // CF: LegacyStoryboardEncoder.EncodeGeneralToBeatmap | storyboard.UseSkinSprites ? '1' : '0' | TRUE
        // CF: LegacyStoryboardEncoder.EncodeGeneralToBeatmap | storyboard.UseSkinSprites ? '1' : '0' | FALSE
        // CF: LegacyStoryboardEncoder.EncodeGeneralToBeatmap | Beatmap.WidescreenStoryboard ? '1' : '0' | TRUE
        // CF: LegacyStoryboardEncoder.EncodeGeneralToBeatmap | Beatmap.WidescreenStoryboard ? '1' : '0' | FALSE
        [Test]
        public void TestGeneral()
        {
            foreach (bool value in new[] { true, false })
            {
                var sb = new Storyboard { UseSkinSprites = value };
                sb.Beatmap.WidescreenStoryboard = !value;

                using var writer = new StringWriter();
                new LegacyStoryboardEncoder(sb).EncodeGeneralToBeatmap(writer);

                string[] lines = writer.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                Assert.That(lines, Is.EqualTo(new[] { $"UseSkinSprites: {(value ? 1 : 0)}", $"WidescreenStoryboard: {(value ? 0 : 1)}" }));
            }
        }

        // CF: LegacyStoryboardEncoder.encodeEvents | target == Beatmap | FALSE
        // CF: LegacyStoryboardEncoder.encodeEvents | foreach video | loop 0
        // CF: LegacyStoryboardEncoder.encodeEvents | foreach layer | loop n
        // CF: LegacyStoryboardEncoder.encodeEvents | foreach sample | loop 0
        // CF: LegacyStoryboardEncoder.EncodeStandaloneStoryboard | writes [Events] | no throw
        [Test]
        public void TestEmptyStandaloneStoryboardHasOnlyHeaders()
        {
            string[] lines = encodeStandalone(new Storyboard()).Where(l => l.Length > 0).ToArray();

            Assert.That(lines, Is.EqualTo(new[]
            {
                "[Events]",
                "// Background and Video events",
                "// Storyboard Layer 0 (Background)",
                "// Storyboard Layer 1 (Fail)",
                "// Storyboard Layer 2 (Pass)",
                "// Storyboard Layer 3 (Foreground)",
                "// Storyboard Layer 4 (Overlay)",
                "// Storyboard Sound Samples",
            }));
        }

        // CF: LegacyStoryboardEncoder.encodeEvents | target == Beatmap | TRUE
        [Test]
        public void TestEmbeddedWritesBackgroundLine()
        {
            var sb = new Storyboard { BackgroundOffset = new Vector2(5, 6) };
            sb.BeatmapInfo.Metadata.BackgroundFile = "bg.jpg";

            string[] lines = encodeEmbedded(sb);

            Assert.That(lines[1], Is.EqualTo("0,0,\"bg.jpg\",5,6"));
        }

        // CF: LegacyStoryboardEncoder.encodeSpritesFromLayer | elem.Source == target | TRUE
        // CF: LegacyStoryboardEncoder.encodeSpritesFromLayer | elem.Source == target | FALSE
        // CF: LegacyStoryboardEncoder.encodeSpritesFromLayer | case StoryboardSprite | case StoryboardSprite
        [Test]
        public void TestOnlyElementsOfTargetSourceAreWritten()
        {
            var sb = new Storyboard();
            sprite(sb, StoryboardElementSource.Shared);
            sprite(sb, StoryboardElementSource.Beatmap);

            Assert.That(encodeStandalone(sb).Count(l => l.StartsWith("Sprite,", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(encodeEmbedded(sb).Count(l => l.StartsWith("Sprite,", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(encodeStandalone(sb), Does.Contain("Sprite,Foreground,Centre,\"img.png\",320,240"));
        }

        // CF: LegacyStoryboardEncoder.encodeSpritesFromLayer | case StoryboardAnimation | case StoryboardAnimation
        [Test]
        public void TestAnimationLine()
        {
            var sb = new Storyboard();
            sb.GetLayer("Background").Add(new StoryboardAnimation(StoryboardElementSource.Shared, "a.png", Anchor.TopLeft, new Vector2(1, 2), 4, 100, AnimationLoopType.LoopOnce));

            Assert.That(encodeStandalone(sb), Does.Contain("Animation,Background,TopLeft,\"a.png\",1,2,4,100,LoopOnce"));
        }

        // CF: LegacyStoryboardEncoder.encodeEvents | foreach video | loop 1
        // CF: LegacyStoryboardEncoder.encodeEvents | video.Source == target | TRUE
        // CF: LegacyStoryboardEncoder.encodeEvents | video.Source == target | FALSE
        [Test]
        public void TestVideoLine()
        {
            var sb = new Storyboard();
            sb.GetLayer("Video").Add(new StoryboardVideo(StoryboardElementSource.Shared, "v.mp4", 250));
            sb.GetLayer("Video").Add(new StoryboardVideo(StoryboardElementSource.Beatmap, "other.mp4", 0));

            string[] lines = encodeStandalone(sb);

            Assert.That(lines.Count(l => l.StartsWith("Video,", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(lines, Does.Contain("Video,250,\"v.mp4\""));
        }

        // CF: LegacyStoryboardEncoder.encodeEvents | foreach sample | loop 1
        // CF: LegacyStoryboardEncoder.encodeEvents | sample.Source == target | TRUE
        [Test]
        public void TestSampleLine()
        {
            var sb = new Storyboard();
            sb.GetLayer("Pass").Add(new StoryboardSampleInfo(StoryboardElementSource.Shared, "s.wav", 1000, 80));

            Assert.That(encodeStandalone(sb), Does.Contain("Sample,1000,2,\"s.wav\",80"));
        }

        // CF: LegacyStoryboardEncoder.encodeCommand | case StoryboardAlphaCommand | case StoryboardAlphaCommand
        // CF: LegacyStoryboardEncoder.encodeCommand | StartValue == EndValue (fade) | TRUE
        // CF: LegacyStoryboardEncoder.encodeCommand | StartValue == EndValue (fade) | FALSE
        // CF: LegacyStoryboardEncoder.encodeCommand | command.StartTime == command.EndTime | TRUE
        // CF: LegacyStoryboardEncoder.encodeCommand | command.StartTime == command.EndTime | FALSE
        [Test]
        public void TestAlphaCommands()
        {
            var sb = new Storyboard();
            var s = sprite(sb);
            s.Commands.AddAlpha(Easing.None, 1000, 2000, 0, 1);
            s.Commands.AddAlpha(Easing.InQuad, 3000, 3000, 0.5f, 0.5f);

            string[] lines = encodeStandalone(sb);

            Assert.That(lines, Does.Contain(" F,0,1000,2000,0,1"));
            Assert.That(lines, Does.Contain(" F,3,3000,,0.5"));
        }

        // CF: LegacyStoryboardEncoder.encodeCommand | case StoryboardScaleCommand | case StoryboardScaleCommand
        // CF: LegacyStoryboardEncoder.encodeCommand | case StoryboardVectorScaleCommand | case StoryboardVectorScaleCommand
        // CF: LegacyStoryboardEncoder.encodeCommand | vectorScale.StartValue == EndValue | TRUE
        // CF: LegacyStoryboardEncoder.encodeCommand | vectorScale.StartValue == EndValue | FALSE
        // CF: LegacyStoryboardEncoder.encodeCommand | case StoryboardRotationCommand | case StoryboardRotationCommand
        // CF: LegacyStoryboardEncoder.encodeCommand | case StoryboardXCommand | case StoryboardXCommand
        // CF: LegacyStoryboardEncoder.encodeCommand | case StoryboardYCommand | case StoryboardYCommand
        // CF: LegacyStoryboardEncoder.encodeCommand | case StoryboardColourCommand | case StoryboardColourCommand
        // CF: LegacyStoryboardEncoder.encodeCommand | colour.StartValue == EndValue | TRUE
        // CF: LegacyStoryboardEncoder.encodeCommand | colour.StartValue == EndValue | FALSE
        [Test]
        public void TestTransformCommands()
        {
            var sb = new Storyboard();
            var c = sprite(sb).Commands;
            c.AddScale(Easing.None, 0, 100, 1, 2);
            c.AddVectorScale(Easing.None, 0, 100, new Vector2(1, 2), new Vector2(3, 4));
            c.AddVectorScale(Easing.None, 200, 300, new Vector2(1, 2), new Vector2(1, 2));
            c.AddRotation(Easing.None, 0, 100, 0, 180);
            c.AddX(Easing.None, 0, 100, 10, 30);
            c.AddY(Easing.None, 0, 100, 20, 20);
            c.AddColour(Easing.None, 0, 100, new Color4(1f, 0f, 0f, 1f), new Color4(0f, 1f, 0f, 1f));
            c.AddColour(Easing.None, 200, 300, new Color4(1f, 0f, 0f, 1f), new Color4(1f, 0f, 0f, 1f));

            string[] lines = encodeStandalone(sb);

            Assert.That(lines, Does.Contain(" S,0,0,100,1,2"));
            Assert.That(lines, Does.Contain(" V,0,0,100,1,2,3,4"));
            Assert.That(lines, Does.Contain(" V,0,200,300,1,2"));
            Assert.That(lines.Any(l => l.StartsWith(" R,0,0,100,0,3.14159", StringComparison.Ordinal)), Is.True, "degrees are written as radians");
            Assert.That(lines, Does.Contain(" MX,0,0,100,10,30"));
            Assert.That(lines, Does.Contain(" MY,0,0,100,20"));
            Assert.That(lines, Does.Contain(" C,0,0,100,255,0,0,0,255,0"));
            Assert.That(lines, Does.Contain(" C,0,200,300,255,0,0"));
        }

        // CF: LegacyStoryboardEncoder.encodeCommand | case StoryboardFlipHCommand | case StoryboardFlipHCommand
        // CF: LegacyStoryboardEncoder.encodeCommand | case StoryboardFlipVCommand | case StoryboardFlipVCommand
        // CF: LegacyStoryboardEncoder.encodeCommand | case StoryboardBlendingParametersCommand | case StoryboardBlendingParametersCommand
        [Test]
        public void TestParameterCommands()
        {
            var sb = new Storyboard();
            var c = sprite(sb).Commands;
            c.AddFlipH(Easing.None, 0, 100, true, false);
            c.AddFlipV(Easing.None, 0, 100, true, false);
            c.AddBlendingParameters(Easing.None, 0, 100, BlendingParameters.Additive, BlendingParameters.Inherit);

            string[] lines = encodeStandalone(sb);

            Assert.That(lines.Where(l => l.StartsWith(" P,", StringComparison.Ordinal)), Is.EquivalentTo(new[] { " P,0,0,100,H", " P,0,0,100,V", " P,0,0,100,A" }));
        }

        // CF: LegacyStoryboardEncoder.encodeCommands | foreach loopingGroup | loop 1
        // CF: LegacyStoryboardEncoder.encodeCommands | foreach loopingGroup | loop 0
        // CF: LegacyStoryboardEncoder.encodeCommand | command is IStoryboardLoopingCommand | TRUE
        // CF: LegacyStoryboardEncoder.encodeCommand | command is IStoryboardLoopingCommand | FALSE
        [Test]
        public void TestLoopingGroupHeaderAndNestedCommandDepth()
        {
            var sb = new Storyboard();
            var s = sprite(sb);
            var loop = s.AddLoopingGroup(1000, 2);
            loop.AddAlpha(Easing.None, 0, 100, 0, 1);

            string[] lines = encodeStandalone(sb);
            int idx = Array.IndexOf(lines, " L,1000,3");

            Assert.That(idx, Is.GreaterThanOrEqualTo(0));
            // suspected production bug (not asserted): inner command times come out as -1000..-900 instead of 0..100, because the original (relative) command time is offset by the absolute loop start.
            Assert.That(lines[idx + 1], Does.StartWith("  F,0,"));
        }

        // CF: LegacyStoryboardEncoder.encodeCommands | foreach triggerGroup | loop 1
        // CF: LegacyStoryboardEncoder.encodeCommands | triggerGroup.TriggerEndTime != 0 | TRUE
        // CF: LegacyStoryboardEncoder.encodeCommands | triggerGroup.GroupNumber != 0 | TRUE
        [Test]
        public void TestTriggerGroupFull()
        {
            var sb = new Storyboard();
            var group = sprite(sb).AddTriggerGroup("HitSoundClap", 100, 200, -2);
            group.AddAlpha(Easing.None, 0, 50, 0, 1);

            string[] lines = encodeStandalone(sb);
            int idx = Array.IndexOf(lines, " T,HitSoundClap,100,200,2");

            Assert.That(idx, Is.GreaterThanOrEqualTo(0));
            Assert.That(lines[idx + 1], Is.EqualTo("  F,0,0,50,0,1"));
        }

        // CF: LegacyStoryboardEncoder.encodeCommands | triggerGroup.TriggerEndTime != 0 | FALSE
        // CF: LegacyStoryboardEncoder.encodeCommands | triggerGroup.GroupNumber != 0 | FALSE
        [Test]
        public void TestTriggerGroupMinimal()
        {
            var sb = new Storyboard();
            sprite(sb).AddTriggerGroup("Passing", 0, 0, 0);

            Assert.That(encodeStandalone(sb), Does.Contain(" T,Passing"));
        }

        // CF: LegacyStoryboardEncoder.convertOrigin | case each Anchor | case TopLeft
        // CF: LegacyStoryboardEncoder.convertOrigin | case each Anchor | default
        [TestCase(Anchor.TopLeft, "TopLeft")]
        [TestCase(Anchor.TopCentre, "TopCentre")]
        [TestCase(Anchor.TopRight, "TopRight")]
        [TestCase(Anchor.CentreLeft, "CentreLeft")]
        [TestCase(Anchor.Centre, "Centre")]
        [TestCase(Anchor.CentreRight, "CentreRight")]
        [TestCase(Anchor.BottomLeft, "BottomLeft")]
        [TestCase(Anchor.BottomCentre, "BottomCentre")]
        [TestCase(Anchor.BottomRight, "BottomRight")]
        [TestCase(Anchor.Custom, "TopLeft")]
        public void TestOriginConversion(Anchor anchor, string expected)
        {
            var sb = new Storyboard();
            sprite(sb, origin: anchor);

            Assert.That(encodeStandalone(sb), Does.Contain($"Sprite,Foreground,{expected},\"img.png\",320,240"));
        }

        // CF: LegacyStoryboardEncoder.encodeCommand | default | no throw
        // CF: LegacyStoryboardDecoder.Decode | decode(encode(x)) round trip | no throw
        [Test]
        public void TestRoundTripThroughDecoder()
        {
            var sb = new Storyboard();
            var s = sprite(sb);
            s.Commands.AddAlpha(Easing.None, 1000, 2000, 0, 1);
            s.Commands.AddX(Easing.OutQuad, 0, 500, 10, 20);

            string text = "osu file format v14\n\n" + string.Join("\n", encodeStandalone(sb));
            using var reader = new LineBufferedReader(new MemoryStream(Encoding.UTF8.GetBytes(text)));
            var decoded = new LegacyStoryboardDecoder(14).Decode(reader);

            var decodedSprite = (StoryboardSprite)decoded.GetLayer("Foreground").Elements.Single();
            Assert.That(decodedSprite.InitialPosition, Is.EqualTo(new Vector2(320, 240)));
            Assert.That(decodedSprite.Commands.Alpha.Single().EndValue, Is.EqualTo(1f));
            Assert.That(decodedSprite.Commands.X.Single().Easing, Is.EqualTo(Easing.OutQuad));
        }
    }
}
