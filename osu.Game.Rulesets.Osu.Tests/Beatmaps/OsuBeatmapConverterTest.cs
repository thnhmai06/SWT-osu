// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Beatmaps
{
    [TestFixture]
    public class OsuBeatmapConverterTest
    {
        private static Beatmap decode(int version, string hitObjects, string timingPoints = "0,500,4,1,0,100,1,0")
        {
            string text = $"osu file format v{version}\n\n[General]\nMode: 0\n\n[Difficulty]\nSliderMultiplier:1.4\n\n[TimingPoints]\n{timingPoints}\n\n[HitObjects]\n{hitObjects}\n";

            using var reader = new LineBufferedReader(new MemoryStream(Encoding.UTF8.GetBytes(text)));
            return new LegacyBeatmapDecoder().Decode(reader);
        }

        private static List<OsuHitObject> convert(IBeatmap beatmap) => new OsuBeatmapConverter(beatmap, new OsuRuleset()).Convert().HitObjects.Cast<OsuHitObject>().ToList();

        // ---- CanConvert: All(h => h is IHasPosition) ----

        [Test]
        public void TestCanConvert_AllObjectsPositioned_True()
        {
            var beatmap = decode(14, "256,192,1000,1,0,0:0:0:0:");

            Assert.That(new OsuBeatmapConverter(beatmap, new OsuRuleset()).CanConvert(), Is.True);
        }

        [Test]
        public void TestCanConvert_EmptyBeatmap_True()
        {
            // loop body never executes
            Assert.That(new OsuBeatmapConverter(new Beatmap(), new OsuRuleset()).CanConvert(), Is.True);
        }

        [Test]
        public void TestCanConvert_ObjectWithoutPosition_False()
        {
            var beatmap = new Beatmap { HitObjects = { new PlainHitObject() } };

            Assert.That(new OsuBeatmapConverter(beatmap, new OsuRuleset()).CanConvert(), Is.False);
        }

        // ---- ConvertHitObject switch: slider / duration / default ----

        [Test]
        public void TestConvert_Circle_DefaultCase()
        {
            var circle = convert(decode(14, "100,120,1000,37,0,0:0:0:0:")).Single();

            Assert.That(circle, Is.TypeOf<HitCircle>());
            Assert.That(circle.StartTime, Is.EqualTo(1000));
            Assert.That(circle.Position, Is.EqualTo(new Vector2(100, 120)));
            Assert.That(circle.NewCombo, Is.True);
            Assert.That(circle.ComboOffset, Is.EqualTo(2));
        }

        [Test]
        public void TestConvert_CircleWithoutNewCombo()
        {
            // `comboData?.NewCombo ?? false` takes the real value (false)
            var objects = convert(decode(14, "100,120,1000,5,0,0:0:0:0:\n100,120,1500,1,0,0:0:0:0:"));

            Assert.That(objects[1].NewCombo, Is.False);
            Assert.That(objects[1].ComboOffset, Is.EqualTo(0));
        }

        [Test]
        public void TestConvert_Slider_PathCase()
        {
            var slider = (Slider)convert(decode(14, "100,100,2000,2,0,L|300:100,2,200")).Single();

            Assert.That(slider.StartTime, Is.EqualTo(2000));
            Assert.That(slider.RepeatCount, Is.EqualTo(1));
            Assert.That(slider.Path.ControlPoints.Count, Is.EqualTo(2));
            Assert.That(slider.Position, Is.EqualTo(new Vector2(100, 100)));
            Assert.That(slider.GenerateTicks, Is.True);
            Assert.That(slider.SliderVelocityMultiplier, Is.EqualTo(1));
        }

        [Test]
        public void TestConvert_Slider_ModernBeatmapKeepsTickDistanceMultiplier()
        {
            // BeatmapVersion < 8 FALSE
            var slider = (Slider)convert(decode(14, "100,100,2000,2,0,L|300:100,1,200", "0,500,4,1,0,100,1,0\n0,-50,4,1,0,100,0,0")).Single();

            Assert.That(slider.TickDistanceMultiplier, Is.EqualTo(1));
        }

        [Test]
        public void TestConvert_Slider_LegacyBeatmapScalesTickDistanceByInverseVelocity()
        {
            // BeatmapVersion < 8 TRUE: 1 / SliderVelocity (inherited point at 50% beat length => velocity 2)
            var slider = (Slider)convert(decode(7, "100,100,2000,2,0,L|300:100,1,200", "0,500,4,1,0,100,1,0\n0,-50,4,1,0,100,0,0")).Single();

            Assert.That(slider.TickDistanceMultiplier, Is.EqualTo(0.5).Within(1e-6));
        }

        [Test]
        public void TestConvert_Spinner_DurationCase()
        {
            var spinner = (Spinner)convert(decode(14, "256,192,4000,12,0,6000,0:0:0:0:")).Single();

            Assert.That(spinner.StartTime, Is.EqualTo(4000));
            Assert.That(spinner.EndTime, Is.EqualTo(6000));
            Assert.That(spinner.Position, Is.EqualTo(new Vector2(256, 192)));
            Assert.That(spinner.NewCombo, Is.True);
        }

        // ---- null-coalescing fallbacks (objects that expose none of the optional interfaces) ----

        [Test]
        public void TestConvert_UnknownObject_BecomesCircleAtOrigin()
        {
            // positionData / comboData both null
            var circle = convert(new Beatmap { HitObjects = { new PlainHitObject { StartTime = 500 } } }).Single();

            Assert.That(circle, Is.TypeOf<HitCircle>());
            Assert.That(circle.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(circle.NewCombo, Is.False);
            Assert.That(circle.ComboOffset, Is.EqualTo(0));
        }

        [Test]
        public void TestConvert_DurationObjectWithoutPosition_SpinnerAtPlayfieldCentre()
        {
            // `positionData?.Position ?? BASE_SIZE / 2`
            var spinner = (Spinner)convert(new Beatmap { HitObjects = { new PlainDurationObject { StartTime = 100, Duration = 900 } } }).Single();

            Assert.That(spinner.Position, Is.EqualTo(new Vector2(256, 192)));
            Assert.That(spinner.EndTime, Is.EqualTo(1000));
            Assert.That(spinner.NewCombo, Is.False);
        }

        [Test]
        public void TestConvert_PathObjectWithoutOptionalInterfaces_UsesDefaults()
        {
            // positionData / comboData / sliderVelocityData / generateTicksData all null
            var slider = (Slider)convert(new Beatmap { HitObjects = { new PlainPathObject { StartTime = 100 } } }).Single();

            Assert.That(slider.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(slider.NewCombo, Is.False);
            Assert.That(slider.GenerateTicks, Is.True);
            Assert.That(slider.SliderVelocityMultiplier, Is.EqualTo(1));
            Assert.That(slider.TickDistanceMultiplier, Is.EqualTo(1));
        }

        [Test]
        public void TestConvert_PreservesChronologicalOrder()
        {
            var objects = convert(decode(14, "256,192,3000,1,0,0:0:0:0:\n256,192,1000,1,0,0:0:0:0:"));

            Assert.That(objects.Select(o => o.StartTime), Is.EqualTo(new[] { 1000d, 3000d }));
        }

        [Test]
        public void TestConvert_ProducesOsuBeatmap()
        {
            Assert.That(new OsuBeatmapConverter(new Beatmap(), new OsuRuleset()).Convert(), Is.TypeOf<OsuBeatmap>());
        }

        private class PlainHitObject : HitObject
        {
        }

        private class PlainDurationObject : HitObject, IHasDuration
        {
            public double EndTime => StartTime + Duration;
            public double Duration { get; set; }
        }

        private class PlainPathObject : HitObject, IHasPathWithRepeats
        {
            public SliderPath Path { get; } = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) });
            public double Distance => Path.Distance;
            public double EndTime => StartTime + Duration;
            public double Duration { get; set; } = 1000;
            public int RepeatCount { get; set; }
            public IList<IList<HitSampleInfo>> NodeSamples { get; } = new List<IList<HitSampleInfo>>();
        }
    }
}
