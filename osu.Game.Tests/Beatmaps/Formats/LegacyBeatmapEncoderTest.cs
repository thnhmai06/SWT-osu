// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Taiko;
using osu.Game.Skinning;
using osu.Game.Storyboards;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tests.Beatmaps.Formats
{
    [TestFixture]
    public class LegacyBeatmapEncoderTest
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

        private static Beatmap createBeatmap(RulesetInfo? ruleset = null)
        {
            var beatmap = new Beatmap();
            beatmap.BeatmapInfo.Ruleset = ruleset ?? new OsuRuleset().RulesetInfo;
            return beatmap;
        }

        private static string[] encode(IBeatmap beatmap, ISkin? skin = null, Storyboard? storyboard = null)
        {
            using var writer = new StringWriter();
            new LegacyBeatmapEncoder(beatmap, skin, storyboard).Encode(writer);
            return writer.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        private static List<string> section(string[] lines, string name)
            => lines.SkipWhile(l => l != $"[{name}]").Skip(1).TakeWhile(l => !l.StartsWith('[') && l.Length > 0).ToList();

        // CF: LegacyBeatmapEncoder.LegacyBeatmapEncoder | onlineRulesetID < 0 || onlineRulesetID > 3 | TRUE
        // CF: LegacyBeatmapEncoder.LegacyBeatmapEncoder | onlineRulesetID < 0 | TRUE
        // CF: LegacyBeatmapEncoder.LegacyBeatmapEncoder | onlineRulesetID > 3 | TRUE
        // CF: LegacyBeatmapEncoder.LegacyBeatmapEncoder | onlineRulesetID > 3 | throws ArgumentException
        [Test]
        public void TestUnsupportedRulesetThrows()
        {
            var tooHigh = createBeatmap(new RulesetInfo("custom", "Custom", "x", 4));
            var negative = createBeatmap(new RulesetInfo("custom", "Custom", "x", -1));

            Assert.Throws<ArgumentException>(() => new LegacyBeatmapEncoder(tooHigh, null, null));
            Assert.Throws<ArgumentException>(() => new LegacyBeatmapEncoder(negative, null, null));
        }

        // CF: LegacyBeatmapEncoder.LegacyBeatmapEncoder | onlineRulesetID > 3 | FALSE
        // CF: LegacyBeatmapEncoder.LegacyBeatmapEncoder | storyboard != null | FALSE
        // CF: LegacyBeatmapEncoder.LegacyBeatmapEncoder | storyboard != null | TRUE
        // CF: LegacyBeatmapEncoder.handleGeneral | storyboardEncoder != null | FALSE
        // CF: LegacyBeatmapEncoder.handleGeneral | AudioFile not empty | FALSE
        // CF: LegacyBeatmapEncoder.handleGeneral | beatmap.EpilepsyWarning | FALSE
        // CF: LegacyBeatmapEncoder.handleGeneral | beatmap.CountdownOffset > 0 | FALSE
        // CF: LegacyBeatmapEncoder.handleGeneral | onlineRulesetID == 3 | FALSE
        // CF: LegacyBeatmapEncoder.handleGeneral | beatmap.SamplesMatchPlaybackRate | FALSE
        // CF: LegacyBeatmapEncoder.Encode | writes all sections | no throw
        [Test]
        public void TestMinimalBeatmapGeneral()
        {
            var lines = encode(createBeatmap());

            Assert.That(lines[0], Is.EqualTo($"osu file format v{LegacyBeatmapEncoder.FIRST_LAZER_VERSION}"));
            Assert.That(lines.Where(l => l.StartsWith('[')), Is.EqualTo(new[] { "[General]", "[Editor]", "[Metadata]", "[Difficulty]", "[Events]", "[TimingPoints]", "[HitObjects]" }),
                "no [Colours] section without a skin");

            var general = section(lines, "General");
            Assert.That(general, Does.Contain("Mode: 0"));
            Assert.That(general, Does.Contain("WidescreenStoryboard: 1"), "enabled by default");
            Assert.That(general.Any(l => l.StartsWith("AudioFilename")), Is.False);
            Assert.That(general.Any(l => l.StartsWith("EpilepsyWarning")), Is.False);
            Assert.That(general.Any(l => l.StartsWith("CountdownOffset")), Is.False);
            Assert.That(general.Any(l => l.StartsWith("SpecialStyle")), Is.False);
            Assert.That(general.Any(l => l.StartsWith("SamplesMatchPlaybackRate")), Is.False);
            Assert.That(general, Does.Contain("SampleSet: Normal"));
        }

        // CF: LegacyBeatmapEncoder.handleGeneral | AudioFile not empty | TRUE
        // CF: LegacyBeatmapEncoder.handleGeneral | beatmap.EpilepsyWarning | TRUE
        // CF: LegacyBeatmapEncoder.handleGeneral | beatmap.CountdownOffset > 0 | TRUE
        // CF: LegacyBeatmapEncoder.handleGeneral | onlineRulesetID == 3 | TRUE
        // CF: LegacyBeatmapEncoder.handleGeneral | beatmap.SamplesMatchPlaybackRate | TRUE
        [Test]
        public void TestOptionalGeneralLines()
        {
            var beatmap = createBeatmap(new osu.Game.Rulesets.Mania.ManiaRuleset().RulesetInfo);
            beatmap.Metadata.AudioFile = "sub/dir/audio.mp3";
            beatmap.AudioLeadIn = 250;
            beatmap.Metadata.PreviewTime = 1234;
            beatmap.Countdown = CountdownType.HalfSpeed;
            beatmap.CountdownOffset = 3;
            beatmap.StackLeniency = 0.25f;
            beatmap.LetterboxInBreaks = true;
            beatmap.EpilepsyWarning = true;
            beatmap.SpecialStyle = true;
            beatmap.SamplesMatchPlaybackRate = true;

            var general = section(encode(beatmap), "General");

            Assert.That(general, Does.Contain("AudioFilename: audio.mp3"));
            Assert.That(general, Does.Contain("AudioLeadIn: 250"));
            Assert.That(general, Does.Contain("PreviewTime: 1234"));
            Assert.That(general, Does.Contain("Countdown: 2"));
            Assert.That(general, Does.Contain("StackLeniency: 0.25"));
            Assert.That(general, Does.Contain("Mode: 3"));
            Assert.That(general, Does.Contain("LetterboxInBreaks: 1"));
            Assert.That(general, Does.Contain("EpilepsyWarning: 1"));
            Assert.That(general, Does.Contain("CountdownOffset: 3"));
            Assert.That(general, Does.Contain("SpecialStyle: 1"));
            Assert.That(general, Does.Contain("SamplesMatchPlaybackRate: 1"));
        }

        // CF: LegacyBeatmapEncoder.handleGeneral | storyboardEncoder != null | TRUE
        // CF: LegacyBeatmapEncoder.handleEvents | storyboardEncoder != null | TRUE
        [Test]
        public void TestStoryboardOverridesGeneralAndEvents()
        {
            var beatmap = createBeatmap();
            var lines = encode(beatmap, null, new Storyboard { Beatmap = beatmap, UseSkinSprites = true });

            var general = section(lines, "General");
            Assert.That(general, Does.Contain("UseSkinSprites: 1"));
            Assert.That(general, Does.Contain("WidescreenStoryboard: 1"));
            Assert.That(section(lines, "Events"), Does.Contain("// Break Periods"));
        }

        // CF: LegacyBeatmapEncoder.handleEditor | beatmap.Bookmarks.Length > 0 | TRUE
        // CF: LegacyBeatmapEncoder.handleEditor | beatmap.Bookmarks.Length > 0 | FALSE
        [Test]
        public void TestEditorSection()
        {
            var beatmap = createBeatmap();
            beatmap.DistanceSpacing = 1.5;
            beatmap.GridSize = 8;
            beatmap.TimelineZoom = 2;
            beatmap.SliderVelocityPresets = new[] { 0.5, 2 };
            beatmap.BeatmapInfo.BeatDivisor = 6;

            var withoutBookmarks = section(encode(beatmap), "Editor");
            Assert.That(withoutBookmarks.Any(l => l.StartsWith("Bookmarks")), Is.False);
            Assert.That(withoutBookmarks, Is.SupersetOf(new[] { "DistanceSpacing: 1.5", "BeatDivisor: 6", "GridSize: 8", "TimelineZoom: 2", "VelocityPresets: 0.5,2" }));

            beatmap.Bookmarks = new[] { 100, 200 };
            Assert.That(section(encode(beatmap), "Editor"), Does.Contain("Bookmarks: 100,200"));
        }

        // CF: LegacyBeatmapEncoder.handleMetadata | TitleUnicode not empty | FALSE
        // CF: LegacyBeatmapEncoder.handleMetadata | ArtistUnicode not empty | FALSE
        // CF: LegacyBeatmapEncoder.handleMetadata | Source not empty | FALSE
        // CF: LegacyBeatmapEncoder.handleMetadata | Tags not empty | FALSE
        // CF: LegacyBeatmapEncoder.handleMetadata | OnlineID > 0 | FALSE
        // CF: LegacyBeatmapEncoder.handleMetadata | BeatmapSet?.OnlineID > 0 | FALSE
        [Test]
        public void TestMetadataOptionalFieldsOmitted()
        {
            var beatmap = createBeatmap();
            beatmap.Metadata.Title = "T";
            beatmap.Metadata.Artist = "A";
            beatmap.Metadata.Author.Username = "C";
            beatmap.BeatmapInfo.DifficultyName = "V";

            var metadata = section(encode(beatmap), "Metadata");

            Assert.That(metadata, Is.EqualTo(new[] { "Title: T", "Artist: A", "Creator: C", "Version: V" }));
        }

        // CF: LegacyBeatmapEncoder.handleMetadata | TitleUnicode not empty | TRUE
        // CF: LegacyBeatmapEncoder.handleMetadata | ArtistUnicode not empty | TRUE
        // CF: LegacyBeatmapEncoder.handleMetadata | Source not empty | TRUE
        // CF: LegacyBeatmapEncoder.handleMetadata | Tags not empty | TRUE
        // CF: LegacyBeatmapEncoder.handleMetadata | OnlineID > 0 | TRUE
        // CF: LegacyBeatmapEncoder.handleMetadata | BeatmapSet?.OnlineID > 0 | TRUE
        [Test]
        public void TestMetadataOptionalFieldsWritten()
        {
            var beatmap = createBeatmap();
            beatmap.Metadata.TitleUnicode = "TU";
            beatmap.Metadata.ArtistUnicode = "AU";
            beatmap.Metadata.Source = "S";
            beatmap.Metadata.Tags = "x y";
            beatmap.BeatmapInfo.OnlineID = 5;
            beatmap.BeatmapInfo.BeatmapSet = new BeatmapSetInfo { OnlineID = 9 };

            var metadata = section(encode(beatmap), "Metadata");

            Assert.That(metadata, Is.SupersetOf(new[] { "TitleUnicode: TU", "ArtistUnicode: AU", "Source: S", "Tags: x y", "BeatmapID: 5", "BeatmapSetID: 9" }));
        }

        [Test]
        public void TestDifficultySection()
        {
            var beatmap = createBeatmap();
            beatmap.Difficulty.DrainRate = 3;
            beatmap.Difficulty.CircleSize = 4;
            beatmap.Difficulty.OverallDifficulty = 5;
            beatmap.Difficulty.ApproachRate = 6;
            beatmap.Difficulty.SliderMultiplier = 1.4;
            beatmap.Difficulty.SliderTickRate = 2;

            Assert.That(section(encode(beatmap), "Difficulty"),
                Is.EqualTo(new[] { "HPDrainRate: 3", "CircleSize: 4", "OverallDifficulty: 5", "ApproachRate: 6", "SliderMultiplier: 1.4", "SliderTickRate: 2" }));
        }

        // CF: LegacyBeatmapEncoder.handleEvents | !string.IsNullOrEmpty(BackgroundFile) | TRUE
        // CF: LegacyBeatmapEncoder.handleEvents | !string.IsNullOrEmpty(BackgroundFile) | FALSE
        // CF: LegacyBeatmapEncoder.handleEvents | foreach break | loop 0
        // CF: LegacyBeatmapEncoder.handleEvents | foreach break | loop n
        // CF: LegacyBeatmapEncoder.handleEvents | storyboardEncoder != null | FALSE
        [Test]
        public void TestEventsBackgroundAndBreaks()
        {
            var beatmap = createBeatmap();
            Assert.That(section(encode(beatmap), "Events"), Is.EqualTo(new[] { "// Break Periods" }));

            beatmap.Metadata.BackgroundFile = "bg.jpg";
            beatmap.Breaks.Add(new osu.Game.Beatmaps.Timing.BreakPeriod(1000, 2000));
            beatmap.Breaks.Add(new osu.Game.Beatmaps.Timing.BreakPeriod(3000, 4000));

            Assert.That(section(encode(beatmap), "Events"), Is.EqualTo(new[] { "0,0,\"bg.jpg\",0,0", "// Break Periods", "2,1000,2000", "2,3000,4000" }));
        }

        // CF: LegacyBeatmapEncoder.handleColours | colours == null | TRUE
        // CF: LegacyBeatmapEncoder.handleColours | colours.Count == 0 | TRUE
        // CF: LegacyBeatmapEncoder.handleColours | colours.Count == 0 | FALSE
        // CF: LegacyBeatmapEncoder.handleColours | for colours | loop n
        [Test]
        public void TestColoursSection()
        {
            var skin = new Mock<ISkin>();
            skin.Setup(s => s.GetConfig<GlobalSkinColours, IReadOnlyList<Color4>>(GlobalSkinColours.ComboColours))
                .Returns(() => new Bindable<IReadOnlyList<Color4>>(Enumerable.Repeat(new Color4(255, 0, 128, 255), 10).ToArray()));

            var lines = section(encode(createBeatmap(), skin.Object), "Colours");

            Assert.That(lines.Count, Is.EqualTo(LegacyBeatmapDecoder.MAX_COMBO_COLOUR_COUNT), "colours beyond the legacy maximum are not written");
            Assert.That(lines[0], Is.EqualTo("Combo1: 255,0,128,255"));
            Assert.That(lines[7], Does.StartWith("Combo8: "));
        }

        [Test]
        public void TestColoursSectionOmittedWhenNoColoursAvailable()
        {
            var nullSkin = new Mock<ISkin>();
            nullSkin.Setup(s => s.GetConfig<GlobalSkinColours, IReadOnlyList<Color4>>(GlobalSkinColours.ComboColours)).Returns((IBindable<IReadOnlyList<Color4>>?)null);
            Assert.That(encode(createBeatmap(), nullSkin.Object), Does.Not.Contain("[Colours]"));

            var emptySkin = new Mock<ISkin>();
            emptySkin.Setup(s => s.GetConfig<GlobalSkinColours, IReadOnlyList<Color4>>(GlobalSkinColours.ComboColours))
                     .Returns(new Bindable<IReadOnlyList<Color4>>(Array.Empty<Color4>()));
            Assert.That(encode(createBeatmap(), emptySkin.Object), Does.Not.Contain("[Colours]"));
        }

        // CF: LegacyBeatmapEncoder.handleHitObjects | beatmap.HitObjects.Count == 0 | TRUE
        // CF: LegacyBeatmapEncoder.handleHitObjects | beatmap.HitObjects.Count == 0 | FALSE
        // CF: LegacyBeatmapEncoder.handleHitObjects | foreach | loop 0
        [Test]
        public void TestNoHitObjects()
        {
            Assert.That(section(encode(createBeatmap()), "HitObjects"), Is.Empty);
        }

        // CF: LegacyBeatmapEncoder.handleHitObject | case 0 | case 0
        // CF: LegacyBeatmapEncoder.getObjectType | hitObject is IHasCombo | TRUE
        // CF: LegacyBeatmapEncoder.getObjectType | combo.NewCombo | TRUE
        // CF: LegacyBeatmapEncoder.getObjectType | default (Circle) | default
        // CF: LegacyBeatmapEncoder.handleHitObject | hitObject is IHasPath | FALSE
        // CF: LegacyBeatmapEncoder.handleHitObject | hitObject is IHasDuration | FALSE
        // CF: LegacyBeatmapEncoder.toLegacyHitSoundType | case HIT_WHISTLE | case HIT_WHISTLE
        // CF: LegacyBeatmapEncoder.toLegacySampleBank | case BANK_NORMAL | case BANK_NORMAL
        // CF: LegacyBeatmapEncoder.getSampleBank | onlineRulesetID != 3 | TRUE
        [Test]
        public void TestCircleLine()
        {
            var beatmap = createBeatmap();
            beatmap.HitObjects.Add(new HitCircle
            {
                Position = new Vector2(100, 120),
                StartTime = 1000,
                NewCombo = true,
                ComboOffset = 1,
                Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL), new HitSampleInfo(HitSampleInfo.HIT_WHISTLE) }
            });

            Assert.That(section(encode(beatmap), "HitObjects"), Is.EqualTo(new[] { "100,120,1000,21,2,1:0:0:0:" }));
        }

        // CF: LegacyBeatmapEncoder.getObjectType | combo.NewCombo | FALSE
        // CF: LegacyBeatmapEncoder.toLegacyHitSoundType | case HIT_FINISH | case HIT_FINISH
        // CF: LegacyBeatmapEncoder.toLegacyHitSoundType | case HIT_CLAP | case HIT_CLAP
        // CF: LegacyBeatmapEncoder.toLegacySampleBank | case BANK_SOFT | case BANK_SOFT
        // CF: LegacyBeatmapEncoder.toLegacySampleBank | case BANK_DRUM | case BANK_DRUM
        // CF: LegacyBeatmapEncoder.toLegacySampleBank | default | default
        // CF: LegacyBeatmapEncoder.getSampleBank | !s.EditorAutoBank | TRUE
        [Test]
        public void TestSampleBanksAndHitSounds()
        {
            var beatmap = createBeatmap();
            beatmap.HitObjects.Add(new HitCircle
            {
                Position = new Vector2(1, 2),
                StartTime = 0,
                Samples = new List<HitSampleInfo>
                {
                    new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_SOFT),
                    new HitSampleInfo(HitSampleInfo.HIT_FINISH, HitSampleInfo.BANK_DRUM, editorAutoBank: false),
                    new HitSampleInfo(HitSampleInfo.HIT_CLAP, HitSampleInfo.BANK_DRUM, editorAutoBank: false),
                }
            });

            Assert.That(section(encode(beatmap), "HitObjects"), Is.EqualTo(new[] { "1,2,0,1,12,2:3:0:0:" }));
        }

        // CF: LegacyBeatmapEncoder.getObjectType | hitObject is IHasCombo | FALSE
        // CF: LegacyBeatmapEncoder.getObjectType | case IHasDuration | case IHasDuration
        // CF: LegacyBeatmapEncoder.getObjectType | onlineRulesetID == 3 | FALSE
        // CF: LegacyBeatmapEncoder.handleHitObject | hitObject is IHasDuration | TRUE
        // CF: LegacyBeatmapEncoder.addEndTimeData | type == Hold | FALSE
        [Test]
        public void TestSpinnerLine()
        {
            var beatmap = createBeatmap();
            beatmap.HitObjects.Add(new Spinner { Position = new Vector2(256, 192), StartTime = 1000, Duration = 2000 });

            Assert.That(section(encode(beatmap), "HitObjects"), Is.EqualTo(new[] { "256,192,1000,8,0,3000,0:0:0:0:" }));
        }

        // CF: LegacyBeatmapEncoder.handleHitObject | hitObject is IHasPath | TRUE
        // CF: LegacyBeatmapEncoder.getObjectType | case IHasPath | case IHasPath
        // CF: LegacyBeatmapEncoder.addPathData | point.Type != null | TRUE
        // CF: LegacyBeatmapEncoder.addPathData | case Linear | case Linear
        // CF: LegacyBeatmapEncoder.addPathData | i != 0 | TRUE
        // CF: LegacyBeatmapEncoder.addPathData | i != Count - 1 | TRUE
        // CF: LegacyBeatmapEncoder.addPathData | i != Count - 1 | FALSE
        // CF: LegacyBeatmapEncoder.addPathData | ExpectedDistance.Value ?? Distance | non-null
        // CF: LegacyBeatmapEncoder.addPathData | curveData != null | TRUE
        // CF: LegacyBeatmapEncoder.addPathData | i < NodeSamples.Count | FALSE
        [Test]
        public void TestSliderLine()
        {
            var beatmap = createBeatmap();
            beatmap.HitObjects.Add(new Slider
            {
                Position = new Vector2(10, 20),
                StartTime = 500,
                RepeatCount = 1,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(50, 0)),
                    new PathControlPoint(new Vector2(50, 50)),
                }, 100)
            });

            Assert.That(section(encode(beatmap), "HitObjects"), Is.EqualTo(new[] { "10,20,500,2,0,L|60:20|60:70,2,100,0|0|0,0:0|0:0|0:0,0:0:0:0:" }));
        }

        // CF: LegacyBeatmapEncoder.addPathData | case BSpline | case BSpline
        // CF: LegacyBeatmapEncoder.addPathData | Degree > 0 | TRUE
        // CF: LegacyBeatmapEncoder.addPathData | Degree > 0 | FALSE
        // CF: LegacyBeatmapEncoder.addPathData | case Catmull | case Catmull
        // CF: LegacyBeatmapEncoder.addPathData | case PerfectCurve | case PerfectCurve
        // CF: LegacyBeatmapEncoder.addPathData | ExpectedDistance.Value ?? Distance | null
        [Test]
        public void TestSliderPathTypesAndMultiSegment()
        {
            var beatmap = createBeatmap();
            beatmap.HitObjects.Add(new Slider
            {
                Position = Vector2.Zero,
                StartTime = 0,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.BSpline(3)),
                    new PathControlPoint(new Vector2(10, 0), PathType.BEZIER),
                    new PathControlPoint(new Vector2(20, 0), PathType.CATMULL),
                    new PathControlPoint(new Vector2(30, 0), PathType.PERFECT_CURVE),
                    new PathControlPoint(new Vector2(40, 0)),
                })
            });

            string line = section(encode(beatmap), "HitObjects").Single();

            Assert.That(line, Does.Contain(",B3|"));
            Assert.That(line, Does.Contain("|B|10:0"));
            Assert.That(line, Does.Contain("|C|20:0"));
            Assert.That(line, Does.Contain("|P|30:0"));
        }

        // CF: LegacyBeatmapEncoder.handleControlPoints | groupTimingPoint != null | TRUE
        // CF: LegacyBeatmapEncoder.handleControlPoints | controlPointProperties.IsRedundant(last) | TRUE
        // CF: LegacyBeatmapEncoder.handleControlPoints | foreach group | loop n
        [Test]
        public void TestTimingPointLine()
        {
            var beatmap = createBeatmap();
            beatmap.ControlPointInfo.Add(1000, new TimingControlPoint { BeatLength = 500 });
            beatmap.ControlPointInfo.Add(1000, new EffectControlPoint { KiaiMode = true });

            var lines = section(encode(beatmap), "TimingPoints");

            // kiai flag is never written on the timing point itself, so a separate inherited point carries it.
            Assert.That(lines[0], Does.StartWith("1000,500,4,"));
            Assert.That(lines[0], Does.EndWith(",1,0"));
            Assert.That(lines.Count, Is.EqualTo(2));
            Assert.That(lines[1], Does.StartWith("1000,-100,4,"));
            Assert.That(lines[1], Does.EndWith(",0,1"));
        }

        // CF: LegacyBeatmapEncoder.handleControlPoints | groupTimingPoint != null | FALSE
        // CF: LegacyBeatmapEncoder.handleControlPoints | controlPointProperties.IsRedundant(last) | FALSE
        // CF: LegacyBeatmapEncoder.getLegacyControlPointProperties | effectPoint.KiaiMode | FALSE
        // CF: LegacyBeatmapEncoder.getLegacyControlPointProperties | timingPoint.OmitFirstBarLine | TRUE
        [Test]
        public void TestOmitFirstBarLineFlagAndNonTimingGroup()
        {
            var beatmap = createBeatmap();
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500, OmitFirstBarLine = true });
            beatmap.ControlPointInfo.Add(2000, new EffectControlPoint { KiaiMode = true });

            var lines = section(encode(beatmap), "TimingPoints");

            Assert.That(lines[0], Does.EndWith(",1,8"));
            Assert.That(lines[1], Does.StartWith("2000,-100,"));
            Assert.That(lines[1], Does.EndWith(",0,9"), "kiai | omit-first-bar-line");
        }

        // CF: LegacyBeatmapEncoder.handleControlPoints | scrollSpeedEncodedAsSliderVelocity | TRUE
        // CF: LegacyBeatmapEncoder.collectDifficultyControlPoints | scrollSpeedEncodedAsSliderVelocity | TRUE
        [Test]
        public void TestTaikoScrollSpeedWrittenAsSliderVelocity()
        {
            var beatmap = createBeatmap(new TaikoRuleset().RulesetInfo);
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.ControlPointInfo.Add(1000, new EffectControlPoint { ScrollSpeed = 2 });

            var lines = section(encode(beatmap), "TimingPoints");

            Assert.That(lines.Any(l => l.StartsWith("1000,-50,")), Is.True);
        }

        // CF: LegacyBeatmapEncoder.handleControlPoints | scrollSpeedEncodedAsSliderVelocity | FALSE
        // CF: LegacyBeatmapEncoder.collectDifficultyControlPoints | hitObject is IHasSliderVelocity | TRUE
        // CF: LegacyBeatmapEncoder.extractDifficultyControlPoints | !hDifficultyPoint.IsRedundant(last) | TRUE
        // CF: LegacyBeatmapEncoder.extractDifficultyControlPoints | !hDifficultyPoint.IsRedundant(last) | FALSE
        [Test]
        public void TestHitObjectSliderVelocityBecomesDifficultyPoint()
        {
            var beatmap = createBeatmap();
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.Add(new Slider { Position = Vector2.Zero, StartTime = 1000, SliderVelocityMultiplier = 2, Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(10, 0) }, 10) });
            beatmap.HitObjects.Add(new Slider { Position = Vector2.Zero, StartTime = 2000, SliderVelocityMultiplier = 2, Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(10, 0) }, 10) });

            var lines = section(encode(beatmap), "TimingPoints");

            Assert.That(lines.Count(l => l.StartsWith("1000,-50,")), Is.EqualTo(1));
            Assert.That(lines.Any(l => l.StartsWith("2000,")), Is.False, "second identical velocity is redundant");
        }

        // CF: LegacyBeatmapEncoder.collectSampleControlPoints | hitObject.Samples.Count > 0 | TRUE
        // CF: LegacyBeatmapEncoder.collectSampleControlPoints | hitObject is IHasRepeats | FALSE
        // CF: LegacyBeatmapEncoder.createSampleControlPointFor | samples.Max volume | non-null
        // CF: LegacyBeatmapEncoder.extractSampleControlPoints | !hSamplePoint.IsRedundant(last) | TRUE
        [Test]
        public void TestHitObjectSamplesBecomeSampleControlPoints()
        {
            var beatmap = createBeatmap();
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.Add(new HitCircle
            {
                Position = Vector2.Zero,
                StartTime = 1000,
                Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_DRUM, volume: 40) }
            });

            var lines = section(encode(beatmap), "TimingPoints");
            // sample points before the first one fall back to it, so the timing point at 0 already carries the volume.
            Assert.That(lines.Single(), Does.Contain(",40,"), "volume");
        }

        // CF: LegacyBeatmapEncoder.collectSampleControlPoints | hitObject is IHasRepeats | TRUE
        // CF: LegacyBeatmapEncoder.collectSampleControlPoints | NodeSamples[i].Count > 0 | TRUE
        // CF: LegacyBeatmapEncoder.collectSampleControlPoints | NodeSamples[i].Count > 0 | FALSE
        // CF: LegacyBeatmapEncoder.collectSampleControlPoints | foreach nested | loop 0
        [Test]
        public void TestSliderNodeSamplesBecomeSampleControlPoints()
        {
            var beatmap = createBeatmap();
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            var nodeSlider = new Slider
            {
                Position = Vector2.Zero,
                StartTime = 1000,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(10, 0) }, 10),
                NodeSamples = new List<IList<HitSampleInfo>>
                {
                    new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_SOFT, volume: 30) },
                    new List<HitSampleInfo>(),
                }
            };
            nodeSlider.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
            beatmap.HitObjects.Add(nodeSlider);

            var lines = section(encode(beatmap), "TimingPoints");

            Assert.That(lines.Any(l => l.Contains(",30,")), Is.True);
        }

        // CF: LegacyBeatmapEncoder.handleHitObject | case 3 | case 3
        // CF: LegacyBeatmapEncoder.getObjectType | onlineRulesetID == 3 | TRUE
        // CF: LegacyBeatmapEncoder.addEndTimeData | type == Hold | TRUE
        // CF: LegacyBeatmapEncoder.getSampleBank | onlineRulesetID != 3 | FALSE
        [Test]
        public void TestManiaHoldNoteLine()
        {
            var beatmap = createBeatmap(new osu.Game.Rulesets.Mania.ManiaRuleset().RulesetInfo);
            beatmap.Difficulty.CircleSize = 4;
            beatmap.HitObjects.Add(new osu.Game.Rulesets.Mania.Objects.HoldNote
            {
                Column = 1,
                StartTime = 1000,
                Duration = 500,
                Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_NORMAL, volume: 70) }
            });

            // column 1 of 4: x = floor((1 + 0.5) * 512 / 4) = 192; hold type = 128; end time precedes sample data.
            Assert.That(section(encode(beatmap), "HitObjects"), Is.EqualTo(new[] { "192,192,1000,128,0,1500:1:0:0:70:" }));
        }

        // CF: LegacyBeatmapEncoder.handleHitObject | case 2 | case 2
        [Test]
        public void TestCatchUsesObjectPosition()
        {
            var beatmap = createBeatmap(new osu.Game.Rulesets.Catch.CatchRuleset().RulesetInfo);
            beatmap.HitObjects.Add(new osu.Game.Rulesets.Catch.Objects.Fruit { OriginalX = 77, StartTime = 100 });

            Assert.That(section(encode(beatmap), "HitObjects").Single(), Does.StartWith("77,"));
        }

        // CF: LegacyBeatmapEncoder.Encode | decode(encode(x)) == x | no throw
        [Test]
        public void TestRoundTripThroughDecoder()
        {
            const string source = @"osu file format v14

[General]
AudioFilename: a.mp3
Mode: 0

[Metadata]
Title:T
Artist:A
Creator:C
Version:V

[Difficulty]
HPDrainRate:4
CircleSize:5
OverallDifficulty:6
ApproachRate:7
SliderMultiplier:1.4
SliderTickRate:2

[TimingPoints]
0,500,4,1,0,100,1,0

[HitObjects]
100,100,1000,5,0
0,0,2000,2,0,L|100:0,1,100
256,192,3000,8,0,4000
";
            using var input = new MemoryStream(Encoding.UTF8.GetBytes(source));
            using var reader = new LineBufferedReader(input);
            var original = new LegacyBeatmapDecoder(14).Decode(reader);

            using var writer = new StringWriter();
            new LegacyBeatmapEncoder(original, null, null).Encode(writer);

            using var encoded = new MemoryStream(Encoding.UTF8.GetBytes(writer.ToString()));
            using var encodedReader = new LineBufferedReader(encoded);
            var decoded = new LegacyBeatmapDecoder(LegacyBeatmapEncoder.FIRST_LAZER_VERSION).Decode(encodedReader);

            Assert.That(decoded.Metadata.Title, Is.EqualTo("T"));
            Assert.That(decoded.Difficulty.ApproachRate, Is.EqualTo(7));
            Assert.That(decoded.Difficulty.SliderMultiplier, Is.EqualTo(1.4));
            Assert.That(decoded.ControlPointInfo.TimingPoints.Single().BeatLength, Is.EqualTo(500));
            Assert.That(decoded.HitObjects.Select(h => h.StartTime), Is.EqualTo(new[] { 1000.0, 2000, 3000 }));
            Assert.That(decoded.HitObjects.Select(h => h.GetType()), Is.EqualTo(original.HitObjects.Select(h => h.GetType())));
            Assert.That(((IHasPath)decoded.HitObjects[1]).Path.Distance, Is.EqualTo(100).Within(1e-3));
            Assert.That(((IHasDuration)decoded.HitObjects[2]).EndTime, Is.EqualTo(4000));
        }
    }
}
