// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Catch.Tests.Objects
{
    [TestFixture]
    public class CatchHitObjectTest
    {
        // CF: CatchHitObject.EffectiveX | Math.Clamp lower bound | OriginalX + XOffset < 0 -> 0
        [Test]
        public void TestEffectiveXClampsToZero()
        {
            Assert.That(new Fruit { OriginalX = 10, XOffset = -50 }.EffectiveX, Is.EqualTo(0f));
        }

        // CF: CatchHitObject.EffectiveX | Math.Clamp upper bound | OriginalX + XOffset > WIDTH -> WIDTH
        [Test]
        public void TestEffectiveXClampsToPlayfieldWidth()
        {
            Assert.That(new Fruit { OriginalX = 500, XOffset = 50 }.EffectiveX, Is.EqualTo(CatchPlayfield.WIDTH));
        }

        // CF: CatchHitObject.EffectiveX | Math.Clamp in range | passthrough
        [Test]
        public void TestEffectiveXInRangeIsSum()
        {
            Assert.That(new Fruit { OriginalX = 100, XOffset = 25 }.EffectiveX, Is.EqualTo(125f));
        }

        // CF: CatchHitObject.X / IHasPosition.Position | setters forward to OriginalX / LegacyConvertedY
        [Test]
        public void TestPositionAccessorsForwardToOriginalXAndLegacyY()
        {
            var fruit = new Fruit();
            ((IHasPosition)fruit).Position = new Vector2(30, 40);

            Assert.That(fruit.OriginalX, Is.EqualTo(30f));
            Assert.That(fruit.LegacyConvertedY, Is.EqualTo(40f));
            Assert.That(((IHasPosition)fruit).Position, Is.EqualTo(new Vector2(30, 40)));

            fruit.X = 77;
            Assert.That(fruit.OriginalX, Is.EqualTo(77f));
        }

        // CF: CatchHitObject.RandomSeed | (int)StartTime truncation
        [Test]
        public void TestRandomSeedIsTruncatedStartTime()
        {
            Assert.That(new Fruit { StartTime = 1234.9 }.RandomSeed, Is.EqualTo(1234));
        }

        // CF: CatchHitObject.ApplyDefaultsToSelf | TimePreempt from DifficultyRangeInt at AR 0 / 5 / 10 (range min/mid/max)
        [TestCase(0, 1800)]
        [TestCase(5, 1200)]
        [TestCase(10, 450)]
        public void TestApplyDefaultsSetsPreemptFromApproachRate(float approachRate, double expectedPreempt)
        {
            var fruit = new Fruit();
            fruit.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { ApproachRate = approachRate });

            Assert.That(fruit.TimePreempt, Is.EqualTo(expectedPreempt));
        }

        // CF: CatchHitObject.ApplyDefaultsToSelf | Scale from circle size (larger CS -> smaller scale)
        [Test]
        public void TestApplyDefaultsScaleDecreasesWithCircleSize()
        {
            var small = new Fruit();
            small.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { CircleSize = 2 });

            var large = new Fruit();
            large.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty { CircleSize = 8 });

            Assert.That(small.Scale, Is.GreaterThan(large.Scale));
        }

        // CF: UpdateComboInformation | lastObj == null TRUE (C), this is not BananaShower TRUE (A) -> starts first combo
        [Test]
        public void TestFirstObjectStartsNewComboEvenWithoutNewComboFlag()
        {
            var fruit = new Fruit { ComboOffset = 2 };
            fruit.UpdateComboInformation(null);

            Assert.That(fruit.ComboIndex, Is.EqualTo(1));
            Assert.That(fruit.ComboIndexWithOffsets, Is.EqualTo(3));
            Assert.That(fruit.IndexInCurrentCombo, Is.EqualTo(0));
        }

        // CF: UpdateComboInformation | NewCombo TRUE (B), lastObj == null FALSE (C), lastObj is BananaShower FALSE (D)
        [Test]
        public void TestNewComboFlagStartsNewComboAndMarksPreviousAsLastInCombo()
        {
            var last = new Fruit { ComboIndex = 4, ComboIndexWithOffsets = 9, IndexInCurrentCombo = 3 };
            var fruit = new Fruit { NewCombo = true, ComboOffset = 1 };
            fruit.UpdateComboInformation(last);

            Assert.That(fruit.ComboIndex, Is.EqualTo(5));
            Assert.That(fruit.ComboIndexWithOffsets, Is.EqualTo(11));
            Assert.That(fruit.IndexInCurrentCombo, Is.EqualTo(0));
            Assert.That(last.LastInCombo, Is.True);
        }

        // CF: UpdateComboInformation | NewCombo FALSE, lastObj == null FALSE, lastObj is BananaShower FALSE -> continues combo
        [Test]
        public void TestContinuingComboIncrementsIndexInCurrentCombo()
        {
            var last = new Fruit { ComboIndex = 4, ComboIndexWithOffsets = 9, IndexInCurrentCombo = 3 };
            var fruit = new Fruit { NewCombo = false };
            fruit.UpdateComboInformation(last);

            Assert.That(fruit.ComboIndex, Is.EqualTo(4));
            Assert.That(fruit.ComboIndexWithOffsets, Is.EqualTo(9));
            Assert.That(fruit.IndexInCurrentCombo, Is.EqualTo(4));
            Assert.That(last.LastInCombo, Is.False);
        }

        // CF: UpdateComboInformation | lastObj is BananaShower TRUE (D) with NewCombo FALSE, lastObj != null -> new combo
        [Test]
        public void TestObjectAfterBananaShowerStartsNewCombo()
        {
            var last = new BananaShower { ComboIndex = 2, ComboIndexWithOffsets = 2, IndexInCurrentCombo = 0 };
            var fruit = new Fruit { NewCombo = false };
            fruit.UpdateComboInformation(last);

            Assert.That(fruit.ComboIndex, Is.EqualTo(3));
            Assert.That(fruit.IndexInCurrentCombo, Is.EqualTo(0));
            Assert.That(last.LastInCombo, Is.True);
        }

        // CF: UpdateComboInformation | this is not BananaShower FALSE (A) with NewCombo TRUE -> shower never starts a combo
        [Test]
        public void TestBananaShowerNeverStartsNewCombo()
        {
            var last = new Fruit { ComboIndex = 4, ComboIndexWithOffsets = 9, IndexInCurrentCombo = 3 };
            var shower = new BananaShower { NewCombo = true };
            shower.UpdateComboInformation(last);

            Assert.That(shower.ComboIndex, Is.EqualTo(4));
            Assert.That(shower.IndexInCurrentCombo, Is.EqualTo(4));
            Assert.That(last.LastInCombo, Is.False);
        }

        // CF: UpdateComboInformation | A FALSE with lastObj == null -> `?? 0` fallbacks only
        [Test]
        public void TestBananaShowerWithoutPreviousObjectUsesZeroDefaults()
        {
            var shower = new BananaShower();
            shower.UpdateComboInformation(null);

            Assert.That(shower.ComboIndex, Is.EqualTo(0));
            Assert.That(shower.ComboIndexWithOffsets, Is.EqualTo(0));
            Assert.That(shower.IndexInCurrentCombo, Is.EqualTo(0));
        }
    }
}
