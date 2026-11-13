// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Objects;
using osuTK;

namespace osu.Game.Rulesets.Taiko.Tests.Beatmaps
{
    [TestFixture]
    public class TaikoBeatmapConverterTest
    {
        private class FakeCircle : HitObject
        {
        }

        private class FakeSpinner : HitObject, IHasDuration
        {
            public double EndTime => StartTime + Duration;
            public double Duration { get; set; }
        }

        private class FakePath : HitObject, IHasPath
        {
            public double Distance { get; set; }
            public double EndTime => StartTime;
            public double Duration { get; set; }
            public SliderPath Path => new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2((float)Distance, 0) }, Distance);
        }

        private class FakeSlider : FakePath, IHasPathWithRepeats, IHasSliderVelocity
        {
            public int RepeatCount { get; set; }
            public IList<IList<HitSampleInfo>> NodeSamples { get; } = new List<IList<HitSampleInfo>>
            {
                new List<HitSampleInfo>(),
                new List<HitSampleInfo>(),
            };
            public double SliderVelocityMultiplier { get; set; } = 1;
            public BindableNumber<double> SliderVelocityMultiplierBindable { get; } = new BindableDouble(1);
        }

        private static readonly RulesetInfo osu_ruleset = new RulesetInfo { OnlineID = 0, ShortName = "osu", Name = "osu!" };
        private static readonly RulesetInfo mania_ruleset = new RulesetInfo { OnlineID = 3, ShortName = "mania", Name = "mania" };

        private static IBeatmap convert(RulesetInfo ruleset, int version, params HitObject[] objects)
        {
            var beatmap = new Beatmap<HitObject>
            {
                BeatmapInfo = { Ruleset = ruleset },
                BeatmapVersion = version,
                Difficulty = new BeatmapDifficulty { SliderMultiplier = 1.4, SliderTickRate = 1, OverallDifficulty = 5 },
            };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            beatmap.HitObjects.AddRange(objects);
            return new TaikoRuleset().CreateBeatmapConverter(beatmap).Convert();
        }

        private static IBeatmap convert(RulesetInfo ruleset, params HitObject[] objects) => convert(ruleset, 14, objects);

        // CF: TaikoBeatmapConverter.CanConvert | always | TRUE
        [Test]
        public void TestCanConvert() => Assert.That(new TaikoRuleset().CreateBeatmapConverter(new Beatmap<HitObject>()).CanConvert(), Is.True);

        // CF: TaikoBeatmapConverter.ConvertHitObject | switch (obj) | default
        [Test]
        public void TestCircleBecomesHitWithSamples()
        {
            var circle = new FakeCircle { StartTime = 100, Samples = { new HitSampleInfo(HitSampleInfo.HIT_CLAP) } };
            var hit = (Hit)convert(osu_ruleset, circle).HitObjects.Single();
            Assert.That(hit.StartTime, Is.EqualTo(100));
            Assert.That(hit.Type, Is.EqualTo(HitType.Rim));
        }

        // CF: TaikoBeatmapConverter.ConvertHitObject | case IHasDuration | case IHasDuration
        // CF: TaikoBeatmapConverter.ConvertHitObject | Math.Max(1, duration / 1000 * hitMultiplier) | FALSE
        [Test]
        public void TestSpinnerBecomesSwell()
        {
            var swell = (Swell)convert(osu_ruleset, new FakeSpinner { StartTime = 0, Duration = 2000 }).HitObjects.Single();
            Assert.That(swell.RequiredHits, Is.EqualTo(16)); // 2s * (5 * 1.65) = 16.5
            Assert.That(swell.Duration, Is.EqualTo(2000));
        }

        // CF: TaikoBeatmapConverter.ConvertHitObject | Math.Max(1, duration / 1000 * hitMultiplier) | TRUE
        [Test]
        public void TestShortSpinnerRequiresAtLeastOneHit()
        {
            var swell = (Swell)convert(osu_ruleset, new FakeSpinner { Duration = 10 }).HitObjects.Single();
            Assert.That(swell.RequiredHits, Is.EqualTo(1));
        }

        [Test]
        public void TestRequiredSwellHitsPerSecondByDifficulty()
        {
            Assert.That(TaikoBeatmapConverter.RequiredSwellHitsPerSecond(0), Is.EqualTo(3 * 1.65f).Within(1e-6));
            Assert.That(TaikoBeatmapConverter.RequiredSwellHitsPerSecond(10), Is.EqualTo(7.5 * 1.65f).Within(1e-6));
        }

        // CF: TaikoBeatmapConverter.ConvertHitObject | case IHasPath | case IHasPath
        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | return tickSpacing > 0 && distance/osuVelocity*1000 < 2*beatLength | TRUE
        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | obj is IHasSliderVelocity | TRUE
        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | beatmap.BeatmapVersion >= 8 | TRUE
        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | isForCurrentRuleset | FALSE
        // CF: TaikoBeatmapConverter.ConvertHitObject | obj is IHasPathWithRepeats | TRUE
        // CF: TaikoBeatmapConverter.ConvertHitObject | for j <= StartTime + taikoDuration + tickSpacing / 8 | loop n
        // CF: TaikoBeatmapConverter.ConvertHitObject | Precision.AlmostEquals(0, tickSpacing) | FALSE
        [Test]
        public void TestShortSliderWithRepeatBecomesHitsWithNodeSamples()
        {
            var slider = new FakeSlider { Distance = 100, RepeatCount = 1 };
            slider.NodeSamples.Clear();
            slider.NodeSamples.Add(new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) });
            slider.NodeSamples.Add(new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_CLAP) });

            var hits = convert(osu_ruleset, slider).HitObjects.Cast<Hit>().ToList();

            // distance 100 * 1.4 * 2 spans = 280 -> duration 714, spacing min(500, 357) = 357
            Assert.That(hits.Select(h => h.StartTime), Is.EqualTo(new[] { 0d, 357d, 714d }));
            // node index wraps around after the last node sample set
            Assert.That(hits.Select(h => h.Type), Is.EqualTo(new[] { HitType.Centre, HitType.Rim, HitType.Centre }));
        }

        // CF: TaikoBeatmapConverter.ConvertHitObject | obj is IHasPathWithRepeats | FALSE
        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | obj is IHasSliderVelocity | FALSE
        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | (obj as IHasRepeats)?.SpanCount() ?? 1 | null
        // CF: TaikoBeatmapConverter.ConvertHitObject | for j <= StartTime + taikoDuration + tickSpacing / 8 | loop n
        [Test]
        public void TestPathWithoutRepeatsOrVelocityBecomesHits()
        {
            var hits = convert(osu_ruleset, new FakePath { Distance = 100 }).HitObjects.Cast<Hit>().ToList();
            Assert.That(hits.Select(h => h.StartTime), Is.EqualTo(new[] { 0d, 357d }));
        }

        // CF: TaikoBeatmapConverter.ConvertHitObject | Precision.AlmostEquals(0, tickSpacing) | TRUE
        // CF: TaikoBeatmapConverter.ConvertHitObject | for j <= StartTime + taikoDuration + tickSpacing / 8 | loop 1
        [Test]
        public void TestVanishingTickSpacingYieldsSingleHit()
        {
            var beatmap = new Beatmap<HitObject> { BeatmapInfo = { Ruleset = osu_ruleset }, Difficulty = new BeatmapDifficulty { SliderMultiplier = 1.4, SliderTickRate = 100000000 } };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 6 });
            beatmap.HitObjects.Add(new FakePath { Distance = 100 });

            var hits = new TaikoRuleset().CreateBeatmapConverter(beatmap).Convert().HitObjects;
            Assert.That(hits, Has.Count.EqualTo(1));
            Assert.That(hits[0], Is.TypeOf<Hit>());
        }

        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | tickSpacing > 0 && distance/osuVelocity*1000 < 2*beatLength | FALSE
        [Test]
        public void TestLongSliderBecomesDrumRoll()
        {
            var roll = (DrumRoll)convert(osu_ruleset, new FakePath { Distance = 1000 }).HitObjects.Single();
            Assert.That(roll.Duration, Is.EqualTo(3571)); // (int)(1400 / 196 * 500)
        }

        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | tickSpacing > 0 | FALSE
        [Test]
        public void TestZeroLengthSliderBecomesDrumRoll()
        {
            var roll = (DrumRoll)convert(osu_ruleset, new FakePath { Distance = 0 }).HitObjects.Single();
            Assert.That(roll.Duration, Is.EqualTo(0));
        }

        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | isForCurrentRuleset | TRUE
        [Test]
        public void TestSliderInTaikoBeatmapAlwaysDrumRoll()
        {
            var result = convert(new TaikoRuleset().RulesetInfo, new FakePath { Distance = 100 });
            Assert.That(result.HitObjects.Single(), Is.TypeOf<DrumRoll>());
        }

        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | beatmap.BeatmapVersion >= 8 | TRUE
        [Test]
        public void TestNewVersionUsesUnadjustedBeatLengthForSpacing()
        {
            var result = convert(osu_ruleset, 14, new FakeSlider { Distance = 400, SliderVelocityMultiplier = 2 });
            Assert.That(result.HitObjects.Select(h => h.GetType()), Is.All.EqualTo(typeof(Hit)));
        }

        // CF: TaikoBeatmapConverter.shouldConvertSliderToHits | beatmap.BeatmapVersion >= 8 | FALSE
        [Test]
        public void TestOldVersionUsesSpeedAdjustedBeatLengthForSpacing()
        {
            var result = convert(osu_ruleset, 6, new FakeSlider { Distance = 400, SliderVelocityMultiplier = 2 });
            Assert.That(result.HitObjects.Single(), Is.TypeOf<DrumRoll>());
        }

        // CF: TaikoBeatmapConverter.ConvertBeatmap | original.BeatmapInfo.Ruleset.OnlineID == 0 | TRUE
        // CF: TaikoBeatmapConverter.ConvertBeatmap | hitObject is not IHasSliderVelocity | FALSE
        // CF: TaikoBeatmapConverter.ConvertBeatmap | !Precision.AlmostEquals(lastScrollSpeed, nextScrollSpeed) | TRUE
        [Test]
        public void TestSliderVelocityChangeBecomesScrollSpeedPoint()
        {
            var result = convert(osu_ruleset, new FakeSlider { StartTime = 1000, Distance = 1000, SliderVelocityMultiplier = 2 });
            Assert.That(result.ControlPointInfo.EffectPointAt(1000).ScrollSpeed, Is.EqualTo(2));
            Assert.That(result.ControlPointInfo.EffectPointAt(500).ScrollSpeed, Is.EqualTo(1));
        }

        // CF: TaikoBeatmapConverter.ConvertBeatmap | !Precision.AlmostEquals(lastScrollSpeed, nextScrollSpeed) | FALSE
        // CF: TaikoBeatmapConverter.ConvertBeatmap | hitObject is not IHasSliderVelocity | TRUE
        // CF: TaikoBeatmapConverter.ConvertBeatmap | foreach original.HitObjects | loop n
        [Test]
        public void TestUnchangedVelocityAndCirclesAddNoPoints()
        {
            var result = convert(osu_ruleset, new FakeCircle { StartTime = 500 }, new FakeSlider { StartTime = 1000, Distance = 1000 });
            Assert.That(result.ControlPointInfo.EffectPoints, Is.Empty);
        }

        // CF: TaikoBeatmapConverter.ConvertBeatmap | original.BeatmapInfo.Ruleset.OnlineID == 0 | FALSE
        // CF: TaikoBeatmapConverter.ConvertBeatmap | original.BeatmapInfo.Ruleset.OnlineID == 3 | FALSE
        [Test]
        public void TestTaikoSourceSkipsPostProcessing()
        {
            var result = convert(new TaikoRuleset().RulesetInfo, new FakeCircle { StartTime = 100 }, new FakeCircle { StartTime = 100 });
            Assert.That(result.HitObjects, Has.Count.EqualTo(2));
        }

        // CF: TaikoBeatmapConverter.ConvertBeatmap | original.BeatmapInfo.Ruleset.OnlineID == 3 | TRUE
        // CF: TaikoBeatmapConverter.ConvertBeatmap | x.Skip(1).Any() | TRUE
        // CF: TaikoBeatmapConverter.ConvertBeatmap | first is TaikoStrongableHitObject | TRUE
        // CF: TaikoBeatmapConverter.ConvertBeatmap | x.Skip(1).Any() | FALSE
        [Test]
        public void TestManiaSimultaneousObjectsBecomeStrongHit()
        {
            var result = convert(mania_ruleset, new FakeCircle { StartTime = 100 }, new FakeCircle { StartTime = 100 }, new FakeCircle { StartTime = 200 });
            var hits = result.HitObjects.Cast<Hit>().ToList();
            Assert.That(hits, Has.Count.EqualTo(2));
            Assert.That(hits.Select(h => h.IsStrong), Is.EqualTo(new[] { true, false }));
        }

        // CF: TaikoBeatmapConverter.ConvertBeatmap | first is TaikoStrongableHitObject | FALSE
        [Test]
        public void TestManiaSimultaneousSwellsNotStrong()
        {
            var result = convert(mania_ruleset, new FakeSpinner { StartTime = 100, Duration = 500 }, new FakeSpinner { StartTime = 100, Duration = 500 });
            Assert.That(result.HitObjects.Single(), Is.TypeOf<Swell>());
        }
    }
}
