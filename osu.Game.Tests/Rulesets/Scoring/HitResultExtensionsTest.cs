// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Scoring;

#pragma warning disable CS0618 // HitResult.LegacyComboIncrease is obsolete but part of the behaviour under test.

namespace osu.Game.Tests.Rulesets.Scoring
{
    [TestFixture]
    public class HitResultExtensionsTest
    {
        // CF: HitResultExtensions.AffectsCombo | switch listed cases | return true
        [TestCase(HitResult.Miss)]
        [TestCase(HitResult.Meh)]
        [TestCase(HitResult.Ok)]
        [TestCase(HitResult.Good)]
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.Perfect)]
        [TestCase(HitResult.LargeTickHit)]
        [TestCase(HitResult.LargeTickMiss)]
        [TestCase(HitResult.LegacyComboIncrease)]
        [TestCase(HitResult.ComboBreak)]
        [TestCase(HitResult.SliderTailHit)]
        public void TestAffectsComboTrueCases(HitResult result)
        {
            Assert.That(result.AffectsCombo(), Is.True);
        }

        // CF: HitResultExtensions.AffectsCombo | switch default | return false
        [TestCase(HitResult.None)]
        [TestCase(HitResult.SmallTickHit)]
        [TestCase(HitResult.SmallTickMiss)]
        [TestCase(HitResult.SmallBonus)]
        [TestCase(HitResult.LargeBonus)]
        [TestCase(HitResult.IgnoreHit)]
        [TestCase(HitResult.IgnoreMiss)]
        public void TestAffectsComboDefaultCases(HitResult result)
        {
            Assert.That(result.AffectsCombo(), Is.False);
        }

        // CF: HitResultExtensions.IncreasesCombo | AffectsCombo TRUE, IsHit TRUE | TRUE
        [Test]
        public void TestIncreasesComboForComboHit()
        {
            Assert.That(HitResult.Great.IncreasesCombo(), Is.True);
        }

        // CF: HitResultExtensions.IncreasesCombo | AffectsCombo TRUE, IsHit FALSE | FALSE
        [Test]
        public void TestIncreasesComboFalseForComboMiss()
        {
            Assert.That(HitResult.Miss.IncreasesCombo(), Is.False);
        }

        // CF: HitResultExtensions.IncreasesCombo | AffectsCombo FALSE (short-circuits IsHit) | FALSE
        [Test]
        public void TestIncreasesComboFalseWhenNotAffectingCombo()
        {
            Assert.That(HitResult.SmallBonus.IncreasesCombo(), Is.False);
        }

        // CF: HitResultExtensions.BreaksCombo | AffectsCombo TRUE, !IsHit TRUE | TRUE
        [Test]
        public void TestBreaksComboForComboMiss()
        {
            Assert.That(HitResult.Miss.BreaksCombo(), Is.True);
        }

        // CF: HitResultExtensions.BreaksCombo | AffectsCombo TRUE, !IsHit FALSE | FALSE
        [Test]
        public void TestBreaksComboFalseForComboHit()
        {
            Assert.That(HitResult.Great.BreaksCombo(), Is.False);
        }

        // CF: HitResultExtensions.BreaksCombo | AffectsCombo FALSE (short-circuits !IsHit) | FALSE
        [Test]
        public void TestBreaksComboFalseWhenNotAffectingCombo()
        {
            Assert.That(HitResult.SmallTickMiss.BreaksCombo(), Is.False);
        }

        // CF: HitResultExtensions.AffectsAccuracy | case LegacyComboIncrease | FALSE
        [Test]
        public void TestAffectsAccuracyLegacyComboIncreaseIsFalse() => Assert.That(HitResult.LegacyComboIncrease.AffectsAccuracy(), Is.False);

        // CF: HitResultExtensions.AffectsAccuracy | case ComboBreak | FALSE
        [Test]
        public void TestAffectsAccuracyComboBreakIsFalse() => Assert.That(HitResult.ComboBreak.AffectsAccuracy(), Is.False);

        // CF: HitResultExtensions.AffectsAccuracy | default: IsScorable TRUE, !IsBonus TRUE | TRUE
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.LargeTickHit)]
        [TestCase(HitResult.SliderTailHit)]
        public void TestAffectsAccuracyTrueForScorableNonBonus(HitResult result) => Assert.That(result.AffectsAccuracy(), Is.True);

        // CF: HitResultExtensions.AffectsAccuracy | default: IsScorable TRUE, !IsBonus FALSE | FALSE
        [TestCase(HitResult.SmallBonus)]
        [TestCase(HitResult.LargeBonus)]
        public void TestAffectsAccuracyFalseForBonus(HitResult result) => Assert.That(result.AffectsAccuracy(), Is.False);

        // CF: HitResultExtensions.AffectsAccuracy | default: IsScorable FALSE (short-circuits !IsBonus) | FALSE
        [TestCase(HitResult.None)]
        [TestCase(HitResult.IgnoreHit)]
        [TestCase(HitResult.IgnoreMiss)]
        public void TestAffectsAccuracyFalseForNonScorable(HitResult result) => Assert.That(result.AffectsAccuracy(), Is.False);

        // CF: HitResultExtensions.IsBasic | case LegacyComboIncrease / ComboBreak | FALSE
        [TestCase(HitResult.LegacyComboIncrease)]
        [TestCase(HitResult.ComboBreak)]
        public void TestIsBasicFalseForSpecialTypes(HitResult result) => Assert.That(result.IsBasic(), Is.False);

        // CF: HitResultExtensions.IsBasic | default: IsScorable TRUE, !IsTick TRUE, !IsBonus TRUE | TRUE
        [TestCase(HitResult.Miss)]
        [TestCase(HitResult.Meh)]
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.Perfect)]
        public void TestIsBasicTrueForJudgementTiers(HitResult result) => Assert.That(result.IsBasic(), Is.True);

        // CF: HitResultExtensions.IsBasic | default: IsScorable TRUE, !IsTick FALSE | FALSE
        [TestCase(HitResult.LargeTickHit)]
        [TestCase(HitResult.SmallTickMiss)]
        [TestCase(HitResult.SliderTailHit)]
        public void TestIsBasicFalseForTicks(HitResult result) => Assert.That(result.IsBasic(), Is.False);

        // CF: HitResultExtensions.IsBasic | default: IsScorable TRUE, !IsTick TRUE, !IsBonus FALSE | FALSE
        [Test]
        public void TestIsBasicFalseForBonus() => Assert.That(HitResult.LargeBonus.IsBasic(), Is.False);

        // CF: HitResultExtensions.IsBasic | default: IsScorable FALSE (short-circuit) | FALSE
        [TestCase(HitResult.None)]
        [TestCase(HitResult.IgnoreHit)]
        public void TestIsBasicFalseForNonScorable(HitResult result) => Assert.That(result.IsBasic(), Is.False);

        // CF: HitResultExtensions.IsTick | switch tick cases | TRUE, default | FALSE
        [TestCase(HitResult.LargeTickHit, true)]
        [TestCase(HitResult.LargeTickMiss, true)]
        [TestCase(HitResult.SmallTickHit, true)]
        [TestCase(HitResult.SmallTickMiss, true)]
        [TestCase(HitResult.SliderTailHit, true)]
        [TestCase(HitResult.Great, false)]
        [TestCase(HitResult.SmallBonus, false)]
        public void TestIsTick(HitResult result, bool expected) => Assert.That(result.IsTick(), Is.EqualTo(expected));

        // CF: HitResultExtensions.IsBonus | switch bonus cases | TRUE, default | FALSE
        [TestCase(HitResult.SmallBonus, true)]
        [TestCase(HitResult.LargeBonus, true)]
        [TestCase(HitResult.Great, false)]
        [TestCase(HitResult.IgnoreHit, false)]
        public void TestIsBonus(HitResult result, bool expected) => Assert.That(result.IsBonus(), Is.EqualTo(expected));

        // CF: HitResultExtensions.IsMiss | switch miss cases | TRUE, default | FALSE
        [TestCase(HitResult.IgnoreMiss, true)]
        [TestCase(HitResult.Miss, true)]
        [TestCase(HitResult.SmallTickMiss, true)]
        [TestCase(HitResult.LargeTickMiss, true)]
        [TestCase(HitResult.ComboBreak, true)]
        [TestCase(HitResult.Great, false)]
        [TestCase(HitResult.None, false)]
        public void TestIsMiss(HitResult result, bool expected) => Assert.That(result.IsMiss(), Is.EqualTo(expected));

        // CF: HitResultExtensions.IsHit | switch non-hit cases | FALSE, default | TRUE
        [TestCase(HitResult.None, false)]
        [TestCase(HitResult.IgnoreMiss, false)]
        [TestCase(HitResult.Miss, false)]
        [TestCase(HitResult.SmallTickMiss, false)]
        [TestCase(HitResult.LargeTickMiss, false)]
        [TestCase(HitResult.ComboBreak, false)]
        [TestCase(HitResult.Great, true)]
        [TestCase(HitResult.IgnoreHit, true)]
        [TestCase(HitResult.SmallBonus, true)]
        public void TestIsHit(HitResult result, bool expected) => Assert.That(result.IsHit(), Is.EqualTo(expected));

        // CF: HitResultExtensions.IsScorable | special cases LegacyComboIncrease / ComboBreak / SliderTailHit | TRUE
        [TestCase(HitResult.LegacyComboIncrease)]
        [TestCase(HitResult.ComboBreak)]
        [TestCase(HitResult.SliderTailHit)]
        public void TestIsScorableSpecialCases(HitResult result) => Assert.That(result.IsScorable(), Is.True);

        // CF: HitResultExtensions.IsScorable | default: result >= Miss TRUE, result < IgnoreMiss TRUE | TRUE
        [TestCase(HitResult.Miss)]
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.LargeBonus)]
        public void TestIsScorableWithinRange(HitResult result) => Assert.That(result.IsScorable(), Is.True);

        // CF: HitResultExtensions.IsScorable | default: result >= Miss FALSE (None) | FALSE
        [Test]
        public void TestIsScorableFalseBelowMiss() => Assert.That(HitResult.None.IsScorable(), Is.False);

        // CF: HitResultExtensions.IsScorable | default: result >= Miss TRUE, result < IgnoreMiss FALSE | FALSE
        [TestCase(HitResult.IgnoreMiss)]
        [TestCase(HitResult.IgnoreHit)]
        public void TestIsScorableFalseAtOrAboveIgnoreMiss(HitResult result) => Assert.That(result.IsScorable(), Is.False);

        // CF: HitResultExtensions.ALL_TYPES | Except(LegacyComboIncrease)
        [Test]
        public void TestAllTypesExcludesLegacyComboIncrease()
        {
            Assert.That(HitResultExtensions.ALL_TYPES, Does.Not.Contain(HitResult.LegacyComboIncrease));
            Assert.That(HitResultExtensions.ALL_TYPES, Does.Contain(HitResult.Great));
            Assert.That(HitResultExtensions.ALL_TYPES.Length, Is.EqualTo(Enum.GetValues<HitResult>().Length - 1));
        }

        // CF: HitResultExtensions.IsValidHitResult | result == None | TRUE -> FALSE
        [Test]
        public void TestIsValidHitResultNoneIsInvalid() => Assert.That(HitResult.None.IsValidHitResult(HitResult.Miss, HitResult.Great), Is.False);

        // CF: HitResultExtensions.IsValidHitResult | result == minResult TRUE -> TRUE
        [Test]
        public void TestIsValidHitResultEqualToMin() => Assert.That(HitResult.Miss.IsValidHitResult(HitResult.Miss, HitResult.Great), Is.True);

        // CF: HitResultExtensions.IsValidHitResult | result == minResult FALSE, result == maxResult TRUE -> TRUE
        [Test]
        public void TestIsValidHitResultEqualToMax() => Assert.That(HitResult.Great.IsValidHitResult(HitResult.Miss, HitResult.Great), Is.True);

        // CF: HitResultExtensions.IsValidHitResult | result > min TRUE, result < max TRUE -> TRUE
        [Test]
        public void TestIsValidHitResultBetweenBounds() => Assert.That(HitResult.Ok.IsValidHitResult(HitResult.Miss, HitResult.Great), Is.True);

        // CF: HitResultExtensions.IsValidHitResult | result > min FALSE (short-circuit) -> FALSE
        [Test]
        public void TestIsValidHitResultBelowMin() => Assert.That(HitResult.Miss.IsValidHitResult(HitResult.Meh, HitResult.Great), Is.False);

        // CF: HitResultExtensions.IsValidHitResult | result > min TRUE, result < max FALSE -> FALSE
        [Test]
        public void TestIsValidHitResultAboveMax() => Assert.That(HitResult.Perfect.IsValidHitResult(HitResult.Miss, HitResult.Great), Is.False);

        // CF: HitResultExtensions.GetIndexForOrderedDisplay | Order attribute ordering
        [Test]
        public void TestOrderedDisplayIndexFollowsOrderAttribute()
        {
            Assert.That(HitResult.Perfect.GetIndexForOrderedDisplay(), Is.EqualTo(0));
            Assert.That(HitResult.Great.GetIndexForOrderedDisplay(), Is.LessThan(HitResult.Miss.GetIndexForOrderedDisplay()));
            Assert.That(HitResult.Miss.GetIndexForOrderedDisplay(), Is.LessThan(HitResult.None.GetIndexForOrderedDisplay()));
        }

        // CF: HitResultExtensions.ValidateHitResultPair | valid pairs | no throw (incl. early return for IgnoreMiss)
        [TestCase(HitResult.Great, HitResult.Miss)]
        [TestCase(HitResult.Great, HitResult.IgnoreMiss)]
        [TestCase(HitResult.IgnoreHit, HitResult.IgnoreMiss)]
        [TestCase(HitResult.IgnoreHit, HitResult.ComboBreak)]
        [TestCase(HitResult.SmallBonus, HitResult.IgnoreMiss)]
        [TestCase(HitResult.LargeBonus, HitResult.IgnoreMiss)]
        [TestCase(HitResult.SliderTailHit, HitResult.LargeTickMiss)]
        [TestCase(HitResult.LargeTickHit, HitResult.LargeTickMiss)]
        [TestCase(HitResult.SmallTickHit, HitResult.SmallTickMiss)]
        public void TestValidateHitResultPairAcceptsValidPairs(HitResult max, HitResult min)
        {
            Assert.That(() => HitResultExtensions.ValidateHitResultPair(max, min), Throws.Nothing);
        }

        // CF: ValidateHitResultPair | maxResult == None TRUE | throws for maxResult
        [Test]
        public void TestValidateRejectsNoneMax()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => HitResultExtensions.ValidateHitResultPair(HitResult.None, HitResult.Miss));
            Assert.That(ex!.ParamName, Is.EqualTo("maxResult"));
        }

        // CF: ValidateHitResultPair | maxResult == None FALSE, !IsHit(maxResult) TRUE | throws for maxResult
        [Test]
        public void TestValidateRejectsMissAsMax()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => HitResultExtensions.ValidateHitResultPair(HitResult.Miss, HitResult.Miss));
            Assert.That(ex!.ParamName, Is.EqualTo("maxResult"));
        }

        // CF: ValidateHitResultPair | minResult == None TRUE | throws for minResult
        [Test]
        public void TestValidateRejectsNoneMin()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => HitResultExtensions.ValidateHitResultPair(HitResult.Great, HitResult.None));
            Assert.That(ex!.ParamName, Is.EqualTo("minResult"));
        }

        // CF: ValidateHitResultPair | minResult == None FALSE, IsHit(minResult) TRUE | throws for minResult
        [Test]
        public void TestValidateRejectsHitAsMin()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => HitResultExtensions.ValidateHitResultPair(HitResult.Great, HitResult.Good));
            Assert.That(ex!.ParamName, Is.EqualTo("minResult"));
        }

        // CF: ValidateHitResultPair | maxResult == IgnoreHit TRUE, min is not (IgnoreMiss or ComboBreak) TRUE | throws
        [Test]
        public void TestValidateRejectsIgnoreHitWithOtherMin()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => HitResultExtensions.ValidateHitResultPair(HitResult.IgnoreHit, HitResult.Miss));
            Assert.That(ex!.ParamName, Is.EqualTo("minResult"));
        }

        // CF: ValidateHitResultPair | maxResult.IsBonus() TRUE, minResult != IgnoreMiss TRUE | throws
        [TestCase(HitResult.SmallBonus)]
        [TestCase(HitResult.LargeBonus)]
        public void TestValidateRejectsBonusWithNonIgnoreMissMin(HitResult max)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => HitResultExtensions.ValidateHitResultPair(max, HitResult.Miss));
            Assert.That(ex!.ParamName, Is.EqualTo("minResult"));
        }

        // CF: ValidateHitResultPair | maxResult == SliderTailHit TRUE, minResult != LargeTickMiss TRUE | throws
        [Test]
        public void TestValidateRejectsSliderTailHitWithWrongMin()
        {
            Assert.That(() => HitResultExtensions.ValidateHitResultPair(HitResult.SliderTailHit, HitResult.Miss), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        // CF: ValidateHitResultPair | maxResult == LargeTickHit TRUE, minResult != LargeTickMiss TRUE | throws
        [Test]
        public void TestValidateRejectsLargeTickHitWithWrongMin()
        {
            Assert.That(() => HitResultExtensions.ValidateHitResultPair(HitResult.LargeTickHit, HitResult.SmallTickMiss), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        // CF: ValidateHitResultPair | maxResult == SmallTickHit TRUE, minResult != SmallTickMiss TRUE | throws
        [Test]
        public void TestValidateRejectsSmallTickHitWithWrongMin()
        {
            Assert.That(() => HitResultExtensions.ValidateHitResultPair(HitResult.SmallTickHit, HitResult.LargeTickMiss), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        // CF: ValidateHitResultPair | maxResult.IsBasic() TRUE, minResult != Miss TRUE | throws
        [Test]
        public void TestValidateRejectsBasicWithNonMissMin()
        {
            Assert.That(() => HitResultExtensions.ValidateHitResultPair(HitResult.Great, HitResult.SmallTickMiss), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
