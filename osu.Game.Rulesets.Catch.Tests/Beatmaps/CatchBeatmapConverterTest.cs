// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Catch.Tests.Beatmaps
{
    [TestFixture]
    public class CatchBeatmapConverterTest
    {
        /// <summary>An osu!-like circle with full position and combo data.</summary>
        private class StubCircle : HitObject, IHasPosition, IHasCombo
        {
            public float X { get; set; }
            public float Y { get; set; }

            public Vector2 Position
            {
                get => new Vector2(X, Y);
                set => (X, Y) = (value.X, value.Y);
            }

            public bool NewCombo { get; set; }
            public int ComboOffset { get; set; }
        }

        /// <summary>An osu!-like slider with full position, combo and velocity data.</summary>
        private class StubSlider : BarePathHit, IHasPosition, IHasCombo, IHasSliderVelocity
        {
            public float X { get; set; }
            public float Y { get; set; }

            public Vector2 Position
            {
                get => new Vector2(X, Y);
                set => (X, Y) = (value.X, value.Y);
            }

            public bool NewCombo { get; set; }
            public int ComboOffset { get; set; }
            public double SliderVelocityMultiplier { get; set; } = 1;
            public BindableNumber<double> SliderVelocityMultiplierBindable { get; } = new BindableDouble(1);
        }

        /// <summary>An osu!-like spinner with combo data.</summary>
        private class StubSpinner : BareDurationHit, IHasCombo
        {
            public bool NewCombo { get; set; }
            public int ComboOffset { get; set; }
        }

        /// <summary>A hit object with nothing but a position.</summary>
        private class PositionOnlyHit : HitObject, IHasXPosition
        {
            public float X { get; set; }
        }

        /// <summary>A hit object with no position information at all.</summary>
        private class BareHit : HitObject
        {
        }

        /// <summary>A hit object with a duration and no combo or position data.</summary>
        private class BareDurationHit : HitObject, IHasDuration
        {
            public double EndTime => StartTime + Duration;
            public double Duration { get; set; }
        }

        /// <summary>A path hit object with no position, combo or velocity data.</summary>
        private class BarePathHit : HitObject, IHasPathWithRepeats
        {
            public double EndTime => StartTime + Duration;
            public double Duration { get; set; }
            public double Distance => 100;
            public SliderPath Path { get; } = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(10, 0) });
            public int RepeatCount { get; set; }
            public IList<IList<HitSampleInfo>> NodeSamples { get; } = new List<IList<HitSampleInfo>>();
        }

        private static IBeatmap convert(int beatmapVersion, params HitObject[] hitObjects)
        {
            var beatmap = new Beatmap { BeatmapVersion = beatmapVersion, ControlPointInfo = new LegacyControlPointInfo() };
            beatmap.HitObjects.AddRange(hitObjects);
            return new CatchBeatmapConverter(beatmap, new CatchRuleset()).Convert();
        }

        private static CatchBeatmapConverter createConverter(params HitObject[] hitObjects)
        {
            var beatmap = new Beatmap();
            beatmap.HitObjects.AddRange(hitObjects);
            return new CatchBeatmapConverter(beatmap, new CatchRuleset());
        }

        // CF: CatchBeatmapConverter.CanConvert | All(h is IHasXPosition) | TRUE
        [Test]
        public void TestCanConvertWhenAllObjectsHaveXPosition()
        {
            Assert.That(createConverter(new PositionOnlyHit(), new StubCircle()).CanConvert(), Is.True);
            Assert.That(createConverter().CanConvert(), Is.True);
        }

        // CF: CatchBeatmapConverter.CanConvert | All(h is IHasXPosition) | FALSE
        [Test]
        public void TestCannotConvertWhenAnyObjectLacksXPosition()
        {
            Assert.That(createConverter(new PositionOnlyHit(), new BareHit()).CanConvert(), Is.False);
        }

        // CF: CatchBeatmapConverter.ConvertHitObject | switch obj | default
        // CF: CatchBeatmapConverter.ConvertHitObject | xPositionData?.X ?? 0 | non-null
        // CF: CatchBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | non-null
        // CF: CatchBeatmapConverter.ConvertHitObject | yPositionData?.Y ?? DEFAULT_LEGACY_CONVERT_Y | non-null
        [Test]
        public void TestConvertCircleCopiesData()
        {
            var source = new StubCircle { StartTime = 500, X = 123, Y = 77, NewCombo = true, ComboOffset = 2 };

            var converted = convert(8, source).HitObjects.Single();

            Assert.That(converted, Is.TypeOf<Fruit>());
            var fruit = (Fruit)converted;
            Assert.That(fruit.StartTime, Is.EqualTo(500));
            Assert.That(fruit.OriginalX, Is.EqualTo(123));
            Assert.That(fruit.NewCombo, Is.True);
            Assert.That(fruit.ComboOffset, Is.EqualTo(2));
            Assert.That(fruit.LegacyConvertedY, Is.EqualTo(77));
        }

        // CF: CatchBeatmapConverter.ConvertHitObject | xPositionData?.X ?? 0 | null
        // CF: CatchBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | null
        // CF: CatchBeatmapConverter.ConvertHitObject | yPositionData?.Y ?? DEFAULT_LEGACY_CONVERT_Y | null
        [Test]
        public void TestConvertObjectWithoutExtraDataUsesDefaults()
        {
            var fruit = (Fruit)convert(8, new BareHit { StartTime = 10 }).HitObjects.Single();

            Assert.That(fruit.OriginalX, Is.Zero);
            Assert.That(fruit.NewCombo, Is.False);
            Assert.That(fruit.ComboOffset, Is.Zero);
            Assert.That(fruit.LegacyConvertedY, Is.EqualTo(CatchHitObject.DEFAULT_LEGACY_CONVERT_Y));
        }

        // CF: CatchBeatmapConverter.ConvertHitObject | switch obj | case IHasDuration
        // CF: CatchBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | non-null (banana shower)
        [Test]
        public void TestConvertSpinnerLikeObjectToBananaShower()
        {
            var shower = (BananaShower)convert(8, new StubSpinner { StartTime = 100, Duration = 900, NewCombo = true }).HitObjects.Single();

            Assert.That(shower.StartTime, Is.EqualTo(100));
            Assert.That(shower.Duration, Is.EqualTo(900));
            Assert.That(shower.NewCombo, Is.True);
        }

        // CF: CatchBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | null (banana shower)
        [Test]
        public void TestConvertDurationObjectWithoutComboData()
        {
            var shower = (BananaShower)convert(8, new BareDurationHit { StartTime = 100, Duration = 900 }).HitObjects.Single();

            Assert.That(shower.Duration, Is.EqualTo(900));
            Assert.That(shower.NewCombo, Is.False);
            Assert.That(shower.ComboOffset, Is.Zero);
        }

        // CF: CatchBeatmapConverter.ConvertHitObject | switch obj | case IHasPathWithRepeats
        // CF: CatchBeatmapConverter.ConvertHitObject | beatmap.BeatmapVersion < 8 | FALSE
        // CF: CatchBeatmapConverter.ConvertHitObject | sliderVelocityData?.SliderVelocityMultiplier ?? 1 | non-null
        [Test]
        public void TestConvertSliderFromModernBeatmap()
        {
            var source = new StubSlider { StartTime = 200, X = 50, RepeatCount = 2, SliderVelocityMultiplier = 1.5, NewCombo = true };

            var stream = (JuiceStream)convert(8, source).HitObjects.Single();

            Assert.That(stream.RepeatCount, Is.EqualTo(2));
            Assert.That(stream.OriginalX, Is.EqualTo(50));
            Assert.That(stream.TickDistanceMultiplier, Is.EqualTo(1));
            Assert.That(stream.SliderVelocityMultiplier, Is.EqualTo(1.5));
            Assert.That(stream.NewCombo, Is.True);
            Assert.That(stream.Path.ControlPoints, Has.Count.EqualTo(2));
        }

        // CF: CatchBeatmapConverter.ConvertHitObject | beatmap.BeatmapVersion < 8 | TRUE
        [Test]
        public void TestConvertSliderFromLegacyBeatmapCompensatesSliderVelocity()
        {
            var beatmap = new Beatmap { BeatmapVersion = 7, ControlPointInfo = new LegacyControlPointInfo() };
            ((LegacyControlPointInfo)beatmap.ControlPointInfo).Add(0, new DifficultyControlPoint { SliderVelocity = 2 });
            beatmap.HitObjects.Add(new StubSlider { StartTime = 100 });

            var stream = (JuiceStream)new CatchBeatmapConverter(beatmap, new CatchRuleset()).Convert().HitObjects.Single();

            Assert.That(stream.TickDistanceMultiplier, Is.EqualTo(0.5).Within(1e-9));
        }

        // CF: CatchBeatmapConverter.ConvertHitObject | xPositionData?.X ?? 0 | null (slider)
        // CF: CatchBeatmapConverter.ConvertHitObject | yPositionData?.Y ?? DEFAULT_LEGACY_CONVERT_Y | null (slider)
        // CF: CatchBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | null (slider)
        // CF: CatchBeatmapConverter.ConvertHitObject | sliderVelocityData?.SliderVelocityMultiplier ?? 1 | null
        [Test]
        public void TestConvertSliderWithoutExtraDataUsesDefaults()
        {
            var stream = (JuiceStream)convert(8, new BarePathHit { StartTime = 5 }).HitObjects.Single();

            Assert.That(stream.OriginalX, Is.Zero);
            Assert.That(stream.NewCombo, Is.False);
            Assert.That(stream.ComboOffset, Is.Zero);
            Assert.That(stream.SliderVelocityMultiplier, Is.EqualTo(1));
            Assert.That(stream.LegacyConvertedY, Is.EqualTo(CatchHitObject.DEFAULT_LEGACY_CONVERT_Y));
        }
    }
}
