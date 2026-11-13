// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Graphics;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tests.Screens.Edit
{
    [TestFixture]
    public class BindableBeatDivisorTest
    {
        // CF: BindableBeatDivisor.ensureValidDivisor | !ValidDivisors.Value.Presets.Contains(Value) | FALSE
        [Test]
        public void TestValidInitialValueKept()
        {
            Assert.That(new BindableBeatDivisor(4).Value, Is.EqualTo(4));
            Assert.That(new BindableBeatDivisor().Value, Is.EqualTo(1));
        }

        // CF: BindableBeatDivisor.ensureValidDivisor | !ValidDivisors.Value.Presets.Contains(Value) | TRUE
        [Test]
        public void TestInvalidInitialValueResets()
        {
            Assert.That(new BindableBeatDivisor(3).Value, Is.EqualTo(1));
        }

        // CF: BindableBeatDivisor.SetArbitraryDivisor | divisor < MINIMUM_DIVISOR || divisor > MAXIMUM_DIVISOR | TRUE
        // CF: BindableBeatDivisor.SetArbitraryDivisor | divisor < MINIMUM_DIVISOR | TRUE
        // CF: BindableBeatDivisor.SetArbitraryDivisor | divisor > MAXIMUM_DIVISOR | TRUE
        [Test]
        public void TestSetArbitraryOutOfRange()
        {
            var d = new BindableBeatDivisor(4);
            Assert.That(d.SetArbitraryDivisor(0), Is.False);
            Assert.That(d.SetArbitraryDivisor(65), Is.False);
            Assert.That(d.Value, Is.EqualTo(4));
        }

        // CF: BindableBeatDivisor.SetArbitraryDivisor | divisor < MINIMUM_DIVISOR || divisor > MAXIMUM_DIVISOR | FALSE
        // CF: BindableBeatDivisor.SetArbitraryDivisor | preferKnownPresets || !ValidDivisors.Value.Presets.Contains(divisor) | FALSE
        [Test]
        public void TestSetArbitraryWithinCurrentPresets()
        {
            var d = new BindableBeatDivisor();
            Assert.That(d.SetArbitraryDivisor(8), Is.True);
            Assert.That(d.Value, Is.EqualTo(8));
            Assert.That(d.ValidDivisors.Value, Is.SameAs(BeatDivisorPresetCollection.COMMON));
        }

        // CF: BindableBeatDivisor.SetArbitraryDivisor | !ValidDivisors.Value.Presets.Contains(divisor) | TRUE
        // CF: BindableBeatDivisor.SetArbitraryDivisor | COMMON.Presets.Contains(divisor) | FALSE
        // CF: BindableBeatDivisor.SetArbitraryDivisor | TRIPLETS.Presets.Contains(divisor) | TRUE
        [Test]
        public void TestSetArbitraryTriplet()
        {
            var d = new BindableBeatDivisor();
            d.SetArbitraryDivisor(3);
            Assert.That(d.ValidDivisors.Value, Is.SameAs(BeatDivisorPresetCollection.TRIPLETS));
            Assert.That(d.Value, Is.EqualTo(3));
        }

        // CF: BindableBeatDivisor.SetArbitraryDivisor | TRIPLETS.Presets.Contains(divisor) | FALSE
        [Test]
        public void TestSetArbitraryCustom()
        {
            var d = new BindableBeatDivisor();
            d.SetArbitraryDivisor(7);
            Assert.That(d.ValidDivisors.Value.Type, Is.EqualTo(BeatDivisorType.Custom));
            Assert.That(d.Value, Is.EqualTo(7));
            Assert.That(d.MaxValue, Is.EqualTo(7));
            Assert.That(d.MinValue, Is.EqualTo(1));
        }

        // CF: BindableBeatDivisor.SetArbitraryDivisor | preferKnownPresets | TRUE
        // CF: BindableBeatDivisor.SetArbitraryDivisor | COMMON.Presets.Contains(divisor) | TRUE
        [Test]
        public void TestPreferKnownPresetsSwitchesBackToCommon()
        {
            var d = new BindableBeatDivisor();
            d.SetArbitraryDivisor(3);
            d.SetArbitraryDivisor(1, true);
            Assert.That(d.ValidDivisors.Value, Is.SameAs(BeatDivisorPresetCollection.COMMON));
        }

        // CF: BindableBeatDivisor.SelectNext | presets after current exist | non-null
        [Test]
        public void TestSelectNext()
        {
            var d = new BindableBeatDivisor(2);
            d.SelectNext();
            Assert.That(d.Value, Is.EqualTo(4));
        }

        // CF: BindableBeatDivisor.SelectNext | presets after current exist | null
        [Test]
        public void TestSelectNextAtEnd()
        {
            var d = new BindableBeatDivisor(16);
            d.SelectNext();
            Assert.That(d.Value, Is.EqualTo(16));
        }

        // CF: BindableBeatDivisor.SelectPrevious | presets before current exist | non-null
        [Test]
        public void TestSelectPrevious()
        {
            var d = new BindableBeatDivisor(4);
            d.SelectPrevious();
            Assert.That(d.Value, Is.EqualTo(2));
        }

        // CF: BindableBeatDivisor.SelectPrevious | presets before current exist | null
        [Test]
        public void TestSelectPreviousAtStart()
        {
            var d = new BindableBeatDivisor(1);
            d.SelectPrevious();
            Assert.That(d.Value, Is.EqualTo(1));
        }

        // CF: BindableBeatDivisor.BindTo | them is BindableBeatDivisor | TRUE
        [Test]
        public void TestBindToAnotherDivisorTransfersPresets()
        {
            var other = new BindableBeatDivisor();
            other.SetArbitraryDivisor(3);

            var d = new BindableBeatDivisor();
            d.BindTo(other);

            Assert.That(d.ValidDivisors.Value, Is.SameAs(BeatDivisorPresetCollection.TRIPLETS));
            Assert.That(d.Value, Is.EqualTo(3));
        }

        // CF: BindableBeatDivisor.BindTo | them is BindableBeatDivisor | FALSE
        [Test]
        public void TestBindToPlainBindable()
        {
            var d = new BindableBeatDivisor();
            var other = new BindableInt(2);
            d.BindTo(other);
            Assert.That(d.Value, Is.EqualTo(2));
        }

        [Test]
        public void TestCreateInstanceOnBoundCopy()
        {
            Assert.That(new BindableBeatDivisor().GetBoundCopy(), Is.TypeOf<BindableBeatDivisor>());
        }

        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 1
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 2
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 4
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 8
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 16
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 3
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 6
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 12
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 5
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 7
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | case 9
        // CF: BindableBeatDivisor.GetColourFor | beatDivisor | default
        [Test]
        public void TestColours()
        {
            var c = new OsuColour();
            Assert.That(BindableBeatDivisor.GetColourFor(1, c), Is.EqualTo(Color4.White));
            Assert.That(BindableBeatDivisor.GetColourFor(2, c), Is.EqualTo(c.Red));
            Assert.That(BindableBeatDivisor.GetColourFor(4, c), Is.EqualTo(c.Blue));
            Assert.That(BindableBeatDivisor.GetColourFor(8, c), Is.EqualTo(c.Yellow));
            Assert.That(BindableBeatDivisor.GetColourFor(16, c), Is.EqualTo(c.PurpleDark));
            Assert.That(BindableBeatDivisor.GetColourFor(3, c), Is.EqualTo(c.Purple));
            Assert.That(BindableBeatDivisor.GetColourFor(6, c), Is.EqualTo(c.YellowDark));
            Assert.That(BindableBeatDivisor.GetColourFor(12, c), Is.EqualTo(c.YellowDarker));
            Assert.That(BindableBeatDivisor.GetColourFor(5, c), Is.EqualTo(c.GreenLight));
            Assert.That(BindableBeatDivisor.GetColourFor(7, c), Is.EqualTo(c.GreenLight));
            Assert.That(BindableBeatDivisor.GetColourFor(9, c), Is.EqualTo(c.GreenLight));
            Assert.That(BindableBeatDivisor.GetColourFor(10, c), Is.EqualTo(Color4.Red));
        }

        // CF: BindableBeatDivisor.GetSize | beatDivisor | case 1
        // CF: BindableBeatDivisor.GetSize | beatDivisor | case 2
        // CF: BindableBeatDivisor.GetSize | beatDivisor | case 3
        // CF: BindableBeatDivisor.GetSize | beatDivisor | case 4
        // CF: BindableBeatDivisor.GetSize | beatDivisor | case 6
        // CF: BindableBeatDivisor.GetSize | beatDivisor | case 8
        // CF: BindableBeatDivisor.GetSize | beatDivisor | default
        [TestCase(1, 1f, 0.9f)]
        [TestCase(2, 1f, 0.9f)]
        [TestCase(3, 0.8f, 0.8f)]
        [TestCase(4, 0.8f, 0.8f)]
        [TestCase(6, 0.8f, 0.7f)]
        [TestCase(8, 0.8f, 0.7f)]
        [TestCase(16, 0.8f, 0.6f)]
        public void TestSize(int divisor, float x, float y)
        {
            Assert.That(BindableBeatDivisor.GetSize(divisor), Is.EqualTo(new Vector2(x, y)));
        }

        // CF: BindableBeatDivisor.GetDivisorForBeatIndex | validDivisors ??= PREDEFINED_DIVISORS | null
        // CF: BindableBeatDivisor.GetDivisorForBeatIndex | foreach validDivisors | loop n
        // CF: BindableBeatDivisor.GetDivisorForBeatIndex | (beat * divisor) % beatDivisor == 0 | TRUE
        // CF: BindableBeatDivisor.GetDivisorForBeatIndex | (beat * divisor) % beatDivisor == 0 | FALSE
        [TestCase(0, 4, 1)]
        [TestCase(1, 4, 4)]
        [TestCase(2, 4, 2)]
        [TestCase(5, 4, 4)]
        public void TestDivisorForBeatIndexDefaults(int index, int beatDivisor, int expected)
        {
            Assert.That(BindableBeatDivisor.GetDivisorForBeatIndex(index, beatDivisor), Is.EqualTo(expected));
        }

        // CF: BindableBeatDivisor.GetDivisorForBeatIndex | validDivisors ??= PREDEFINED_DIVISORS | non-null
        // CF: BindableBeatDivisor.GetDivisorForBeatIndex | foreach validDivisors | loop 0
        [Test]
        public void TestDivisorForBeatIndexNoMatch()
        {
            Assert.That(BindableBeatDivisor.GetDivisorForBeatIndex(1, 4, new[] { 1, 2 }), Is.EqualTo(0));
            Assert.That(BindableBeatDivisor.GetDivisorForBeatIndex(1, 4, new int[0]), Is.EqualTo(0));
        }
    }
}
