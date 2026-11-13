// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour.Data;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty.Preprocessing.Colour
{
    [TestFixture]
    public class TaikoColourDataTest
    {
        // CF: TaikoColourData.PreviousColourChange | MonoStreak?. | null
        // CF: TaikoColourData.NextColourChange | MonoStreak?. | null
        [Test]
        public void TestNoMonoStreakGivesNullChanges()
        {
            var data = new TaikoColourData();
            Assert.That(data.PreviousColourChange, Is.Null);
            Assert.That(data.NextColourChange, Is.Null);
        }

        // CF: TaikoColourData.PreviousColourChange | MonoStreak?. | non-null
        // CF: TaikoColourData.NextColourChange | MonoStreak?. | non-null
        [Test]
        public void TestChangesAreNotesAdjacentToStreak()
        {
            var o = DifficultyObjectFactory.Create((100, HitType.Centre), (100, HitType.Rim), (100, HitType.Rim), (100, HitType.Centre));
            var streak = new MonoStreak();
            streak.HitObjects.Add(o[1]);
            streak.HitObjects.Add(o[2]);

            var data = new TaikoColourData { MonoStreak = streak };

            Assert.That(data.PreviousColourChange, Is.SameAs(o[0]));
            Assert.That(data.NextColourChange, Is.SameAs(o[3]));
        }
    }
}
