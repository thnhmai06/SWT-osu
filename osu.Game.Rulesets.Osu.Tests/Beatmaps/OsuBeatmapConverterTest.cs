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
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Beatmaps
{
    [TestFixture]
    public class OsuBeatmapConverterTest
    {
        private static IBeatmap convert(int version, params HitObject[] objects)
        {
            var beatmap = new Beatmap
            {
                BeatmapVersion = version,
                ControlPointInfo = new LegacyControlPointInfo(),
            };
            beatmap.ControlPointInfo.Add(0, new DifficultyControlPoint { SliderVelocity = 2 });
            beatmap.HitObjects.AddRange(objects);
            return new OsuBeatmapConverter(beatmap, new OsuRuleset()).Convert();
        }

        private static OsuBeatmapConverter converterOf(params HitObject[] objects)
        {
            var beatmap = new Beatmap();
            beatmap.HitObjects.AddRange(objects);
            return new OsuBeatmapConverter(beatmap, new OsuRuleset());
        }

        // CF: OsuBeatmapConverter.CanConvert | h is IHasPosition | TRUE
        [Test]
        public void TestCanConvertWhenAllHavePosition()
        {
            Assert.That(converterOf(new PositionedObject(), new PositionedObject()).CanConvert(), Is.True);
        }

        // CF: OsuBeatmapConverter.CanConvert | h is IHasPosition | FALSE
        [Test]
        public void TestCannotConvertWhenAnyLacksPosition()
        {
            Assert.That(converterOf(new PositionedObject(), new PlainObject()).CanConvert(), Is.False);
        }

        // CF: OsuBeatmapConverter.ConvertHitObject | switch (original) | default
        // CF: OsuBeatmapConverter.ConvertHitObject | positionData?.Position ?? Vector2.Zero | non-null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | non-null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.ComboOffset ?? 0 | non-null
        [Test]
        public void TestCircleKeepsPositionAndCombo()
        {
            var result = convert(14, new PositionedObject { StartTime = 100, Position = new Vector2(10, 20), NewCombo = true, ComboOffset = 3 });

            var circle = result.HitObjects.Single();
            Assert.That(circle, Is.TypeOf<HitCircle>());
            Assert.That(((HitCircle)circle).Position, Is.EqualTo(new Vector2(10, 20)));
            Assert.That(((HitCircle)circle).NewCombo, Is.True);
            Assert.That(((HitCircle)circle).ComboOffset, Is.EqualTo(3));
            Assert.That(circle.StartTime, Is.EqualTo(100));
        }

        // CF: OsuBeatmapConverter.ConvertHitObject | positionData?.Position ?? Vector2.Zero | null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.ComboOffset ?? 0 | null
        [Test]
        public void TestCircleWithoutPositionOrComboUsesDefaults()
        {
            var circle = (HitCircle)convert(14, new PlainObject()).HitObjects.Single();

            Assert.That(circle.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(circle.NewCombo, Is.False);
            Assert.That(circle.ComboOffset, Is.EqualTo(0));
        }

        // CF: OsuBeatmapConverter.ConvertHitObject | switch (original) | case IHasDuration
        // CF: OsuBeatmapConverter.ConvertHitObject | positionData?.Position ?? OsuPlayfield.BASE_SIZE / 2 | non-null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | non-null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.ComboOffset ?? 0 | non-null
        [Test]
        public void TestSpinnerKeepsPositionAndCombo()
        {
            var spinner = (Spinner)convert(14, new PositionedDuration { StartTime = 100, Duration = 500, Position = new Vector2(5, 6), NewCombo = true, ComboOffset = 2 }).HitObjects.Single();

            Assert.That(spinner.EndTime, Is.EqualTo(600));
            Assert.That(spinner.Position, Is.EqualTo(new Vector2(5, 6)));
            Assert.That(spinner.NewCombo, Is.True);
            Assert.That(spinner.ComboOffset, Is.EqualTo(2));
        }

        // CF: OsuBeatmapConverter.ConvertHitObject | positionData?.Position ?? OsuPlayfield.BASE_SIZE / 2 | null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.ComboOffset ?? 0 | null
        [Test]
        public void TestSpinnerWithoutPositionIsCentred()
        {
            var spinner = (Spinner)convert(14, new PlainDuration { StartTime = 0, Duration = 500 }).HitObjects.Single();

            Assert.That(spinner.Position, Is.EqualTo(OsuPlayfield.BASE_SIZE / 2));
            Assert.That(spinner.NewCombo, Is.False);
            Assert.That(spinner.ComboOffset, Is.EqualTo(0));
        }

        // CF: OsuBeatmapConverter.ConvertHitObject | switch (original) | case IHasPathWithRepeats
        // CF: OsuBeatmapConverter.ConvertHitObject | beatmap.BeatmapVersion < 8 | FALSE
        // CF: OsuBeatmapConverter.ConvertHitObject | sliderVelocityData?.SliderVelocityMultiplier ?? 1 | non-null
        // CF: OsuBeatmapConverter.ConvertHitObject | generateTicksData?.GenerateTicks ?? true | non-null
        // CF: OsuBeatmapConverter.ConvertHitObject | positionData?.Position ?? Vector2.Zero | non-null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | non-null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.ComboOffset ?? 0 | non-null
        [Test]
        public void TestFullSliderOnModernVersion()
        {
            var slider = (Slider)convert(14, new FullSlider { Position = new Vector2(1, 2), NewCombo = true, ComboOffset = 4, SliderVelocityMultiplier = 1.5, GenerateTicks = false, RepeatCount = 1 }).HitObjects.Single();

            Assert.That(slider.Position, Is.EqualTo(new Vector2(1, 2)));
            Assert.That(slider.NewCombo, Is.True);
            Assert.That(slider.ComboOffset, Is.EqualTo(4));
            Assert.That(slider.TickDistanceMultiplier, Is.EqualTo(1));
            Assert.That(slider.SliderVelocityMultiplier, Is.EqualTo(1.5));
            Assert.That(slider.GenerateTicks, Is.False);
            Assert.That(slider.RepeatCount, Is.EqualTo(1));
        }

        // CF: OsuBeatmapConverter.ConvertHitObject | beatmap.BeatmapVersion < 8 | TRUE
        // CF: OsuBeatmapConverter.ConvertHitObject | sliderVelocityData?.SliderVelocityMultiplier ?? 1 | null
        // CF: OsuBeatmapConverter.ConvertHitObject | generateTicksData?.GenerateTicks ?? true | null
        // CF: OsuBeatmapConverter.ConvertHitObject | positionData?.Position ?? Vector2.Zero | null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.NewCombo ?? false | null
        // CF: OsuBeatmapConverter.ConvertHitObject | comboData?.ComboOffset ?? 0 | null
        [Test]
        public void TestBareSliderOnLegacyVersion()
        {
            var slider = (Slider)convert(7, new BareSlider()).HitObjects.Single();

            // slider velocity of 2 at the object's start time -> ticks are spaced 1 / 2 as far apart
            Assert.That(slider.TickDistanceMultiplier, Is.EqualTo(0.5));
            Assert.That(slider.SliderVelocityMultiplier, Is.EqualTo(1));
            Assert.That(slider.GenerateTicks, Is.True);
            Assert.That(slider.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(slider.NewCombo, Is.False);
            Assert.That(slider.ComboOffset, Is.EqualTo(0));
        }

        private class PlainObject : HitObject
        {
        }

        private class PositionedObject : HitObject, IHasPosition, IHasCombo
        {
            public Vector2 Position { get; set; }
            public float X { get => Position.X; set => Position = new Vector2(value, Position.Y); }
            public float Y { get => Position.Y; set => Position = new Vector2(Position.X, value); }
            public bool NewCombo { get; set; }
            public int ComboOffset { get; set; }
        }

        private class PlainDuration : HitObject, IHasDuration
        {
            public double EndTime => StartTime + Duration;
            public double Duration { get; set; }
        }

        private class PositionedDuration : PlainDuration, IHasPosition, IHasCombo
        {
            public Vector2 Position { get; set; }
            public float X { get => Position.X; set => Position = new Vector2(value, Position.Y); }
            public float Y { get => Position.Y; set => Position = new Vector2(Position.X, value); }
            public bool NewCombo { get; set; }
            public int ComboOffset { get; set; }
        }

        private class BareSlider : HitObject, IHasPathWithRepeats
        {
            public SliderPath Path { get; } = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) });
            public double Distance => Path.Distance;
            public double EndTime => StartTime + Duration;
            public double Duration { get; set; } = 1000;
            public int RepeatCount { get; set; }
            public IList<IList<HitSampleInfo>> NodeSamples { get; } = new List<IList<HitSampleInfo>>();
        }

        private class FullSlider : BareSlider, IHasPosition, IHasCombo, IHasSliderVelocity, IHasGenerateTicks
        {
            public Vector2 Position { get; set; }
            public float X { get => Position.X; set => Position = new Vector2(value, Position.Y); }
            public float Y { get => Position.Y; set => Position = new Vector2(Position.X, value); }
            public bool NewCombo { get; set; }
            public int ComboOffset { get; set; }
            public double SliderVelocityMultiplier { get; set; } = 1;
            public BindableNumber<double> SliderVelocityMultiplierBindable { get; } = new BindableDouble(1);
            public bool GenerateTicks { get; set; } = true;
        }
    }
}
