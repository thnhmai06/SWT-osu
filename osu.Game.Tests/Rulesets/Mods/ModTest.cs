// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Tests.Rulesets.Mods
{
    [TestFixture]
    public class ModTest
    {
        private class TestMod : Mod
        {
            public override string Name => "Test";
            public override string Acronym => "TS";
            public override LocalisableString Description => "desc";

            [SettingSource("Flag", "flag", 0)]
            public BindableBool Flag { get; } = new BindableBool(true);

            [SettingSource("Number", "number", 1)]
            public BindableInt Number { get; } = new BindableInt(5) { MinValue = 0, MaxValue = 10 };
        }

        private class ImplementedMod : TestMod, IApplicableMod
        {
        }

        private class OtherMod : Mod
        {
            public override string Name => "Other";
            public override string Acronym => "OT";
            public override LocalisableString Description => "other";

            // same setting name as TestMod.Flag but with an incompatible bindable type.
            [SettingSource("Flag")]
            public BindableInt Flag { get; } = new BindableInt(3);

            [SettingSource("Number")]
            public BindableInt Number { get; } = new BindableInt(8) { MinValue = 0, MaxValue = 10 };

            [SettingSource("Unique")]
            public BindableInt Unique { get; } = new BindableInt(1);
        }

        // CF: Mod.SettingDescription | foreach setting | loop 0
        // CF: Mod.SettingDescription | bindable.IsDefault | TRUE
        [Test]
        public void TestDefaultSettingsAreNotDescribed()
        {
            Assert.That(new TestMod().SettingDescription, Is.Empty);
        }

        // CF: Mod.SettingDescription | foreach setting | loop n
        // CF: Mod.SettingDescription | bindable.IsDefault | FALSE
        // CF: Mod.GetSettingTooltipText | case Bindable<bool> | case Bindable<bool>
        // CF: Mod.GetSettingTooltipText | b.Value | FALSE
        // CF: Mod.GetSettingTooltipText | default | default
        [Test]
        public void TestChangedSettingsAreDescribed()
        {
            var mod = new TestMod { Flag = { Value = false }, Number = { Value = 7 } };

            var described = mod.SettingDescription.Select(d => (d.setting.ToString(), d.value.ToString())).ToArray();
            Assert.That(described, Is.EqualTo(new[] { ("Flag", "Off"), ("Number", "7") }));
        }

        // CF: Mod.GetSettingTooltipText | b.Value | TRUE
        [Test]
        public void TestBooleanTooltipOn()
        {
            var mod = new TestMod();
            mod.Flag.Default = false;
            mod.Flag.Value = true;

            Assert.That(mod.SettingDescription.Single().value.ToString(), Is.EqualTo("On"));
        }

        [Test]
        public void TestDefaultProperties()
        {
            var mod = new TestMod();

            Assert.That(mod.Type, Is.EqualTo(ModType.Fun));
            Assert.That(mod.UserPlayable, Is.True);
            Assert.That(mod.Ranked, Is.False);
            Assert.That(mod.IncompatibleMods, Is.Empty);
            Assert.That(mod.ExtendedIconInformation, Is.Empty);
            Assert.That(mod.Icon, Is.Null);
        }

        // CF: Mod.HasImplementation | this is IApplicableMod | FALSE
        // CF: Mod.HasImplementation | this is IApplicableMod | TRUE
        [Test]
        public void TestHasImplementation()
        {
            Assert.That(new TestMod().HasImplementation, Is.False);
            Assert.That(new ImplementedMod().HasImplementation, Is.True);
        }

        // CF: Mod.UsesDefaultConfiguration | SettingsBindables.All(IsDefault) | TRUE
        // CF: Mod.UsesDefaultConfiguration | SettingsBindables.All(IsDefault) | FALSE
        [Test]
        public void TestUsesDefaultConfiguration()
        {
            var mod = new TestMod();
            Assert.That(mod.UsesDefaultConfiguration, Is.True);

            mod.Number.Value = 6;
            Assert.That(mod.UsesDefaultConfiguration, Is.False);
        }

        // CF: Mod.DeepClone | Activator.CreateInstance + CopyFrom | non-null
        // CF: Mod.CopyFrom | source.GetType() != GetType() | FALSE
        // CF: Mod.CopyFrom | foreach setting | loop n
        // CF: Mod.CopyAdjustedSetting | source is IBindable | TRUE
        [Test]
        public void TestDeepCloneCopiesSettings()
        {
            var original = new TestMod { Flag = { Value = false }, Number = { Value = 9 } };

            var clone = (TestMod)original.DeepClone();

            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Flag.Value, Is.False);
            Assert.That(clone.Number.Value, Is.EqualTo(9));

            clone.Number.Value = 1;
            Assert.That(original.Number.Value, Is.EqualTo(9), "clone is not bound to the original");
        }

        // CF: Mod.CopyFrom | source.GetType() != GetType() | TRUE
        // CF: Mod.CopyFrom | source.GetType() != GetType() | throws ArgumentException
        [Test]
        public void TestCopyFromDifferentTypeThrows()
        {
            Assert.Throws<ArgumentException>(() => new TestMod().CopyFrom(new OtherMod()));
        }

        // CF: Mod.CopyAdjustedSetting | source is IBindable | FALSE
        // CF: Mod.CopyAdjustedSetting | target is IParseable | TRUE
        [Test]
        public void TestCopyAdjustedSettingFromRawValueParses()
        {
            var mod = new TestMod();

            mod.CopyAdjustedSetting(mod.Number, 4);

            Assert.That(mod.Number.Value, Is.EqualTo(4));
        }

        // CF: Mod.CopyAdjustedSetting | target is IParseable | FALSE
        // CF: Mod.CopyAdjustedSetting | target is IParseable | throws InvalidOperationException
        [Test]
        public void TestCopyAdjustedSettingToNonParseableThrows()
        {
            var mod = new TestMod();

            Assert.Throws<InvalidOperationException>(() => mod.CopyAdjustedSetting(Mock.Of<IBindable>(), 4));
        }

        [Test]
        public void TestResetSettingsToDefaults()
        {
            var mod = new TestMod { Number = { Value = 9 }, Flag = { Value = false } };

            mod.ResetSettingsToDefaults();

            Assert.That(mod.UsesDefaultConfiguration, Is.True);
            Assert.That(mod.Number.Value, Is.EqualTo(5));
        }

        // CF: Mod.CopyCommonSettingsFrom | source.UsesDefaultConfiguration | TRUE
        [Test]
        public void TestCopyCommonSettingsFromDefaultSourceIsNoOp()
        {
            var target = new TestMod { Number = { Value = 2 } };

            target.CopyCommonSettingsFrom(new OtherMod());

            Assert.That(target.Number.Value, Is.EqualTo(2));
        }

        // CF: Mod.CopyCommonSettingsFrom | source.UsesDefaultConfiguration | FALSE
        // CF: Mod.CopyCommonSettingsFrom | foreach target setting | loop n
        // CF: Mod.CopyCommonSettingsFrom | !source.SettingsMap.TryGetValue | TRUE
        // CF: Mod.CopyCommonSettingsFrom | sourceSetting.IsDefault | TRUE
        // CF: Mod.CopyCommonSettingsFrom | incompatible bindable types | TRUE
        // CF: Mod.CopyCommonSettingsFrom | compatible bindable types | FALSE
        [Test]
        public void TestCopyCommonSettingsFromOtherMod()
        {
            var target = new TestMod();
            var source = new OtherMod { Number = { Value = 9 }, Unique = { Value = 5 } };

            target.CopyCommonSettingsFrom(source);

            Assert.That(target.Number.Value, Is.EqualTo(9), "same name and compatible type is copied");
            Assert.That(target.Flag.Value, Is.True, "incompatible bindable type (bool vs int) is skipped");
        }

        [Test]
        public void TestCopyCommonSettingsSkipsDefaultSourceSettings()
        {
            var target = new TestMod { Number = { Value = 2 } };
            var source = new OtherMod { Unique = { Value = 5 } }; // Number left at its default of 8.

            target.CopyCommonSettingsFrom(source);

            Assert.That(target.Number.Value, Is.EqualTo(2), "a default-valued source setting does not overwrite the target");
        }

        // CF: Mod.Equals(Mod) | ReferenceEquals(null, other) | TRUE
        // CF: Mod.Equals(Mod) | ReferenceEquals(this, other) | TRUE
        // CF: Mod.Equals(Mod) | GetType() == other.GetType() && SequenceEqual | TRUE
        // CF: Mod.Equals(Mod) | GetType() == other.GetType() | FALSE
        // CF: Mod.Equals(Mod) | SequenceEqual | FALSE
        // CF: Mod.Equals(IMod) | other is Mod them | FALSE
        // CF: Mod.GetHashCode | foreach setting | loop n
        [Test]
        public void TestEquality()
        {
            var a = new TestMod { Number = { Value = 6 } };
            var b = new TestMod { Number = { Value = 6 } };
            var c = new TestMod { Number = { Value = 7 } };

            Assert.That(a.Equals((Mod?)null), Is.False);
            Assert.That(a.Equals(a), Is.True);
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.Equals(c), Is.False);
            Assert.That(a.Equals(new ImplementedMod { Number = { Value = 6 } }), Is.False, "different type");
            Assert.That(a.Equals((IMod)b), Is.True);
            Assert.That(a.Equals(Mock.Of<IMod>()), Is.False);
        }
    }
}
