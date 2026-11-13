// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Preprocessing
{
    [TestFixture]
    public class TaikoDifficultyHitObjectTest
    {
        // CF: TaikoDifficultyHitObject.ctor | hitObject is Hit hit | TRUE
        // CF: TaikoDifficultyHitObject.ctor | switch (hit.Type) | case Centre
        // CF: TaikoDifficultyHitObject.ctor | switch (hit.Type) | case Rim
        [Test]
        public void TestIndicesPerColourAndNote()
        {
            var o = DifficultyObjectFactory.Create((100, HitType.Centre), (100, HitType.Rim), (100, HitType.Centre), (100, HitType.Rim), (100, HitType.Rim));

            Assert.That(o.ConvertAll(x => x.MonoIndex), Is.EqualTo(new[] { 0, 0, 1, 1, 2 }));
            Assert.That(o.ConvertAll(x => x.NoteIndex), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(o[0].Interval, Is.EqualTo(o[0].DeltaTime));
        }

        // CF: TaikoDifficultyHitObject.PreviousMono | monoDifficultyHitObjects?. | non-null
        // CF: TaikoDifficultyHitObject.NextMono | monoDifficultyHitObjects?. | non-null
        // CF: TaikoDifficultyHitObject.PreviousNote | ElementAtOrDefault | non-null
        // CF: TaikoDifficultyHitObject.NextNote | ElementAtOrDefault | non-null
        [Test]
        public void TestNeighbourLookups()
        {
            var o = DifficultyObjectFactory.Create((100, HitType.Centre), (100, HitType.Rim), (100, HitType.Centre), (100, HitType.Rim));

            Assert.That(o[2].PreviousMono(0), Is.SameAs(o[0]));
            Assert.That(o[0].NextMono(0), Is.SameAs(o[2]));
            Assert.That(o[2].PreviousNote(0), Is.SameAs(o[1]));
            Assert.That(o[1].NextNote(1), Is.SameAs(o[3]));
        }

        // CF: TaikoDifficultyHitObject.PreviousNote | ElementAtOrDefault | null
        // CF: TaikoDifficultyHitObject.NextNote | ElementAtOrDefault | null
        [Test]
        public void TestNeighbourLookupsOutOfRangeGiveNull()
        {
            var o = DifficultyObjectFactory.Create((100, HitType.Centre), (100, HitType.Centre));

            Assert.That(o[0].PreviousNote(0), Is.Null);
            Assert.That(o[1].NextNote(0), Is.Null);
            Assert.That(o[0].PreviousMono(0), Is.Null);
            Assert.That(o[1].NextMono(0), Is.Null);
        }

        // CF: TaikoDifficultyHitObject.ctor | hitObject is Hit hit | FALSE
        // CF: TaikoDifficultyHitObject.PreviousMono | monoDifficultyHitObjects?. | null
        // CF: TaikoDifficultyHitObject.NextMono | monoDifficultyHitObjects?. | null
        [Test]
        public void TestDrumRollIsNotANoteOrMono()
        {
            var roll = new DrumRoll { StartTime = 1000 };
            roll.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            var notes = new List<TaikoDifficultyHitObject>();
            var obj = new TaikoDifficultyHitObject(roll, roll, 1, new List<DifficultyHitObject>(), new(), new(), notes, 0, new ControlPointInfo(), 1);

            Assert.That(notes, Is.Empty);
            Assert.That(obj.PreviousMono(0), Is.Null);
            Assert.That(obj.NextMono(0), Is.Null);
        }

        [Test]
        public void TestEffectiveBpmUsesBpmScrollSpeedGlobalVelocityAndClockRate()
        {
            var cpi = new ControlPointInfo();
            cpi.Add(0, new TimingControlPoint { BeatLength = 500 });
            cpi.Add(0, new EffectControlPoint { ScrollSpeed = 2 });

            var hit = new Hit { StartTime = 1000 };
            hit.ApplyDefaults(cpi, new BeatmapDifficulty());
            var obj = new TaikoDifficultyHitObject(hit, hit, 1.5, new List<DifficultyHitObject>(), new(), new(), new(), 0, cpi, 1.5);

            // BPM 120 (at time 1000 * 1.5) * (global SV 1.5 * scroll speed 2 * clock rate 1.5)
            Assert.That(obj.EffectiveBPM, Is.EqualTo(120 * 1.5 * 2 * 1.5).Within(1e-9));
        }
    }
}
