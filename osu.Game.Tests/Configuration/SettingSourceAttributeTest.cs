// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;

namespace osu.Game.Tests.Configuration
{
    [TestFixture]
    public class SettingSourceAttributeTest
    {
        private static class Strings
        {
            public static readonly LocalisableString FIELD_LABEL = "field-label";
            public static LocalisableString PropertyLabel => "property-label";
            public static void Method() { }
        }

        private class Settings
        {
            [SettingSource("Unordered")]
            public Bindable<int> Unordered { get; } = new Bindable<int>();

            [SettingSource("Second", "d", 2)]
            public Bindable<int> Second { get; } = new Bindable<int>();

            [SettingSource("First", "d", 1)]
            public Bindable<int> First { get; } = new Bindable<int>();

            public Bindable<int> NotASetting { get; } = new Bindable<int>();
        }

        private class DerivedSettings : Settings
        {
            [SettingSource("Extra", "d", 0)]
            public Bindable<bool> Extra { get; } = new Bindable<bool>();
        }

        // CF: SettingSourceAttribute.ctor(string) | label ?? string.Empty | non-null
        // CF: SettingSourceAttribute.ctor(string) | description ?? string.Empty | non-null
        [Test]
        public void TestPlainStringConstructor()
        {
            var attr = new SettingSourceAttribute("label", "description");

            Assert.That(attr.Label, Is.EqualTo((LocalisableString)"label"));
            Assert.That(attr.Description, Is.EqualTo((LocalisableString)"description"));
            Assert.That(attr.OrderPosition, Is.Null);
        }

        // CF: SettingSourceAttribute.ctor(string) | label ?? string.Empty | null
        // CF: SettingSourceAttribute.ctor(string) | description ?? string.Empty | null
        [Test]
        public void TestNullStringsBecomeEmpty()
        {
            var attr = new SettingSourceAttribute(null, null);

            Assert.That(attr.Label, Is.EqualTo((LocalisableString)string.Empty));
            Assert.That(attr.Description, Is.EqualTo((LocalisableString)string.Empty));
        }

        // CF: SettingSourceAttribute.ctor(string, string, int) | orderPosition assigned | no throw
        [Test]
        public void TestOrderedStringConstructor() => Assert.That(new SettingSourceAttribute("l", "d", 5).OrderPosition, Is.EqualTo(5));

        // CF: SettingSourceAttribute.ctor(Type) | member == null | TRUE
        [Test]
        public void TestTypeConstructorNullDescription()
        {
            var attr = new SettingSourceAttribute(typeof(Strings), nameof(Strings.FIELD_LABEL));

            Assert.That(attr.Description, Is.EqualTo((LocalisableString)string.Empty));
        }

        // CF: SettingSourceAttribute.ctor(Type) | member == null | FALSE
        // CF: SettingSourceAttribute.ctor(Type) | property == null | FALSE
        // CF: SettingSourceAttribute.ctor(Type) | switch property | case FieldInfo
        [Test]
        public void TestTypeConstructorField() => Assert.That(new SettingSourceAttribute(typeof(Strings), nameof(Strings.FIELD_LABEL)).Label, Is.EqualTo((LocalisableString)"field-label"));

        // CF: SettingSourceAttribute.ctor(Type) | switch property | case PropertyInfo
        [Test]
        public void TestTypeConstructorProperty() => Assert.That(new SettingSourceAttribute(typeof(Strings), nameof(Strings.PropertyLabel)).Label, Is.EqualTo((LocalisableString)"property-label"));

        // CF: SettingSourceAttribute.ctor(Type) | property == null | TRUE
        [Test]
        public void TestTypeConstructorMissingMemberIsEmpty() => Assert.That(new SettingSourceAttribute(typeof(Strings), "Missing").Label, Is.EqualTo((LocalisableString)string.Empty));

        // CF: SettingSourceAttribute.ctor(Type) | switch property | default
        // CF: SettingSourceAttribute.ctor(Type) | non field/property member | throws InvalidOperationException
        [Test]
        public void TestTypeConstructorMethodMemberThrows() => Assert.Throws<InvalidOperationException>(() => new SettingSourceAttribute(typeof(Strings), nameof(Strings.Method)));

        // CF: SettingSourceAttribute.ctor(Type, string, string, int) | orderPosition assigned | no throw
        [Test]
        public void TestTypeConstructorWithOrder() => Assert.That(new SettingSourceAttribute(typeof(Strings), nameof(Strings.FIELD_LABEL), "d", 3).OrderPosition, Is.EqualTo(3));

        // CF: SettingSourceAttribute.CompareTo | OrderPosition == other?.OrderPosition | TRUE
        [Test]
        public void TestCompareEqualPositions()
        {
            Assert.That(new SettingSourceAttribute("a", "d", 1).CompareTo(new SettingSourceAttribute("b", "d", 1)), Is.EqualTo(0));
            Assert.That(new SettingSourceAttribute("a").CompareTo(new SettingSourceAttribute("b")), Is.EqualTo(0));
        }

        // CF: SettingSourceAttribute.CompareTo | OrderPosition == null | TRUE
        [Test]
        public void TestUnorderedSortsAfterOrdered() => Assert.That(new SettingSourceAttribute("a").CompareTo(new SettingSourceAttribute("b", "d", 1)), Is.EqualTo(1));

        // CF: SettingSourceAttribute.CompareTo | OrderPosition == null | FALSE
        // CF: SettingSourceAttribute.CompareTo | other?.OrderPosition == null | TRUE
        [Test]
        public void TestOrderedSortsBeforeUnordered()
        {
            Assert.That(new SettingSourceAttribute("a", "d", 1).CompareTo(new SettingSourceAttribute("b")), Is.EqualTo(-1));
            Assert.That(new SettingSourceAttribute("a", "d", 1).CompareTo(null), Is.EqualTo(-1));
        }

        // CF: SettingSourceAttribute.CompareTo | other?.OrderPosition == null | FALSE
        [Test]
        public void TestOrderedComparedByPosition()
        {
            Assert.That(new SettingSourceAttribute("a", "d", 1).CompareTo(new SettingSourceAttribute("b", "d", 2)), Is.LessThan(0));
            Assert.That(new SettingSourceAttribute("a", "d", 3).CompareTo(new SettingSourceAttribute("b", "d", 2)), Is.GreaterThan(0));
        }

        // CF: SettingSourceExtensions.getSettingsSourceProperties | attr == null | TRUE
        // CF: SettingSourceExtensions.getSettingsSourceProperties | attr == null | FALSE
        // CF: SettingSourceExtensions.GetSettingsSourceProperties | property_info_cache.TryGetValue | FALSE
        [Test]
        public void TestOnlyAttributedPropertiesAreReturned()
        {
            var names = new Settings().GetSettingsSourceProperties().Select(p => p.Item2.Name).ToArray();

            Assert.That(names, Is.EquivalentTo(new[] { nameof(Settings.Unordered), nameof(Settings.Second), nameof(Settings.First) }));
        }

        // CF: SettingSourceExtensions.GetSettingsSourceProperties | property_info_cache.TryGetValue | TRUE
        [Test]
        public void TestCachedLookupReturnsSameResult()
        {
            var obj = new Settings();

            Assert.That(obj.GetSettingsSourceProperties().Select(p => p.Item2.Name), Is.EqualTo(obj.GetSettingsSourceProperties().Select(p => p.Item2.Name)));
        }

        // CF: SettingSourceExtensions.GetOrderedSettingsSourceProperties | OrderBy attribute | no throw
        [Test]
        public void TestOrderedPropertiesAreSortedAndUnorderedLast()
        {
            var names = new DerivedSettings().GetOrderedSettingsSourceProperties().Select(p => p.Item2.Name).ToArray();

            Assert.That(names, Is.EqualTo(new[] { nameof(DerivedSettings.Extra), nameof(Settings.First), nameof(Settings.Second), nameof(Settings.Unordered) }));
        }

        // CF: SettingSourceExtensions.GetUnderlyingSettingValue | switch setting | case Bindable<double>
        [Test]
        public void TestUnderlyingDouble() => Assert.That(new Bindable<double>(1.5).GetUnderlyingSettingValue(), Is.EqualTo(1.5));

        // CF: SettingSourceExtensions.GetUnderlyingSettingValue | switch setting | case Bindable<int>
        [Test]
        public void TestUnderlyingInt() => Assert.That(new Bindable<int>(3).GetUnderlyingSettingValue(), Is.EqualTo(3));

        // CF: SettingSourceExtensions.GetUnderlyingSettingValue | switch setting | case Bindable<float>
        [Test]
        public void TestUnderlyingFloat() => Assert.That(new Bindable<float>(2.5f).GetUnderlyingSettingValue(), Is.EqualTo(2.5f));

        // CF: SettingSourceExtensions.GetUnderlyingSettingValue | switch setting | case Bindable<bool>
        [Test]
        public void TestUnderlyingBool() => Assert.That(new Bindable<bool>(true).GetUnderlyingSettingValue(), Is.EqualTo(true));

        // CF: SettingSourceExtensions.GetUnderlyingSettingValue | switch setting | case BindableColour4
        [Test]
        public void TestUnderlyingColourIsHexString()
        {
            object value = new BindableColour4(Colour4.Red).GetUnderlyingSettingValue();

            Assert.That(value, Is.TypeOf<string>());
            Assert.That(((string)value).ToUpperInvariant(), Does.Contain("FF0000"));
        }

        // CF: SettingSourceExtensions.GetUnderlyingSettingValue | switch setting | case IBindable
        [Test]
        public void TestUnderlyingOtherBindable() => Assert.That(new Bindable<string>("text").GetUnderlyingSettingValue(), Is.EqualTo("text"));

        // CF: SettingSourceExtensions.GetUnderlyingSettingValue | switch setting | default
        [Test]
        public void TestUnderlyingNonBindableReturnsInput() => Assert.That("plain".GetUnderlyingSettingValue(), Is.EqualTo("plain"));
    }
}
