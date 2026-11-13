// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class BeatmapConverterTest
    {
        /// <summary>Converts every non-circle object to two circles 10ms apart, and drops spinners.</summary>
        private class TestConverter : BeatmapConverter<HitCircle>
        {
            public TestConverter(IBeatmap beatmap)
                : base(beatmap, new OsuRuleset())
            {
            }

            public override bool CanConvert() => true;

            protected override IEnumerable<HitCircle> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken)
            {
                if (original is Spinner)
                    return [];

                if (original is Slider)
                    return [new HitCircle { StartTime = original.StartTime + 10 }, new HitCircle { StartTime = original.StartTime }, null!];

                return base.ConvertHitObject(original, beatmap, cancellationToken);
            }
        }

        private static Beatmap source(params HitObject[] objects)
        {
            var beatmap = new Beatmap();
            beatmap.HitObjects.AddRange(objects);
            return beatmap;
        }

        // CF: BeatmapConverter.convertHitObjects | obj is T tObj | TRUE
        [Test]
        public void TestObjectsOfTargetTypePassThroughWithoutConversionEvent()
        {
            var circle = new HitCircle { StartTime = 100 };
            var converter = new TestConverter(source(circle));
            int events = 0;
            ((IBeatmapConverter)converter).ObjectConverted += (_, _) => events++;

            var converted = converter.Convert();

            Assert.That(converted.HitObjects.Single(), Is.SameAs(circle));
            Assert.That(events, Is.Zero);
        }

        // CF: BeatmapConverter.convertHitObjects | obj is T tObj | FALSE
        // CF: BeatmapConverter.convertHitObjects | objectConverted != null | TRUE
        // CF: BeatmapConverter.convertHitObjects | c != null | TRUE
        // CF: BeatmapConverter.convertHitObjects | c != null | FALSE
        [Test]
        public void TestConvertedObjectsAreSortedFilteredAndReported()
        {
            var slider = new Slider { StartTime = 500 };
            var converter = new TestConverter(source(slider));
            var reported = new List<(HitObject original, HitObject[] converted)>();
            ((IBeatmapConverter)converter).ObjectConverted += (o, c) => reported.Add((o, c.ToArray()));

            var converted = converter.Convert();

            Assert.That(converted.HitObjects.Select(h => h.StartTime), Is.EqualTo(new[] { 500d, 510d }), "ordered by start time, null results dropped");
            Assert.That(reported, Has.Count.EqualTo(1));
            Assert.That(reported[0].original, Is.SameAs(slider));
        }

        // CF: BeatmapConverter.convertHitObjects | objectConverted != null | FALSE
        [Test]
        public void TestConversionWithoutSubscribersStillConverts()
        {
            var converted = new TestConverter(source(new Slider { StartTime = 500 }, new Spinner { StartTime = 1000 })).Convert();

            Assert.That(converted.HitObjects.Select(h => h.StartTime), Is.EqualTo(new[] { 500d, 510d }), "spinner produced nothing");
        }

        [Test]
        public void TestConvertCopiesBeatmapProperties()
        {
            var original = source(new HitCircle { StartTime = 100 });
            original.AudioLeadIn = 123;
            original.StackLeniency = 0.3f;
            original.SpecialStyle = true;
            original.LetterboxInBreaks = true;
            original.WidescreenStoryboard = true;
            original.EpilepsyWarning = true;
            original.SamplesMatchPlaybackRate = true;
            original.DistanceSpacing = 2.5;
            original.GridSize = 8;
            original.TimelineZoom = 3;
            original.Countdown = CountdownType.DoubleSpeed;
            original.CountdownOffset = 4;
            original.Bookmarks = [1, 2];
            original.SliderVelocityPresets = [1.5];
            original.Breaks.Add(new BreakPeriod(10, 5000));

            var converted = new TestConverter(original).Convert();

            Assert.That(converted.AudioLeadIn, Is.EqualTo(123));
            Assert.That(converted.StackLeniency, Is.EqualTo(0.3f));
            Assert.That(converted.SpecialStyle && converted.LetterboxInBreaks && converted.WidescreenStoryboard && converted.EpilepsyWarning && converted.SamplesMatchPlaybackRate, Is.True);
            Assert.That(converted.DistanceSpacing, Is.EqualTo(2.5));
            Assert.That(converted.GridSize, Is.EqualTo(8));
            Assert.That(converted.TimelineZoom, Is.EqualTo(3));
            Assert.That(converted.Countdown, Is.EqualTo(CountdownType.DoubleSpeed));
            Assert.That(converted.CountdownOffset, Is.EqualTo(4));
            Assert.That(converted.Bookmarks, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(converted.SliderVelocityPresets, Is.EqualTo(new[] { 1.5 }));
            Assert.That(converted.Breaks, Has.Count.EqualTo(1));
        }

        [Test]
        public void TestConvertDoesNotMutateOriginal()
        {
            var original = source(new HitCircle { StartTime = 100 });
            var info = original.BeatmapInfo;
            var controlPoints = original.ControlPointInfo;

            var converted = new TestConverter(original).Convert();

            Assert.That(converted.BeatmapInfo, Is.Not.SameAs(info), "beatmap info is cloned");
            Assert.That(converted.ControlPointInfo, Is.Not.SameAs(controlPoints), "control points are deep cloned");
            Assert.That(original.HitObjects, Has.Count.EqualTo(1));
        }

        [Test]
        public void TestDefaultConvertHitObjectProducesNothing()
        {
            Assert.That(new TestConverter(source(new Spinner { StartTime = 1 })).Convert().HitObjects, Is.Empty);
        }
    }
}
