// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Beatmaps.Legacy;
using osu.Game.IO;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Tests.Beatmaps.Formats
{
    [TestFixture]
    public class LegacyBeatmapDecoderTest
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

        private static Beatmap decode(string body, int version = 14, bool applyOffsets = true)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"osu file format v{version}\n\n{body}"));
            using var reader = new LineBufferedReader(stream);
            return new LegacyBeatmapDecoder(version) { ApplyOffsets = applyOffsets }.Decode(reader);
        }

        // CF: LegacyBeatmapDecoder.ParseLine | case General | case General
        // CF: LegacyBeatmapDecoder.handleGeneral | case AudioFilename | case AudioFilename
        // CF: LegacyBeatmapDecoder.handleGeneral | case AudioLeadIn | case AudioLeadIn
        // CF: LegacyBeatmapDecoder.handleGeneral | case StackLeniency | case StackLeniency
        // CF: LegacyBeatmapDecoder.handleGeneral | case LetterboxInBreaks | case LetterboxInBreaks
        // CF: LegacyBeatmapDecoder.handleGeneral | case SpecialStyle | case SpecialStyle
        // CF: LegacyBeatmapDecoder.handleGeneral | case WidescreenStoryboard | case WidescreenStoryboard
        // CF: LegacyBeatmapDecoder.handleGeneral | case EpilepsyWarning | case EpilepsyWarning
        // CF: LegacyBeatmapDecoder.handleGeneral | case SamplesMatchPlaybackRate | case SamplesMatchPlaybackRate
        // CF: LegacyBeatmapDecoder.handleGeneral | case Countdown | case Countdown
        // CF: LegacyBeatmapDecoder.handleGeneral | case CountdownOffset | case CountdownOffset
        // CF: LegacyBeatmapDecoder.handleGeneral | case Mode | case Mode
        // CF: LegacyBeatmapDecoder.handleGeneral | case PreviewTime | case PreviewTime
        // CF: LegacyBeatmapDecoder.handleGeneral | time == -1 | TRUE
        [Test]
        public void TestGeneralSection()
        {
            var beatmap = decode(@"[General]
AudioFilename: sub\song.mp3
AudioLeadIn: 500
PreviewTime: -1
StackLeniency: 0.5
Mode: 1
LetterboxInBreaks: 1
SpecialStyle: 1
WidescreenStoryboard: 1
EpilepsyWarning: 1
SamplesMatchPlaybackRate: 1
Countdown: DoubleSpeed
CountdownOffset: 2
");

            Assert.That(beatmap.BeatmapInfo.Metadata.AudioFile, Is.EqualTo("sub/song.mp3"));
            Assert.That(beatmap.AudioLeadIn, Is.EqualTo(500));
            Assert.That(beatmap.BeatmapInfo.Metadata.PreviewTime, Is.EqualTo(-1));
            Assert.That(beatmap.StackLeniency, Is.EqualTo(0.5f));
            Assert.That(beatmap.BeatmapInfo.Ruleset.OnlineID, Is.EqualTo(1));
            Assert.That(beatmap.LetterboxInBreaks, Is.True);
            Assert.That(beatmap.SpecialStyle, Is.True);
            Assert.That(beatmap.WidescreenStoryboard, Is.True);
            Assert.That(beatmap.EpilepsyWarning, Is.True);
            Assert.That(beatmap.SamplesMatchPlaybackRate, Is.True);
            Assert.That(beatmap.Countdown, Is.EqualTo(CountdownType.DoubleSpeed));
            Assert.That(beatmap.CountdownOffset, Is.EqualTo(2));
        }

        // CF: LegacyBeatmapDecoder.handleGeneral | time == -1 | FALSE
        // CF: LegacyBeatmapDecoder.LegacyBeatmapDecoder | FormatVersion < 5 | TRUE
        [Test]
        public void TestPreviewTimeAndEarlyVersionOffset()
        {
            Assert.That(decode("[General]\nPreviewTime: 1000", 4).BeatmapInfo.Metadata.PreviewTime, Is.EqualTo(1000 + LegacyBeatmapDecoder.EARLY_VERSION_TIMING_OFFSET));
            Assert.That(decode("[General]\nPreviewTime: 1000", 4, applyOffsets: false).BeatmapInfo.Metadata.PreviewTime, Is.EqualTo(1000));
        }

        // CF: LegacyBeatmapDecoder.LegacyBeatmapDecoder | FormatVersion < 5 | FALSE
        // CF: LegacyBeatmapDecoder.handleGeneral | case SampleSet | case SampleSet
        // CF: LegacyBeatmapDecoder.handleGeneral | case SampleVolume | case SampleVolume
        [Test]
        public void TestModernVersionHasNoOffsetAndDefaultSampleSettingsApply()
        {
            var beatmap = decode(@"[General]
SampleSet: Soft
SampleVolume: 70
PreviewTime: 1000

[TimingPoints]
0,500
");

            Assert.That(beatmap.BeatmapInfo.Metadata.PreviewTime, Is.EqualTo(1000));

            var sample = ((LegacyControlPointInfo)beatmap.ControlPointInfo).SamplePointAt(0);
            Assert.That(sample.SampleBank, Is.EqualTo("soft"));
            Assert.That(sample.SampleVolume, Is.EqualTo(70));
        }

        // CF: LegacyBeatmapDecoder.handleGeneral | Mode ruleset unavailable | no throw
        [Test]
        public void TestUnknownModeIsSkippedByLineErrorHandling()
        {
            // the failing line is logged and skipped by the base decoder; the default (osu!) ruleset remains.
            Assert.That(decode("[General]\nMode: 99").BeatmapInfo.Ruleset.OnlineID, Is.EqualTo(0));
        }

        // CF: LegacyBeatmapDecoder.ParseLine | case Metadata | case Metadata
        // CF: LegacyBeatmapDecoder.handleMetadata | case Title..BeatmapSetID | case Title
        // CF: LegacyBeatmapDecoder.handleMetadata | case TitleUnicode | case TitleUnicode
        // CF: LegacyBeatmapDecoder.handleMetadata | case Artist | case Artist
        // CF: LegacyBeatmapDecoder.handleMetadata | case ArtistUnicode | case ArtistUnicode
        // CF: LegacyBeatmapDecoder.handleMetadata | case Creator | case Creator
        // CF: LegacyBeatmapDecoder.handleMetadata | case Version | case Version
        // CF: LegacyBeatmapDecoder.handleMetadata | case Source | case Source
        // CF: LegacyBeatmapDecoder.handleMetadata | case Tags | case Tags
        // CF: LegacyBeatmapDecoder.handleMetadata | case BeatmapID | case BeatmapID
        // CF: LegacyBeatmapDecoder.handleMetadata | case BeatmapSetID | case BeatmapSetID
        [Test]
        public void TestMetadataSection()
        {
            var beatmap = decode(@"[Metadata]
Title:T
TitleUnicode:TU
Artist:A
ArtistUnicode:AU
Creator:C
Version:V
Source:S
Tags:a b
BeatmapID:11
BeatmapSetID:22
");

            var m = beatmap.BeatmapInfo.Metadata;
            Assert.That((m.Title, m.TitleUnicode, m.Artist, m.ArtistUnicode, m.Author.Username, m.Source, m.Tags), Is.EqualTo(("T", "TU", "A", "AU", "C", "S", "a b")));
            Assert.That(beatmap.BeatmapInfo.DifficultyName, Is.EqualTo("V"));
            Assert.That(beatmap.BeatmapInfo.OnlineID, Is.EqualTo(11));
            Assert.That(beatmap.BeatmapInfo.BeatmapSet!.OnlineID, Is.EqualTo(22));
        }

        // CF: LegacyBeatmapDecoder.ParseLine | case Editor | case Editor
        // CF: LegacyBeatmapDecoder.handleEditor | case Bookmarks | case Bookmarks
        // CF: LegacyBeatmapDecoder.handleEditor | case VelocityPresets | case VelocityPresets
        // CF: LegacyBeatmapDecoder.handleEditor | case DistanceSpacing | case DistanceSpacing
        // CF: LegacyBeatmapDecoder.handleEditor | case BeatDivisor | case BeatDivisor
        // CF: LegacyBeatmapDecoder.handleEditor | case GridSize | case GridSize
        // CF: LegacyBeatmapDecoder.handleEditor | case TimelineZoom | case TimelineZoom
        [Test]
        public void TestEditorSection()
        {
            var beatmap = decode(@"[Editor]
Bookmarks: 1,x,3
VelocityPresets: 0.5,bad,2
DistanceSpacing: -1
BeatDivisor: 1000
GridSize: 8
TimelineZoom: -4
");

            Assert.That(beatmap.Bookmarks, Is.EqualTo(new[] { 1, 3 }), "unparseable entries are skipped");
            Assert.That(beatmap.SliderVelocityPresets, Is.EqualTo(new[] { 0.5, 2 }));
            Assert.That(beatmap.DistanceSpacing, Is.Zero, "negative values clamp to 0");
            Assert.That(beatmap.BeatmapInfo.BeatDivisor, Is.EqualTo(64), "clamped to maximum divisor");
            Assert.That(beatmap.GridSize, Is.EqualTo(8));
            Assert.That(beatmap.TimelineZoom, Is.Zero);
        }

        // CF: LegacyBeatmapDecoder.ParseLine | case Difficulty | case Difficulty
        // CF: LegacyBeatmapDecoder.handleDifficulty | case OverallDifficulty && !hasApproachRate | TRUE
        // CF: LegacyBeatmapDecoder.handleDifficulty | case HPDrainRate | case HPDrainRate
        // CF: LegacyBeatmapDecoder.handleDifficulty | case CircleSize | case CircleSize
        // CF: LegacyBeatmapDecoder.handleDifficulty | case SliderMultiplier | case SliderMultiplier
        // CF: LegacyBeatmapDecoder.handleDifficulty | case SliderTickRate | case SliderTickRate
        // CF: LegacyBeatmapDecoder.applyDifficultyRestrictions | Ruleset.OnlineID != 3 | TRUE
        [Test]
        public void TestDifficultyWithoutApproachRateCopiesOverallDifficultyAndClamps()
        {
            var d = decode(@"[Difficulty]
HPDrainRate:11
CircleSize:12
OverallDifficulty:7
SliderMultiplier:9
SliderTickRate:0.1
").Difficulty;

            Assert.That(d.DrainRate, Is.EqualTo(10));
            Assert.That(d.CircleSize, Is.EqualTo(10));
            Assert.That(d.OverallDifficulty, Is.EqualTo(7));
            Assert.That(d.ApproachRate, Is.EqualTo(7));
            Assert.That(d.SliderMultiplier, Is.EqualTo(3.6));
            Assert.That(d.SliderTickRate, Is.EqualTo(0.5));
        }

        // CF: LegacyBeatmapDecoder.handleDifficulty | case ApproachRate | case ApproachRate
        // CF: LegacyBeatmapDecoder.handleDifficulty | case OverallDifficulty && !hasApproachRate | FALSE
        [Test]
        public void TestExplicitApproachRateIsKept()
        {
            var d = decode("[Difficulty]\nApproachRate:3\nOverallDifficulty:7").Difficulty;

            Assert.That(d.ApproachRate, Is.EqualTo(3));
            Assert.That(d.OverallDifficulty, Is.EqualTo(7));
        }

        // CF: LegacyBeatmapDecoder.applyDifficultyRestrictions | Ruleset.OnlineID != 3 | FALSE
        [Test]
        public void TestManiaCircleSizeRange()
        {
            Assert.That(decode("[General]\nMode: 3\n[Difficulty]\nCircleSize:0").Difficulty.CircleSize, Is.EqualTo(1));
            Assert.That(decode("[General]\nMode: 3\n[Difficulty]\nCircleSize:30").Difficulty.CircleSize, Is.EqualTo(LegacyBeatmapDecoder.MAX_MANIA_KEY_COUNT));
        }

        // CF: LegacyBeatmapDecoder.ParseLine | case Events | case Events
        // CF: LegacyBeatmapDecoder.handleEvent | Enum.TryParse(split[0]) | TRUE
        // CF: LegacyBeatmapDecoder.handleEvent | case Background | case Background
        // CF: LegacyBeatmapDecoder.handleEvent | case Break | case Break
        // CF: LegacyBeatmapDecoder.handleEvent | case Sprite | case Sprite
        // CF: LegacyBeatmapDecoder.handleEvent | string.IsNullOrEmpty(BackgroundFile) | FALSE
        // CF: LegacyBeatmapDecoder.handleEvent | case Video | case Video
        // CF: LegacyBeatmapDecoder.handleEvent | video extension not supported | FALSE
        [Test]
        public void TestEventsBackgroundVideoBreak()
        {
            var beatmap = decode(@"[Events]
0,0,""bg.jpg"",0,0
Sprite,Background,Centre,""other.png"",320,240
Video,0,""clip.mp4""
2,1000,500
");

            Assert.That(beatmap.BeatmapInfo.Metadata.BackgroundFile, Is.EqualTo("bg.jpg"), "video and later sprite do not replace the background");
            Assert.That(beatmap.Breaks.Single().StartTime, Is.EqualTo(1000));
            Assert.That(beatmap.Breaks.Single().EndTime, Is.EqualTo(1000), "end is clamped to not precede start");
        }

        // CF: LegacyBeatmapDecoder.handleEvent | Enum.TryParse(split[0]) | FALSE
        // CF: LegacyBeatmapDecoder.handleEvent | string.IsNullOrEmpty(BackgroundFile) | TRUE
        // CF: LegacyBeatmapDecoder.handleEvent | video extension not supported | TRUE
        [Test]
        public void TestEventsUnknownTypeIgnoredAndFallbackBackgrounds()
        {
            Assert.That(decode("[Events]\nGarbage,1,2").BeatmapInfo.Metadata.BackgroundFile, Is.Null.Or.Empty);
            Assert.That(decode("[Events]\n4,0,0,\"first.png\",0,0").BeatmapInfo.Metadata.BackgroundFile, Is.EqualTo("first.png"));
            Assert.That(decode("[Events]\n1,0,\"bg-as-video.png\"").BeatmapInfo.Metadata.BackgroundFile, Is.EqualTo("bg-as-video.png"));
        }

        // CF: LegacyBeatmapDecoder.ParseLine | case TimingPoints | case TimingPoints
        // CF: LegacyBeatmapDecoder.handleTimingPoint | beatLength < 0 | FALSE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 3 | FALSE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 4 | FALSE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 5 | FALSE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 6 | FALSE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 7 | FALSE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 8 | FALSE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | timingChange | TRUE
        [Test]
        public void TestMinimalTimingPoint()
        {
            var info = decode("[TimingPoints]\n1000,400").ControlPointInfo;

            Assert.That(info.TimingPoints.Single().Time, Is.EqualTo(1000));
            Assert.That(info.TimingPoints.Single().BeatLength, Is.EqualTo(400));
            Assert.That(info.TimingPoints.Single().TimeSignature, Is.EqualTo(osu.Game.Beatmaps.Timing.TimeSignature.SimpleQuadruple));
            Assert.That(info.EffectPointAt(1000).KiaiMode, Is.False);
        }

        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 3 | TRUE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split[2][0] == '0' | FALSE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 4 | TRUE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 5 | TRUE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 6 | TRUE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 7 | TRUE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | split.Length >= 8 | TRUE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | stringSampleSet == none | FALSE
        [Test]
        public void TestFullTimingPoint()
        {
            // time, beatLength, meter, sampleSet(Drum=3), customBank, volume, timingChange, effects(kiai|omitBarLine = 1|8)
            var beatmap = decode("[TimingPoints]\n0,500,3,3,2,40,1,9");
            var info = (LegacyControlPointInfo)beatmap.ControlPointInfo;

            Assert.That(info.TimingPoints.Single().TimeSignature.Numerator, Is.EqualTo(3));
            Assert.That(info.TimingPoints.Single().OmitFirstBarLine, Is.True);
            Assert.That(info.EffectPointAt(0).KiaiMode, Is.True);

            var sample = info.SamplePointAt(0);
            Assert.That(sample.SampleBank, Is.EqualTo("drum"));
            Assert.That(sample.SampleVolume, Is.EqualTo(40));
        }

        // CF: LegacyBeatmapDecoder.handleTimingPoint | split[2][0] == '0' | TRUE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | stringSampleSet == none | TRUE
        [Test]
        public void TestTimeSignatureZeroAndNoneSampleSetFallBack()
        {
            var info = (LegacyControlPointInfo)decode("[TimingPoints]\n0,500,0,0").ControlPointInfo;

            Assert.That(info.TimingPoints.Single().TimeSignature, Is.EqualTo(osu.Game.Beatmaps.Timing.TimeSignature.SimpleQuadruple));
            Assert.That(info.SamplePointAt(0).SampleBank, Is.EqualTo("normal"));
        }

        // CF: LegacyBeatmapDecoder.handleTimingPoint | beatLength < 0 | TRUE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | timingChange | FALSE
        [Test]
        public void TestInheritedPointSetsSliderVelocity()
        {
            var info = (LegacyControlPointInfo)decode("[TimingPoints]\n0,500\n1000,-50,4,1,0,100,0,0").ControlPointInfo;

            Assert.That(info.TimingPoints.Count, Is.EqualTo(1));
            Assert.That(info.DifficultyPointAt(1000).SliderVelocity, Is.EqualTo(2).Within(1e-9));
        }

        // CF: LegacyBeatmapDecoder.handleTimingPoint | double.IsNaN(beatLength) (timingChange) | TRUE
        [Test]
        public void TestNaNBeatLengthOnTimingChangeIsRejected()
        {
            // the InvalidDataException is caught per-line by the base decoder, so no control point results.
            Assert.That(decode("[TimingPoints]\n0,NaN,4,1,0,100,1,0").ControlPointInfo.TimingPoints, Is.Empty);
        }

        // CF: LegacyBeatmapDecoder.handleTimingPoint | double.IsNaN(beatLength) (timingChange) | FALSE
        [Test]
        public void TestNaNBeatLengthOnInheritedPointDisablesTicks()
        {
            var info = (LegacyControlPointInfo)decode("[TimingPoints]\n0,500\n1000,NaN,4,1,0,100,0,0").ControlPointInfo;

            Assert.That(info.DifficultyPointAt(1000).GenerateTicks, Is.False);
            Assert.That(info.DifficultyPointAt(1000).SliderVelocity, Is.EqualTo(1));
        }

        // CF: LegacyBeatmapDecoder.handleTimingPoint | onlineRulesetID == 1 || onlineRulesetID == 3 | TRUE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | onlineRulesetID == 1 | TRUE
        [Test]
        public void TestTaikoUsesEffectPointForScrollSpeed()
        {
            var info = decode("[General]\nMode: 1\n[TimingPoints]\n0,500\n1000,-50,4,1,0,100,0,0").ControlPointInfo;

            Assert.That(info.EffectPointAt(1000).ScrollSpeed, Is.EqualTo(2).Within(1e-9));
        }

        // CF: LegacyBeatmapDecoder.handleTimingPoint | onlineRulesetID == 1 | FALSE
        // CF: LegacyBeatmapDecoder.handleTimingPoint | onlineRulesetID == 3 | TRUE
        [Test]
        public void TestManiaUsesEffectPointForScrollSpeed()
        {
            var info = decode("[General]\nMode: 3\n[TimingPoints]\n0,500\n1000,-50,4,1,0,100,0,0").ControlPointInfo;

            Assert.That(info.EffectPointAt(1000).ScrollSpeed, Is.EqualTo(2).Within(1e-9));
        }

        // CF: LegacyBeatmapDecoder.handleTimingPoint | onlineRulesetID == 3 | FALSE
        [Test]
        public void TestOsuDoesNotUseEffectScrollSpeed()
        {
            var info = decode("[TimingPoints]\n0,500\n1000,-50,4,1,0,100,0,0").ControlPointInfo;

            Assert.That(info.EffectPointAt(1000).ScrollSpeed, Is.EqualTo(1));
        }

        // CF: LegacyBeatmapDecoder.addControlPoint | time != pendingControlPointsTime | TRUE
        // CF: LegacyBeatmapDecoder.addControlPoint | timingChange | FALSE
        // CF: LegacyBeatmapDecoder.flushPendingPoints | !pendingControlPointTypes.Add(type) | TRUE
        [Test]
        public void TestLaterPointAtSameTimeOverridesEarlier()
        {
            var info = (LegacyControlPointInfo)decode("[TimingPoints]\n0,500,4,1,0,100,1,1\n0,-100,4,1,0,100,0,0").ControlPointInfo;

            Assert.That(info.EffectPointAt(0).KiaiMode, Is.False, "the non-timing-change point overrides the one from the timing point");
        }

        // CF: LegacyBeatmapDecoder.flushPendingPoints | !pendingControlPointTypes.Add(type) | FALSE
        // CF: LegacyBeatmapDecoder.addControlPoint | time != pendingControlPointsTime | FALSE
        [Test]
        public void TestPointsAtDifferentTimesAreKept()
        {
            var info = decode("[TimingPoints]\n0,500\n1000,250").ControlPointInfo;

            Assert.That(info.TimingPoints.Select(t => t.BeatLength), Is.EqualTo(new[] { 500.0, 250.0 }));
        }

        // CF: LegacyBeatmapDecoder.ParseLine | case HitObjects | case HitObjects
        // CF: LegacyBeatmapDecoder.ParseStreamInto | foreach hitObject | loop n
        // CF: LegacyBeatmapDecoder.ParseStreamInto | HitObjects.OrderBy(StartTime) | no throw
        // CF: LegacyBeatmapDecoder.applySamples | hitObject is IHasRepeats | FALSE
        [Test]
        public void TestHitObjectsAreSortedStably()
        {
            var beatmap = decode(@"[HitObjects]
256,192,2000,1,0
100,100,1000,1,0
200,200,1000,1,0
");

            Assert.That(beatmap.HitObjects.Select(h => h.StartTime), Is.EqualTo(new[] { 1000.0, 1000.0, 2000.0 }));
            Assert.That(((IHasPosition)beatmap.HitObjects[0]).X, Is.EqualTo(100), "stable for equal start times");
        }

        // CF: LegacyBeatmapDecoder.applySamples | hitObject is IHasRepeats | TRUE
        // CF: LegacyBeatmapDecoder.applySamples | for NodeSamples | loop n
        // CF: LegacyBeatmapDecoder.applyDefaults | hitObject is IHasGenerateTicks | TRUE
        // CF: LegacyBeatmapDecoder.applyDefaults | hitObject is IHasSliderVelocity | TRUE
        [Test]
        public void TestSliderGetsSamplesFromControlPoints()
        {
            var beatmap = decode(@"[TimingPoints]
0,500,4,2,0,60,1,0

[HitObjects]
0,0,0,2,0,L|100:0,1,100
");

            var slider = (IHasRepeats)beatmap.HitObjects.Single();
            Assert.That(slider.NodeSamples.Count, Is.EqualTo(2));
            Assert.That(slider.NodeSamples.All(n => n.All(s => s.Volume == 60)), Is.True);
        }

        // CF: LegacyBeatmapDecoder.postProcessBreaks | while currentBreak < Breaks.Count && EndTime < StartTime | TRUE
        // CF: LegacyBeatmapDecoder.postProcessBreaks | while currentBreak < Breaks.Count | FALSE
        // CF: LegacyBeatmapDecoder.postProcessBreaks | foreach ConvertHitObject | loop n
        [Test]
        public void TestObjectAfterBreakForcesNewCombo()
        {
            var beatmap = decode(@"[Events]
2,1000,2000

[HitObjects]
0,0,500,1,0
0,0,600,1,0
0,0,3000,1,0
0,0,3100,1,0
");

            var combos = beatmap.HitObjects.Select(h => ((ConvertHitObject)h).NewCombo).ToArray();
            Assert.That(combos, Is.EqualTo(new[] { true, false, true, false }));
        }

        // CF: LegacyBeatmapDecoder.postProcessBreaks | foreach ConvertHitObject | loop 0
        [Test]
        public void TestNoHitObjects()
        {
            Assert.That(decode("[Events]\n2,1000,2000").HitObjects, Is.Empty);
        }

        // CF: LegacyBeatmapDecoder.CreateTemplateObject | ControlPointInfo = new LegacyControlPointInfo | non-null
        // CF: LegacyBeatmapDecoder.ParseLine | unhandled section -> base.ParseLine | default
        [Test]
        public void TestControlPointInfoIsLegacyAndColoursSectionIsHandledByBase()
        {
            var beatmap = decode("[Colours]\nCombo1 : 1,2,3");

            Assert.That(beatmap.ControlPointInfo, Is.TypeOf<LegacyControlPointInfo>());
        }

        // CF: LegacyBeatmapDecoder.Register | AddDecoder/SetFallbackDecoder | no throw
        [Test]
        public void TestRegisteredDecoderSelectedByHeader()
        {
            LegacyBeatmapDecoder.Register();

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("osu file format v9\n\n[Metadata]\nTitle:X"));
            using var reader = new LineBufferedReader(stream);
            var decoder = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader);

            Assert.That(decoder, Is.TypeOf<LegacyBeatmapDecoder>());
            Assert.That(((LegacyBeatmapDecoder)decoder).Decode(reader).BeatmapVersion, Is.EqualTo(9));
        }
    }
}
