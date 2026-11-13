// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
    public class LegacyStoryboardDecoderTest
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

        private static LineBufferedReader reader(string text) => new LineBufferedReader(new MemoryStream(Encoding.UTF8.GetBytes(text)));

        private static Storyboard decode(string events, int version = 14, string preamble = "")
            => new LegacyStoryboardDecoder(version).Decode(reader($"osu file format v{version}\n\n{preamble}[Events]\n{events}\n"));

        private static StoryboardSprite onlySprite(Storyboard sb, string layer = "Foreground") => (StoryboardSprite)sb.GetLayer(layer).Elements.Single();

        private const string sprite_line = "Sprite,Foreground,Centre,\"img.png\",320,240\n";

        // CF: LegacyStoryboardDecoder.ParseLine | case General | case General
        // CF: LegacyStoryboardDecoder.handleGeneral | case UseSkinSprites | case UseSkinSprites
        // CF: LegacyStoryboardDecoder.handleGeneral | case WidescreenStoryboard | case WidescreenStoryboard
        [Test]
        public void TestGeneralSection()
        {
            var sb = decode("", preamble: "[General]\nUseSkinSprites: 1\nWidescreenStoryboard: 1\nIrrelevant: 5\n\n");

            Assert.That(sb.UseSkinSprites, Is.True);
            Assert.That(sb.Beatmap.WidescreenStoryboard, Is.True);

            var off = decode("", preamble: "[General]\nUseSkinSprites: 0\nWidescreenStoryboard: 0\n\n");
            Assert.That(off.UseSkinSprites, Is.False);
            Assert.That(off.Beatmap.WidescreenStoryboard, Is.False);
        }

        // CF: LegacyStoryboardDecoder.handleEvents | depth == 0 | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | case Sprite | case Sprite
        // CF: LegacyStoryboardDecoder.handleEvents | foreach char c in line | loop n
        // CF: LegacyStoryboardDecoder.parseOrigin | case Centre | case Centre
        [Test]
        public void TestSprite()
        {
            var sprite = onlySprite(decode(sprite_line));

            Assert.That(sprite.Path, Is.EqualTo("img.png"));
            Assert.That(sprite.Origin, Is.EqualTo(Anchor.Centre));
            Assert.That(sprite.InitialPosition, Is.EqualTo(new Vector2(320, 240)));
            Assert.That(sprite.Source, Is.EqualTo(StoryboardElementSource.Beatmap));
        }

        // CF: LegacyStoryboardDecoder.parseOrigin | case TopLeft | case TopLeft
        // CF: LegacyStoryboardDecoder.parseOrigin | case TopCentre | case TopCentre
        // CF: LegacyStoryboardDecoder.parseOrigin | case TopRight | case TopRight
        // CF: LegacyStoryboardDecoder.parseOrigin | case CentreLeft | case CentreLeft
        // CF: LegacyStoryboardDecoder.parseOrigin | case CentreRight | case CentreRight
        // CF: LegacyStoryboardDecoder.parseOrigin | case BottomLeft | case BottomLeft
        // CF: LegacyStoryboardDecoder.parseOrigin | case BottomCentre | case BottomCentre
        // CF: LegacyStoryboardDecoder.parseOrigin | case BottomRight | case BottomRight
        // CF: LegacyStoryboardDecoder.parseOrigin | default | default
        [TestCase("TopLeft", Anchor.TopLeft)]
        [TestCase("TopCentre", Anchor.TopCentre)]
        [TestCase("TopRight", Anchor.TopRight)]
        [TestCase("CentreLeft", Anchor.CentreLeft)]
        [TestCase("Centre", Anchor.Centre)]
        [TestCase("CentreRight", Anchor.CentreRight)]
        [TestCase("BottomLeft", Anchor.BottomLeft)]
        [TestCase("BottomCentre", Anchor.BottomCentre)]
        [TestCase("BottomRight", Anchor.BottomRight)]
        [TestCase("Custom", Anchor.TopLeft)]
        public void TestOrigins(string origin, Anchor expected)
        {
            Assert.That(onlySprite(decode($"Sprite,Foreground,{origin},\"i.png\",0,0")).Origin, Is.EqualTo(expected));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | depth == 0 | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | depth < 2 | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | commandType default | default
        // CF: LegacyStoryboardDecoder.handleEvents | string.IsNullOrEmpty(split[3]) | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | case F | case F
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 5 | TRUE
        [Test]
        public void TestAlphaCommandWithUnderscoreAndSpaceDepth()
        {
            var sprite = onlySprite(decode(sprite_line + " F,3,1000,2000,0,1\n_F,0,3000,,0.5"));

            var first = sprite.Commands.Alpha[0];
            Assert.That((first.Easing, first.StartTime, first.EndTime, first.StartValue, first.EndValue), Is.EqualTo((Easing.InQuad, 1000d, 2000d, 0f, 1f)));

            var second = sprite.Commands.Alpha[1];
            Assert.That((second.StartTime, second.EndTime, second.StartValue, second.EndValue), Is.EqualTo((3000d, 3000d, 0.5f, 0.5f)), "empty end time and missing end value copy the start");
        }

        // CF: LegacyStoryboardDecoder.handleEvents | string.IsNullOrEmpty(split[3]) | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | case S | case S
        // CF: LegacyStoryboardDecoder.handleEvents | case V | case V
        // CF: LegacyStoryboardDecoder.handleEvents | case R | case R
        // CF: LegacyStoryboardDecoder.handleEvents | case M | case M
        // CF: LegacyStoryboardDecoder.handleEvents | case MX | case MX
        // CF: LegacyStoryboardDecoder.handleEvents | case MY | case MY
        // CF: LegacyStoryboardDecoder.handleEvents | case C | case C
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 6 | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 7 | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 8 | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 9 | TRUE
        [Test]
        public void TestTransformCommands()
        {
            var sprite = onlySprite(decode(sprite_line +
                                           " S,0,0,100,1,2\n" +
                                           " V,0,0,100,1,2,3,4\n" +
                                           " R,0,0,100,0,3.14159265358979\n" +
                                           " M,0,0,100,10,20,30,40\n" +
                                           " MX,0,0,100,1,2\n" +
                                           " MY,0,0,100,3,4\n" +
                                           " C,0,0,100,255,0,51,0,255,102"));
            var c = sprite.Commands;

            Assert.That(c.Scale.Single().EndValue, Is.EqualTo(2f));
            Assert.That(c.VectorScale.Single().StartValue, Is.EqualTo(new Vector2(1, 2)));
            Assert.That(c.VectorScale.Single().EndValue, Is.EqualTo(new Vector2(3, 4)));
            Assert.That(c.Rotation.Single().EndValue, Is.EqualTo(180f).Within(1e-3), "radians in the file become degrees");
            Assert.That(c.X.Select(x => (x.StartValue, x.EndValue)), Is.EquivalentTo(new[] { (10f, 30f), (1f, 2f) }));
            Assert.That(c.Y.Select(y => (y.StartValue, y.EndValue)), Is.EquivalentTo(new[] { (20f, 40f), (3f, 4f) }));
            Assert.That(c.Colour.Single().StartValue, Is.EqualTo(new Color4(1, 0, 0.2f, 1)));
            Assert.That(c.Colour.Single().EndValue, Is.EqualTo(new Color4(0, 1, 0.4f, 1)));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 6 | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 7 | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 8 | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 9 | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 5 | FALSE
        [Test]
        public void TestCommandsWithoutEndValuesRepeatStartValues()
        {
            var sprite = onlySprite(decode(sprite_line +
                                           " S,0,0,100,1\n V,0,0,100,1,2\n M,0,0,100,10,20\n C,0,0,100,255,0,51"));
            var c = sprite.Commands;

            Assert.That(c.Scale.Single().EndValue, Is.EqualTo(1f));
            Assert.That(c.VectorScale.Single().EndValue, Is.EqualTo(new Vector2(1, 2)));
            Assert.That(c.X.Single().EndValue, Is.EqualTo(10f));
            Assert.That(c.Y.Single().EndValue, Is.EqualTo(20f));
            Assert.That(c.Colour.Single().EndValue, Is.EqualTo(c.Colour.Single().StartValue));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | case P | case P
        // CF: LegacyStoryboardDecoder.handleEvents | case A | case A
        // CF: LegacyStoryboardDecoder.handleEvents | case H | case H
        // CF: LegacyStoryboardDecoder.handleEvents | case V (parameter) | case V
        // CF: LegacyStoryboardDecoder.handleEvents | startTime == endTime | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | startTime == endTime | FALSE
        [Test]
        public void TestParameterCommands()
        {
            var c = onlySprite(decode(sprite_line + " P,0,0,100,A\n P,0,500,500,A\n P,0,0,100,H\n P,0,0,0,V\n P,0,0,100,X")).Commands;

            Assert.That(c.BlendingParameters[0].StartValue, Is.EqualTo(BlendingParameters.Additive));
            Assert.That(c.BlendingParameters[0].EndValue, Is.EqualTo(BlendingParameters.Inherit));
            Assert.That(c.BlendingParameters[1].EndValue, Is.EqualTo(BlendingParameters.Additive), "instant command stays additive");
            Assert.That((c.FlipH.Single().StartValue, c.FlipH.Single().EndValue), Is.EqualTo((true, false)));
            Assert.That((c.FlipV.Single().StartValue, c.FlipV.Single().EndValue), Is.EqualTo((true, true)));
            Assert.That(c.AllCommands.Count(), Is.EqualTo(4), "unknown parameter type adds nothing");
        }

        // CF: LegacyStoryboardDecoder.handleEvents | commandType default (unknown) | throws InvalidDataException
        // CF: LegacyStoryboardDecoder.handleEvents | currentCommandsGroup?.Add | null
        [Test]
        public void TestUnknownCommandAndCommandWithoutSpriteAreSkipped()
        {
            var sb = decode(" F,0,0,100,1\n" + sprite_line + " Z,0,0,100,1\n F,0,0,100,1");

            Assert.That(onlySprite(sb).Commands.Alpha.Count, Is.EqualTo(1), "the unknown command line is dropped, later ones continue");
        }

        // CF: LegacyStoryboardDecoder.handleEvents | commandType == L | case L
        [Test]
        public void TestLoopGroup()
        {
            var sprite = onlySprite(decode(sprite_line + " L,1000,3\n  F,0,0,100,0,1\n F,0,5000,6000,1,0"));

            Assert.That(sprite.LoopingGroups.Single().TotalIterations, Is.EqualTo(3));
            Assert.That(sprite.LoopingGroups.Single().Alpha.Count, Is.EqualTo(1));
            Assert.That(sprite.Commands.Alpha.Count, Is.EqualTo(1), "depth 1 returns to the root command group");
        }

        // CF: LegacyStoryboardDecoder.handleEvents | repeatCount - 1 clamped by Math.Max(0, ...) | no throw
        [Test]
        public void TestLoopWithZeroRepeatCountRunsOnce()
        {
            Assert.That(onlySprite(decode(sprite_line + " L,0,0")).LoopingGroups.Single().TotalIterations, Is.EqualTo(1));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | commandType == T | case T
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 2 | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 3 | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 4 | TRUE
        [Test]
        public void TestTriggerGroupFull()
        {
            var group = onlySprite(decode(sprite_line + " T,HitSoundClap,100,200,2\n  F,0,0,100,0,1")).TriggerGroups.Single();

            Assert.That((group.TriggerName, group.TriggerStartTime, group.TriggerEndTime, group.GroupNumber), Is.EqualTo(("HitSoundClap", 100d, 200d, -2)));
            Assert.That(group.Alpha.Count, Is.EqualTo(1));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 2 | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 3 | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 4 | FALSE
        [Test]
        public void TestTriggerGroupDefaults()
        {
            var group = onlySprite(decode(sprite_line + " T,Failing")).TriggerGroups.Single();

            Assert.That((group.TriggerStartTime, group.TriggerEndTime, group.GroupNumber), Is.EqualTo((double.MinValue, double.MaxValue, 0)));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | case Animation | case Animation
        // CF: LegacyStoryboardDecoder.handleEvents | FormatVersion < 6 | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 8 | TRUE
        // CF: LegacyStoryboardDecoder.parseAnimationLoopType | Enum.IsDefined(parsed) | TRUE
        [Test]
        public void TestAnimation()
        {
            var anim = (StoryboardAnimation)onlySprite(decode("Animation,Foreground,TopLeft,\"a.png\",10,20,4,100,LoopOnce"));

            Assert.That((anim.FrameCount, anim.FrameDelay, anim.LoopType), Is.EqualTo((4, 100d, AnimationLoopType.LoopOnce)));
            Assert.That(anim.InitialPosition, Is.EqualTo(new Vector2(10, 20)));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | FormatVersion < 6 | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 8 | FALSE
        // CF: LegacyStoryboardDecoder.parseAnimationLoopType | Enum.IsDefined(parsed) | FALSE
        [Test]
        public void TestLegacyAnimationFrameDelayAndLoopDefaults()
        {
            var old = (StoryboardAnimation)onlySprite(decode("Animation,Foreground,TopLeft,\"a.png\",0,0,4,100", 5));

            // stable quirk: round(0.015 * delay) * 1.186 * (1000 / 60)
            Assert.That(old.FrameDelay, Is.EqualTo(2 * 1.186 * (1000 / 60f)).Within(1e-3));
            Assert.That(old.LoopType, Is.EqualTo(AnimationLoopType.LoopForever));

            var undefined = (StoryboardAnimation)onlySprite(decode("Animation,Foreground,TopLeft,\"a.png\",0,0,4,100,99"));
            Assert.That(undefined.LoopType, Is.EqualTo(AnimationLoopType.LoopForever));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | case Sample | case Sample
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 4 (sample) | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 4 (sample) | FALSE
        [Test]
        public void TestSamples()
        {
            var sb = decode("Sample,1000,Pass,\"s.wav\",80\nSample,2000,Pass,\"t.wav\"");

            var samples = sb.GetLayer("Pass").Elements.Cast<StoryboardSampleInfo>().ToArray();
            Assert.That(samples.Select(s => (s.Path, s.StartTime, s.Volume)), Is.EqualTo(new[] { ("s.wav", 1000d, 80), ("t.wav", 2000d, 100) }));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | case Video | case Video
        // CF: LegacyStoryboardDecoder.handleEvents | video extension supported | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | video extension supported | TRUE
        [Test]
        public void TestVideo()
        {
            var sb = decode("Video,100,\"clip.mp4\"\nVideo,0,\"notavideo.png\"");

            Assert.That(sb.PrimaryVideo, Is.Not.Null);
            Assert.That(sb.PrimaryVideo!.Path, Is.EqualTo("clip.mp4"));
            Assert.That(sb.PrimaryVideo.StartTime, Is.EqualTo(100));
            Assert.That(sb.GetLayer("Video").Elements.Count, Is.EqualTo(1));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | case Background | case Background
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 4 (background) | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | split.Length > 4 (background) | FALSE
        [Test]
        public void TestBackgroundOffset()
        {
            Assert.That(decode("0,0,\"bg.jpg\",10,-20").BackgroundOffset, Is.EqualTo(new Vector2(10, -20)));
            Assert.That(decode("0,0,\"bg.jpg\"").BackgroundOffset, Is.EqualTo(Vector2.Zero));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | !Enum.TryParse(split[0]) | TRUE
        // CF: LegacyStoryboardDecoder.handleEvents | Enum.TryParse(split[0]) | FALSE
        // CF: LegacyStoryboardDecoder.handleEvents | type no case (Break/Colour) | default
        [Test]
        public void TestUnknownAndIgnoredEventTypes()
        {
            var sb = decode("Garbage,1,2\n2,1000,2000\n" + sprite_line);

            Assert.That(sb.GetLayer("Foreground").Elements.Count, Is.EqualTo(1));
        }

        // CF: LegacyStoryboardDecoder.handleEvents | isPrimaryStream | FALSE
        [Test]
        public void TestSecondaryStreamUsesSharedSource()
        {
            var decoder = new LegacyStoryboardDecoder(14);
            var sb = decoder.Decode(reader("osu file format v14\n\n[Events]\n" + sprite_line), reader("osu file format v14\n\n[Events]\nSprite,Background,Centre,\"shared.png\",0,0\n"));

            Assert.That(((StoryboardSprite)sb.GetLayer("Foreground").Elements.Single()).Source, Is.EqualTo(StoryboardElementSource.Beatmap));
            Assert.That(((StoryboardSprite)sb.GetLayer("Background").Elements.Single()).Source, Is.EqualTo(StoryboardElementSource.Shared));
        }

        // CF: LegacyStoryboardDecoder.decodeVariables | !line.Contains('$') | TRUE
        // CF: LegacyStoryboardDecoder.decodeVariables | !line.Contains('$') | FALSE
        // CF: LegacyStoryboardDecoder.decodeVariables | foreach variable | loop n
        // CF: LegacyStoryboardDecoder.ParseLine | case Variables | case Variables
        [Test]
        public void TestVariablesAreSubstituted()
        {
            var sb = decode("Sprite,Foreground,Centre,\"$name.png\",$x,240", preamble: "[Variables]\n$x=123\n$name=pic\n\n");

            var sprite = onlySprite(sb);
            Assert.That(sprite.Path, Is.EqualTo("pic.png"));
            Assert.That(sprite.InitialPosition, Is.EqualTo(new Vector2(123, 240)));
        }

        // CF: LegacyStoryboardDecoder.CreateTemplateObject | sb.Beatmap = beatmap | non-null
        // CF: LegacyStoryboardDecoder.ParseLine | unhandled section -> base | default
        [Test]
        public void TestTemplateHasBeatmapAndOtherSectionsDelegate()
        {
            var sb = decode("", preamble: "[Colours]\nCombo1: 1,2,3\n\n");

            Assert.That(sb.Beatmap, Is.Not.Null);
            Assert.That(sb.Beatmap.WidescreenStoryboard, Is.False);
        }

        // CF: LegacyStoryboardDecoder.Register | AddDecoder/SetFallbackDecoder | no throw
        [Test]
        public void TestRegisteredDecoderSelection()
        {
            LegacyStoryboardDecoder.Register();

            var decoder = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Storyboard>(reader("[Events]\n"));
            Assert.That(decoder, Is.TypeOf<LegacyStoryboardDecoder>());
        }
    }
}
