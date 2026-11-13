// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Tests.Rulesets.Objects
{
    [TestFixture]
    public class HitObjectPropertyTest
    {
        // CF: HitObjectProperty.Value | backingBindable != null | FALSE
        [Test]
        public void TestValueWithoutBindableIsStoredDirectly()
        {
            var property = new HitObjectProperty<int>(5);
            Assert.That(property.Value, Is.EqualTo(5));

            property.Value = 7;
            Assert.That(property.Value, Is.EqualTo(7));
        }

        [Test]
        public void TestDefaultConstructedProperty()
        {
            Assert.That(default(HitObjectProperty<int>).Value, Is.Zero);
            Assert.That(new HitObjectProperty<string>("a").Value, Is.EqualTo("a"));
        }

        // CF: HitObjectProperty.Bindable | backingBindable ??= | TRUE (created lazily from the current value)
        [Test]
        public void TestBindableCreatedLazilyFromCurrentValue()
        {
            var property = new HitObjectProperty<int>(5);
            property.Value = 9;

            var bindable = property.Bindable;

            Assert.That(bindable.Value, Is.EqualTo(9));
            Assert.That(bindable.Default, Is.EqualTo(5), "default is the constructor value");
        }

        // CF: HitObjectProperty.Bindable | backingBindable ??= | FALSE
        [Test]
        public void TestBindableInstanceReused()
        {
            var property = new HitObjectProperty<int>(5);

            Assert.That(property.Bindable, Is.SameAs(property.Bindable));
        }

        // CF: HitObjectProperty.Value | backingBindable != null | TRUE
        [Test]
        public void TestValueRoutedThroughBindableOnceCreated()
        {
            var property = new HitObjectProperty<int>(5);
            var bindable = property.Bindable;

            property.Value = 11;
            Assert.That(bindable.Value, Is.EqualTo(11));

            bindable.Value = 12;
            Assert.That(property.Value, Is.EqualTo(12));
        }
    }
}
