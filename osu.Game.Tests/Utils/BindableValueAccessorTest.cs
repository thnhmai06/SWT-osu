// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Moq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Utils;

namespace osu.Game.Tests.Utils
{
    [TestFixture]
    public class BindableValueAccessorTest
    {
        // CF: BindableValueAccessor.GetValue | bindableWithValueType == null | FALSE
        [Test]
        public void TestGetValueOfTypedBindable() => Assert.That(BindableValueAccessor.GetValue(new Bindable<int>(5)), Is.EqualTo(5));

        // CF: BindableValueAccessor.GetValue | bindableWithValueType == null | TRUE
        [Test]
        public void TestGetValueOfUntypedBindableReturnsItself()
        {
            var untyped = new Mock<IBindable>().Object;
            Assert.That(BindableValueAccessor.GetValue(untyped), Is.SameAs(untyped));
        }

        // CF: BindableValueAccessor.SetValue | bindableWithValueType == null | FALSE
        [Test]
        public void TestSetValue()
        {
            var bindable = new BindableDouble(1);
            BindableValueAccessor.SetValue(bindable, 2.5);
            Assert.That(bindable.Value, Is.EqualTo(2.5));
        }

        // CF: BindableValueAccessor.SetValue | bindableWithValueType == null | TRUE
        [Test]
        public void TestSetValueOnUntypedBindableIgnored()
        {
            Assert.DoesNotThrow(() => BindableValueAccessor.SetValue(new Mock<IBindable>().Object, 1));
        }
    }
}
