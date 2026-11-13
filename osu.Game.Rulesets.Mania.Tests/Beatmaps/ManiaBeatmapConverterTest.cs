// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring.Legacy;
using osuTK;

namespace osu.Game.Rulesets.Mania.Tests.Beatmaps
{
    [TestFixture]
    public class ManiaBeatmapConverterTest
    {
        private static readonly RulesetInfo other_ruleset = new RulesetInfo { OnlineID = 0, ShortName = "osu" };

        private class FakeCircle : HitObject, IHasPosition
        {
            public float X { get; set; }
            public float Y { get; set; }

            public Vector2 Position
            {
                get => new Vector2(X, Y);
                set { X = value.X; Y = value.Y; }
            }
        }

        private class FakeSlider : FakeCircle, IHasPath
        {
            public double Duration { get; set; }
            public double EndTime => StartTime + Duration;
            public double Distance => Path.Distance;
            public SliderPath Path { get; } = new SliderPath(PathType.LINEAR, [Vector2.Zero, new Vector2(200, 0)]);
        }

        private class FakeSpinner : FakeCircle, IHasDuration, IHasLegacyHitObjectType
        {
            public double Duration { get; set; }
            public double EndTime => StartTime + Duration;
            public LegacyHitObjectType LegacyType { get; init; } = LegacyHitObjectType.Spinner;
        }

        private class FakeHold : HitObject, IHasDuration, IHasXPosition
        {
            public double Duration { get; set; }
            public double EndTime => StartTime + Duration;
            public float X { get; set; }
        }

        private class FakeNoPosition : HitObject
        {
        }

        private static IBeatmap beatmap(RulesetInfo ruleset, float cs, params HitObject[] objects)
        {
            var b = new Beatmap<HitObject> { HitObjects = objects.ToList() };
            b.BeatmapInfo.Ruleset = ruleset;
            b.Difficulty = new BeatmapDifficulty { CircleSize = cs, OverallDifficulty = 5, DrainRate = 5, ApproachRate = 5 };
            return b;
        }

        private static ManiaBeatmap convert(IBeatmap source, params Mod[] mods)
        {
            var converter = new ManiaBeatmapConverter(source, new ManiaRuleset());
            foreach (var m in mods.OfType<IApplicableToBeatmapConverter>())
                m.ApplyToBeatmapConverter(converter);
            return (ManiaBeatmap)converter.Convert();
        }

        private static LegacyBeatmapConversionDifficultyInfo info(bool mania, float cs, float od = 5, int total = 0, int ends = -1) => new LegacyBeatmapConversionDifficultyInfo
        {
            SourceRuleset = mania ? new ManiaRuleset().RulesetInfo : other_ruleset,
            CircleSize = cs,
            OverallDifficulty = od,
            TotalObjectCount = total,
            EndTimeObjectCount = ends
        };

        // CF: ManiaBeatmapConverter.getColumnCount | difficulty.SourceRuleset.ShortName == mania | TRUE
        // CF: ManiaBeatmapConverter.getColumnCount | Math.Max(1, roundedCircleSize) | non-null
        // CF: ManiaBeatmapConverter..ctor | IsForCurrentRuleset && TargetColumns > MAX_STAGE_KEYS | FALSE
        [TestCase(4f, 4)]
        [TestCase(0f, 1)]
        [TestCase(9f, 9)]
        public void TestNativeColumnCount(float cs, int expected)
        {
            Assert.That(ManiaBeatmapConverter.GetColumnCount(info(true, cs)), Is.EqualTo(expected));
        }

        // CF: ManiaBeatmapConverter..ctor | IsForCurrentRuleset && TargetColumns > MAX_STAGE_KEYS | TRUE
        // CF: ManiaBeatmapConverter.TotalColumns | Dual ? 2 : 1 | TRUE
        [Test]
        public void TestNativeOverMaxKeysBecomesDualStage()
        {
            Assert.That(ManiaBeatmapConverter.GetColumnCount(info(true, 12)), Is.EqualTo(12));

            var converted = convert(beatmap(new ManiaRuleset().RulesetInfo, 12));
            Assert.That(converted.Stages.Select(s => s.Columns), Is.EqualTo(new[] { 6, 6 }));
        }

        // CF: ManiaBeatmapConverter.CreateBeatmap | Dual | FALSE
        // CF: ManiaBeatmapConverter.TotalColumns | Dual ? 2 : 1 | FALSE
        [Test]
        public void TestSingleStage()
        {
            Assert.That(convert(beatmap(new ManiaRuleset().RulesetInfo, 4)).Stages.Select(s => s.Columns), Is.EqualTo(new[] { 4 }));
        }

        // CF: ManiaBeatmapConverter.getColumnCount | TotalObjectCount > 0 && EndTimeObjectCount >= 0 | TRUE
        // CF: ManiaBeatmapConverter.getColumnCount | percentSpecialObjects < 0.2 | TRUE
        [Test]
        public void TestFewSpecialObjectsGiveSevenKeys() => Assert.That(ManiaBeatmapConverter.GetColumnCount(info(false, 4, 5, 100, 10)), Is.EqualTo(7));

        // CF: ManiaBeatmapConverter.getColumnCount | percentSpecialObjects < 0.2 | FALSE
        // CF: ManiaBeatmapConverter.getColumnCount | percentSpecialObjects < 0.3 | TRUE
        // CF: ManiaBeatmapConverter.getColumnCount | roundedOverallDifficulty > 5 | TRUE
        // CF: ManiaBeatmapConverter.getColumnCount | roundedOverallDifficulty > 5 | FALSE
        [TestCase(6f, 7)]
        [TestCase(5f, 6)]
        public void TestSomeSpecialObjects(float od, int expected) => Assert.That(ManiaBeatmapConverter.GetColumnCount(info(false, 4, od, 100, 25)), Is.EqualTo(expected));

        // CF: ManiaBeatmapConverter.getColumnCount | percentSpecialObjects < 0.3 | FALSE
        // CF: ManiaBeatmapConverter.getColumnCount | roundedCircleSize >= 5 | TRUE
        [Test]
        public void TestLargeCircleSizeForcesSixOrSeven() => Assert.That(ManiaBeatmapConverter.GetColumnCount(info(false, 5, 5, 100, 45)), Is.EqualTo(6));

        // CF: ManiaBeatmapConverter.getColumnCount | roundedCircleSize >= 5 | FALSE
        // CF: ManiaBeatmapConverter.getColumnCount | percentSpecialObjects > 0.6 | TRUE
        // CF: ManiaBeatmapConverter.getColumnCount | roundedOverallDifficulty > 4 | TRUE
        // CF: ManiaBeatmapConverter.getColumnCount | roundedOverallDifficulty > 4 | FALSE
        [TestCase(5f, 5)]
        [TestCase(4f, 4)]
        public void TestManySpecialObjects(float od, int expected) => Assert.That(ManiaBeatmapConverter.GetColumnCount(info(false, 4, od, 100, 70)), Is.EqualTo(expected));

        // CF: ManiaBeatmapConverter.getColumnCount | percentSpecialObjects > 0.6 | FALSE
        [TestCase(5f, 6)]
        [TestCase(1f, 4)]
        [TestCase(9f, 7)]
        public void TestFallbackDependsOnOverallDifficulty(float od, int expected) => Assert.That(ManiaBeatmapConverter.GetColumnCount(info(false, 4, od, 100, 45)), Is.EqualTo(expected));

        // CF: ManiaBeatmapConverter.getColumnCount | TotalObjectCount > 0 | FALSE
        // CF: ManiaBeatmapConverter.getColumnCount | EndTimeObjectCount >= 0 | FALSE
        [Test]
        public void TestMissingObjectCountsFallBack()
        {
            Assert.That(ManiaBeatmapConverter.GetColumnCount(info(false, 4, 5, 0, 0)), Is.EqualTo(6));
            Assert.That(ManiaBeatmapConverter.GetColumnCount(info(false, 4, 5, 100, -1)), Is.EqualTo(6));
        }

        // CF: ManiaBeatmapConverter.GetColumnCount | mods != null | TRUE
        // CF: ManiaBeatmapConverter.GetColumnCount | foreach IApplicableToBeatmapConverter mod | loop 1
        // CF: ManiaBeatmapConverter.GetColumnCount | mods != null | FALSE
        // CF: ManiaBeatmapConverter.GetColumnCount | foreach IApplicableToBeatmapConverter mod | loop 0
        [Test]
        public void TestGetColumnCountWithMods()
        {
            Assert.That(ManiaBeatmapConverter.GetColumnCount(info(false, 4), [new ManiaModKey8()]), Is.EqualTo(8));
            Assert.That(ManiaBeatmapConverter.GetColumnCount(info(false, 4), []), Is.EqualTo(6));
            Assert.That(ManiaBeatmapConverter.GetColumnCount(info(false, 4), null), Is.EqualTo(6));
        }

        // CF: ManiaBeatmapConverter.CanConvert | Beatmap.HitObjects.All(IHasXPosition) | TRUE
        // CF: ManiaBeatmapConverter.CanConvert | Beatmap.HitObjects.All(IHasXPosition) | FALSE
        [Test]
        public void TestCanConvert()
        {
            Assert.That(new ManiaBeatmapConverter(beatmap(other_ruleset, 4, new FakeCircle()), new ManiaRuleset()).CanConvert(), Is.True);
            Assert.That(new ManiaBeatmapConverter(beatmap(other_ruleset, 4, new FakeNoPosition()), new ManiaRuleset()).CanConvert(), Is.False);
        }

        // CF: ManiaBeatmapConverter.ConvertHitObject | case ManiaHitObject | case ManiaHitObject
        [Test]
        public void TestManiaObjectsPassThroughUnchanged()
        {
            var note = new Note { StartTime = 100, Column = 2 };
            var converted = convert(beatmap(new ManiaRuleset().RulesetInfo, 4, note));
            Assert.That(converted.HitObjects, Is.EqualTo(new[] { note }));
        }

        private static void assertValid(ManiaBeatmap b)
        {
            Assert.That(b.HitObjects, Is.Not.Empty);
            Assert.That(b.HitObjects.Select(h => h.Column), Is.All.InRange(0, b.TotalColumns - 1));
        }

        // CF: ManiaBeatmapConverter.ConvertHitObject | default legacy type (Circle) | default
        // CF: ManiaBeatmapConverter.ConvertHitObject | legacyType == Circle && !IsForCurrentRuleset | HitCirclePatternGenerator
        // CF: ManiaBeatmapConverter.ConvertHitObject | foreach newPattern | loop n
        // CF: ManiaBeatmapConverter.ConvertHitObject | conversion is HitCirclePatternGenerator | TRUE
        [Test]
        public void TestConvertedCircles()
        {
            var converted = convert(beatmap(other_ruleset, 4,
                new FakeCircle { StartTime = 0, X = 64 }, new FakeCircle { StartTime = 200, X = 256 }, new FakeCircle { StartTime = 400, X = 448 }));

            assertValid(converted);
            Assert.That(converted.HitObjects.OfType<Note>().Count(), Is.GreaterThanOrEqualTo(3));
        }

        // CF: ManiaBeatmapConverter.ConvertHitObject | case Circle && IsForCurrentRuleset | TRUE
        // CF: ManiaBeatmapConverter.ConvertHitObject | conversion is HitCirclePatternGenerator | FALSE
        // CF: ManiaBeatmapConverter.ConvertHitObject | conversion is HitCirclePatternGenerator || SliderPatternGenerator | FALSE
        [Test]
        public void TestPassThroughCircleUsesXPositionForColumn()
        {
            var converted = convert(beatmap(new ManiaRuleset().RulesetInfo, 4, new FakeCircle { StartTime = 0, X = 300 }));

            Assert.That(converted.HitObjects, Has.Count.EqualTo(1));
            Assert.That(converted.HitObjects[0], Is.TypeOf<Note>());
            Assert.That(converted.HitObjects[0].Column, Is.EqualTo(2)); // 300 / (512 / 4)
        }

        // CF: ManiaBeatmapConverter.ConvertHitObject | case Slider && !IsForCurrentRuleset | SliderPatternGenerator
        // CF: ManiaBeatmapConverter.ConvertHitObject | for i <= generator.SpanCount | loop n
        // CF: ManiaBeatmapConverter.ConvertHitObject | conversion is SliderPatternGenerator | TRUE
        [Test]
        public void TestConvertedSlider()
        {
            var slider = new FakeSlider { StartTime = 1000, X = 100, Y = 100, Duration = 1000 };
            var converted = convert(beatmap(other_ruleset, 4, slider));

            assertValid(converted);
            Assert.That(converted.HitObjects.All(h => h.StartTime >= 1000), Is.True);
        }

        // CF: ManiaBeatmapConverter.ConvertHitObject | case Slider && IsForCurrentRuleset | TRUE
        [Test]
        public void TestPassThroughSliderBecomesHoldNote()
        {
            var slider = new FakeSlider { StartTime = 1000, X = 100, Duration = 500 };
            var converted = convert(beatmap(new ManiaRuleset().RulesetInfo, 4, slider));

            Assert.That(converted.HitObjects, Has.Count.EqualTo(1));
            var hold = (HoldNote)converted.HitObjects[0];
            Assert.That(hold.StartTime, Is.EqualTo(1000));
            Assert.That(hold.EndTime, Is.EqualTo(1500));
            Assert.That(hold.PlaySlidingSamples, Is.True);
        }

        // CF: ManiaBeatmapConverter.ConvertHitObject | case Spinner | case Spinner
        [Test]
        public void TestSpinnerBecomesHold()
        {
            var converted = convert(beatmap(other_ruleset, 4, new FakeSpinner { StartTime = 500, Duration = 2000 }));

            assertValid(converted);
            Assert.That(converted.HitObjects.OfType<HoldNote>().Any(h => h.StartTime == 500 && h.EndTime == 2500), Is.True);
        }

        // CF: ManiaBeatmapConverter.ConvertHitObject | case IHasDuration (no legacy type) -> Hold | case Hold
        [Test]
        public void TestDurationObjectBecomesHold()
        {
            var converted = convert(beatmap(other_ruleset, 4, new FakeHold { StartTime = 100, Duration = 400, X = 0 }));

            Assert.That(converted.HitObjects, Has.Count.EqualTo(1));
            Assert.That(converted.HitObjects[0], Is.TypeOf<HoldNote>());
        }

        // CF: ManiaBeatmapConverter.ConvertHitObject | legacyType switch no case matches | default
        // CF: ManiaBeatmapConverter.ConvertHitObject | legacyType switch no case matches | throws ArgumentException
        [Test]
        public void TestInvalidLegacyTypeThrows()
        {
            var bad = new FakeSpinner { LegacyType = LegacyHitObjectType.Circle | LegacyHitObjectType.Slider };
            Assert.That(() => convert(beatmap(other_ruleset, 4, bad)), Throws.TypeOf<ArgumentException>());
        }

        // CF: ManiaBeatmapConverter.ConvertHitObject | case IHasPath (no legacy type) | case Slider
        // CF: ManiaBeatmapConverter.ConvertHitObject | case IHasLegacyHitObjectType | case Circle (legacy flag)
        [Test]
        public void TestLegacyTypeFlagsAreMasked()
        {
            var spinner = new FakeSpinner { StartTime = 0, Duration = 1000, LegacyType = LegacyHitObjectType.Spinner | LegacyHitObjectType.NewCombo };
            assertValid(convert(beatmap(other_ruleset, 4, spinner)));
        }
    }
}
