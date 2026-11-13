// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Tests.Rulesets.Mods
{
    [TestFixture]
    public class DifficultyBindableTest
    {
        // CF: DifficultyBindable.Value | value != null | TRUE
        // CF: DifficultyBindable.Value | extended limits unset: clamp to [MinValue,MaxValue] | TRUE
        [Test]
        public void TestValueClampedToDefaultRange()
        {
            var bindable = new DifficultyBindable { Value = 15 };
            Assert.That(bindable.Value, Is.EqualTo(10));

            bindable.Value = -5;
            Assert.That(bindable.Value, Is.Zero);
        }

        // CF: DifficultyBindable.Value | value != null | FALSE
        [Test]
        public void TestNullValuePassesThrough()
        {
            var bindable = new DifficultyBindable(5) { Value = null };
            Assert.That(bindable.Value, Is.Null);
        }

        // CF: DifficultyBindable.Value | ExtendedMaxValue ?? MaxValue | non-null
        // CF: DifficultyBindable.Value | ExtendedMinValue ?? MinValue | non-null
        [Test]
        public void TestValueBeyondDefaultRangeAllowedWithinExtendedRange()
        {
            var bindable = new DifficultyBindable { ExtendedMaxValue = 11, ExtendedMinValue = -1 };

            bindable.Value = 11;
            Assert.That(bindable.Value, Is.EqualTo(11));

            bindable.Value = -1;
            Assert.That(bindable.Value, Is.EqualTo(-1));

            bindable.Value = 20;
            Assert.That(bindable.Value, Is.EqualTo(11));
        }

        // CF: DifficultyBindable.MinValue | value == minValue | TRUE
        // CF: DifficultyBindable.MinValue | value == minValue | FALSE
        // CF: DifficultyBindable.MaxValue | value == maxValue | TRUE
        // CF: DifficultyBindable.MaxValue | value == maxValue | FALSE
        // CF: DifficultyBindable.ExtendedMinValue | value == extendedMinValue | TRUE
        // CF: DifficultyBindable.ExtendedMinValue | value == extendedMinValue | FALSE
        // CF: DifficultyBindable.ExtendedMaxValue | value == extendedMaxValue | TRUE
        // CF: DifficultyBindable.ExtendedMaxValue | value == extendedMaxValue | FALSE
        [Test]
        public void TestLimitSettersIgnoreUnchangedValuesAndUpdateCurrentNumber()
        {
            var bindable = new DifficultyBindable();

            bindable.MaxValue = 10; // unchanged
            bindable.MinValue = 0; // unchanged
            bindable.ExtendedMaxValue = null; // unchanged
            bindable.ExtendedMinValue = null; // unchanged
            Assert.That(bindable.CurrentNumber.MaxValue, Is.EqualTo(10));

            bindable.MaxValue = 5;
            bindable.MinValue = 1;
            Assert.That(bindable.CurrentNumber.MaxValue, Is.EqualTo(5));
            Assert.That(bindable.CurrentNumber.MinValue, Is.EqualTo(1));

            bindable.ExtendedMaxValue = 8;
            bindable.ExtendedMinValue = -2;
            Assert.That(bindable.CurrentNumber.MaxValue, Is.EqualTo(5), "extended limits inactive");

            bindable.ExtendedLimits.Value = true;
            Assert.That(bindable.CurrentNumber.MaxValue, Is.EqualTo(8));
            Assert.That(bindable.CurrentNumber.MinValue, Is.EqualTo(-2));
        }

        // CF: DifficultyBindable.updateExtents | ExtendedLimits.Value && extendedMinValue != null | FALSE
        // CF: DifficultyBindable.updateExtents | ExtendedLimits.Value && extendedMaxValue != null | FALSE
        [Test]
        public void TestExtendedLimitsWithoutExtendedValuesKeepsDefaultRange()
        {
            var bindable = new DifficultyBindable();
            bindable.ExtendedLimits.Value = true;

            Assert.That(bindable.CurrentNumber.MaxValue, Is.EqualTo(10));
            Assert.That(bindable.CurrentNumber.MinValue, Is.Zero);
        }

        // CF: DifficultyBindable.Precision | set | no throw
        [Test]
        public void TestPrecisionForwardsToCurrentNumber()
        {
            var bindable = new DifficultyBindable { Precision = 0.5f };
            Assert.That(bindable.CurrentNumber.Precision, Is.EqualTo(0.5f));
        }

        // CF: DifficultyBindable.CopyTo | them is DifficultyBindable | TRUE
        [Test]
        public void TestCopyToCopiesLimitsAndReader()
        {
            Func<osu.Game.Beatmaps.IBeatmapDifficultyInfo, float> reader = d => d.CircleSize;
            var source = new DifficultyBindable(3) { MinValue = 1, MaxValue = 7, ExtendedMinValue = 0, ExtendedMaxValue = 9, ReadCurrentFromDifficulty = reader };
            var target = new DifficultyBindable();

            source.CopyTo(target);

            Assert.That(target.MinValue, Is.EqualTo(1));
            Assert.That(target.MaxValue, Is.EqualTo(7));
            Assert.That(target.ExtendedMinValue, Is.EqualTo(0));
            Assert.That(target.ExtendedMaxValue, Is.EqualTo(9));
            Assert.That(target.ReadCurrentFromDifficulty, Is.SameAs(reader));
            Assert.That(target.Value, Is.EqualTo(3));
        }

        // CF: DifficultyBindable.CopyTo | them is DifficultyBindable | FALSE
        // CF: DifficultyBindable.BindTo | them is DifficultyBindable | FALSE
        // CF: DifficultyBindable.UnbindFrom | them is DifficultyBindable | FALSE
        [Test]
        public void TestNonDifficultyBindablesRejected()
        {
            var bindable = new DifficultyBindable();

            Assert.Throws<InvalidOperationException>(() => bindable.CopyTo(new Bindable<float?>()));
            Assert.Throws<InvalidOperationException>(() => bindable.BindTo(new Bindable<float?>()));
            Assert.Throws<InvalidOperationException>(() => bindable.UnbindFrom(new Bindable<float?>()));
        }

        // CF: DifficultyBindable.BindTo | them is DifficultyBindable | TRUE
        // CF: DifficultyBindable.UnbindFrom | them is DifficultyBindable | TRUE
        // CF: DifficultyBindable.CreateInstance | new DifficultyBindable | non-null
        [Test]
        public void TestBindAndUnbindShareValueAndExtendedLimits()
        {
            var a = new DifficultyBindable { ExtendedMaxValue = 12 };
            var b = new DifficultyBindable();

            b.BindTo(a);

            a.ExtendedLimits.Value = true;
            Assert.That(b.ExtendedLimits.Value, Is.True);
            Assert.That(b.ExtendedMaxValue, Is.EqualTo(12));

            a.Value = 11;
            Assert.That(b.Value, Is.EqualTo(11));

            b.UnbindFrom(a);
            a.Value = 3;
            Assert.That(b.Value, Is.EqualTo(11));

            Assert.That(a.GetUnboundCopy(), Is.TypeOf<DifficultyBindable>());
        }
    }
}
