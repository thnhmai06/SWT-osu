// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Tests.Rulesets.Mods
{
    [TestFixture]
    public class ModBarrelRollTest
    {
        private CultureInfo previous = null!;

        [SetUp]
        public void SetUp()
        {
            previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = previous;

        // CF: ModBarrelRoll.SettingDescription | !SpinSpeed.IsDefault | FALSE
        // CF: ModBarrelRoll.SettingDescription | !Direction.IsDefault | FALSE
        [Test]
        public void TestNoDescriptionByDefault()
        {
            Assert.That(new OsuModBarrelRoll().SettingDescription, Is.Empty);
        }

        // CF: ModBarrelRoll.SettingDescription | !SpinSpeed.IsDefault | TRUE
        [Test]
        public void TestSpeedDescription()
        {
            var mod = new OsuModBarrelRoll();
            mod.SpinSpeed.Value = 1;

            var description = mod.SettingDescription.Single();

            Assert.That((description.setting.ToString(), description.value.ToString()), Is.EqualTo(("Roll speed", "1.00 rpm")));
        }

        // CF: ModBarrelRoll.SettingDescription | !Direction.IsDefault | TRUE
        [Test]
        public void TestDirectionDescription()
        {
            var mod = new OsuModBarrelRoll();
            mod.Direction.Value = RotationDirection.Counterclockwise;

            var description = mod.SettingDescription.Single();

            Assert.That(description.setting.ToString(), Is.EqualTo("Direction"));
            Assert.That(description.value.ToString(), Is.Not.Empty);
        }
    }
}
