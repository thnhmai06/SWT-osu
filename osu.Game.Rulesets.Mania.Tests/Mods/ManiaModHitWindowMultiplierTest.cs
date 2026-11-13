// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Scoring;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Mania.Tests.Mods
{
    /// <summary>
    /// Covers the <see cref="IApplicableToHitObject"/> implementations that tweak <see cref="ManiaHitWindows"/>:
    /// <see cref="ManiaModHardRock"/>, <see cref="ManiaModEasy"/> and <see cref="IManiaRateAdjustmentMod"/>.
    /// </summary>
    [TestFixture]
    public class ManiaModHitWindowMultiplierTest
    {
        private static T prepare<T>(T obj) where T : HitObject
        {
            obj.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return obj;
        }

        private static void applyTo(IApplicableToHitObject mod, HitObject obj) => mod.ApplyToHitObject(obj);

        private static ManiaHitWindows windowsOf(HitObject o) => (ManiaHitWindows)o.HitWindows;

        // CF: ManiaModHardRock.ApplyToHitObject | case Note | case Note
        // CF: ManiaModEasy.ApplyToHitObject | case Note | case Note
        [Test]
        public void TestHardRockAndEasyOnNote()
        {
            var note = prepare(new Note());
            applyTo(new ManiaModHardRock(), note);
            Assert.That(windowsOf(note).DifficultyMultiplier, Is.EqualTo(ManiaModHardRock.HIT_WINDOW_DIFFICULTY_MULTIPLIER));

            applyTo(new ManiaModEasy(), note);
            Assert.That(windowsOf(note).DifficultyMultiplier, Is.EqualTo(ManiaModEasy.HIT_WINDOW_DIFFICULTY_MULTIPLIER));
        }

        // CF: ManiaModHardRock.ApplyToHitObject | case HoldNote | case HoldNote
        // CF: ManiaModEasy.ApplyToHitObject | case HoldNote | case HoldNote
        [Test]
        public void TestHardRockAndEasyOnHoldNote()
        {
            var hold = prepare(new HoldNote { Duration = 100 });

            applyTo(new ManiaModHardRock(), hold);
            Assert.That(windowsOf(hold.Head).DifficultyMultiplier, Is.EqualTo(ManiaModHardRock.HIT_WINDOW_DIFFICULTY_MULTIPLIER));
            Assert.That(windowsOf(hold.Tail).DifficultyMultiplier, Is.EqualTo(ManiaModHardRock.HIT_WINDOW_DIFFICULTY_MULTIPLIER));

            applyTo(new ManiaModEasy(), hold);
            Assert.That(windowsOf(hold.Head).DifficultyMultiplier, Is.EqualTo(ManiaModEasy.HIT_WINDOW_DIFFICULTY_MULTIPLIER));
            Assert.That(windowsOf(hold.Tail).DifficultyMultiplier, Is.EqualTo(ManiaModEasy.HIT_WINDOW_DIFFICULTY_MULTIPLIER));
        }

        // CF: ManiaModHardRock.ApplyToHitObject | switch(hitObject) no case matches | default
        // CF: ManiaModEasy.ApplyToHitObject | switch(hitObject) no case matches | default
        // CF: IManiaRateAdjustmentMod.ApplyToHitObject | switch(hitObject) no case matches | default
        [Test]
        public void TestUnrelatedObjectsIgnored()
        {
            var body = prepare(new HoldNoteBody());

            Assert.That(() => applyTo(new ManiaModHardRock(), body), Throws.Nothing);
            Assert.That(() => applyTo(new ManiaModEasy(), body), Throws.Nothing);
            Assert.That(() => applyTo(new ManiaModDoubleTime(), body), Throws.Nothing);
        }

        // CF: IManiaRateAdjustmentMod.ApplyToHitObject | case Note | case Note
        // CF: IManiaRateAdjustmentMod.ApplyToHitObject | case HoldNote | case HoldNote
        [Test]
        public void TestRateAdjustSetsSpeedMultiplier()
        {
            var mod = new ManiaModDoubleTime { SpeedChange = { Value = 1.5 } };
            var note = prepare(new Note());
            var hold = prepare(new HoldNote { Duration = 100 });

            applyTo(mod, note);
            applyTo(mod, hold);

            Assert.That(windowsOf(note).SpeedMultiplier, Is.EqualTo(1.5));
            Assert.That(windowsOf(hold.Head).SpeedMultiplier, Is.EqualTo(1.5));
            Assert.That(windowsOf(hold.Tail).SpeedMultiplier, Is.EqualTo(1.5));
        }
    }
}
