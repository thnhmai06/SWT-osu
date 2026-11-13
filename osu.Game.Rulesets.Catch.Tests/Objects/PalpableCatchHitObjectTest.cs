// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Catch.Objects;

namespace osu.Game.Rulesets.Catch.Tests.Objects
{
    [TestFixture]
    public class PalpableCatchHitObjectTest
    {
        // CF: PalpableCatchHitObject.HyperDashTarget setter | value != null | TRUE -> HyperDash on
        [Test]
        public void TestSettingTargetEnablesHyperDash()
        {
            var fruit = new Fruit { HyperDashTarget = new Fruit() };

            Assert.That(fruit.HyperDash, Is.True);
            Assert.That(fruit.HyperDashBindable.Value, Is.True);
        }

        // CF: PalpableCatchHitObject.HyperDashTarget setter | value != null | FALSE -> HyperDash off
        [Test]
        public void TestClearingTargetDisablesHyperDash()
        {
            var fruit = new Fruit { HyperDashTarget = new Fruit() };
            fruit.HyperDashTarget = null;

            Assert.That(fruit.HyperDash, Is.False);
            Assert.That(fruit.HyperDashTarget, Is.Null);
        }
    }
}
