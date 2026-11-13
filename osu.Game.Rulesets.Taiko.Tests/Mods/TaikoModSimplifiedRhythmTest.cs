// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Beatmaps;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Mods
{
    [TestFixture]
    public class TaikoModSimplifiedRhythmTest
    {
        private const double beat = 500;

        private static TaikoBeatmap create(params double[] times)
        {
            var beatmap = new TaikoBeatmap();
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = beat });
            foreach (double t in times)
                beatmap.HitObjects.Add(new Hit { StartTime = t });
            return beatmap;
        }

        private static double[] apply(TaikoModSimplifiedRhythm mod, TaikoBeatmap beatmap)
        {
            mod.ApplyToBeatmap(beatmap);
            return beatmap.HitObjects.Select(h => h.StartTime).ToArray();
        }

        private static TaikoModSimplifiedRhythm only(bool third = false, bool sixth = false, bool eighth = false)
        {
            var mod = new TaikoModSimplifiedRhythm();
            mod.OneThirdConversion.Value = third;
            mod.OneSixthConversion.Value = sixth;
            mod.OneEighthConversion.Value = eighth;
            return mod;
        }

        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | hits.Length == 0 | TRUE
        [Test]
        public void TestNoHitsReturnsEarly()
        {
            var beatmap = new TaikoBeatmap();
            beatmap.HitObjects.Add(new DrumRoll { StartTime = 0, Duration = 100 });
            Assert.That(apply(only(sixth: true), beatmap), Is.EqualTo(new[] { 0d }));
        }

        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | hits.Length == 0 | FALSE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | OneSixthConversion.Value | TRUE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | OneEighthConversion.Value | FALSE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | OneThirdConversion.Value | FALSE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | foreach conversions | loop 1
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | for i < hits.Length | loop n
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | inPattern | FALSE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | snapValue == baseRhythm | TRUE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | inPattern | TRUE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | inPattern (end of beatmap) | TRUE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | switch (baseRhythm) | case 6
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | indexInPattern % 3 == 1 | TRUE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | indexInPattern % 3 == 2 | TRUE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | indexInPattern % 3 == 2 | FALSE
        [Test]
        public void TestSixthPatternRunningToEndOfBeatmap()
        {
            // 1/6 apart: 0, 83.3, 166.7, 250 -> second removed, third moved onto 1/4 (125).
            var times = apply(only(sixth: true), create(0, beat / 6, beat / 3, beat / 2));
            Assert.That(times, Is.EqualTo(new[] { 0d, 125, 250 }).Within(1e-9));
        }

        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | snapValue == baseRhythm (in pattern) | FALSE
        [Test]
        public void TestSixthPatternEndedByDifferentSnap()
        {
            var times = apply(only(sixth: true), create(0, beat / 6, beat / 3, beat));
            Assert.That(times, Is.EqualTo(new[] { 0d, 125, beat }).Within(1e-9));
        }

        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | snapValue == baseRhythm | FALSE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | OneSixthConversion.Value | FALSE
        [Test]
        public void TestNoMatchingSnapChangesNothing()
        {
            var times = apply(only(sixth: true), create(0, beat / 2, beat));
            Assert.That(times, Is.EqualTo(new[] { 0d, beat / 2, beat }));
        }

        [Test]
        public void TestNothingEnabledChangesNothing()
        {
            var times = apply(only(), create(0, beat / 6, beat / 3));
            Assert.That(times, Is.EqualTo(new[] { 0d, beat / 6, beat / 3 }));
        }

        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | OneEighthConversion.Value | TRUE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | switch (baseRhythm) | case 8
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | indexInPattern % 2 == 1 | TRUE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | indexInPattern % 2 == 1 | FALSE
        [Test]
        public void TestEighthPatternRemovesEverySecondNote()
        {
            var times = apply(only(eighth: true), create(0, beat / 8, beat / 4, beat * 3 / 8));
            Assert.That(times, Is.EqualTo(new[] { 0d, beat / 4 }).Within(1e-9));
        }

        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | OneThirdConversion.Value | TRUE
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | switch (baseRhythm) | case 3
        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | indexInPattern % 3 == 1 | FALSE
        [Test]
        public void TestThirdPatternMovesToHalfBeat()
        {
            var times = apply(only(third: true), create(0, beat / 3, beat * 2 / 3));
            Assert.That(times, Is.EqualTo(new[] { 0d, beat / 2 }).Within(1e-9));
        }

        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | foreach conversions | loop n
        [Test]
        public void TestMultipleConversionsApplyInOrder()
        {
            var times = apply(only(sixth: true, eighth: true), create(0, beat / 8, beat / 4, beat * 3 / 8));
            Assert.That(times, Is.EqualTo(new[] { 0d, beat / 4 }).Within(1e-9));
        }

        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | foreach conversions | loop 0
        [Test]
        public void TestNoConversionsLoopsZeroTimes()
            => Assert.That(apply(only(), create(0)), Is.EqualTo(new[] { 0d }));

        // CF: TaikoModSimplifiedRhythm.ApplyToBeatmap | (TaikoBeatmap)beatmap | throws InvalidCastException
        [Test]
        public void TestNonTaikoBeatmapThrows()
            => Assert.Throws<InvalidCastException>(() => new TaikoModSimplifiedRhythm().ApplyToBeatmap(new Beatmap<HitObject>()));

        // infeasible: TaikoModSimplifiedRhythm.processPattern `default: throw ArgumentOutOfRangeException`, conversions only ever contain base rhythms 8, 6 and 3
    }
}
