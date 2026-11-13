// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Overlays;

namespace osu.Game.Tests.Overlays
{
    [TestFixture]
    public class OverlayColourSchemeTest
    {
        // CF: OverlayColourSchemeExtensions.GetHue | colourScheme | case Red
        // CF: OverlayColourSchemeExtensions.GetHue | colourScheme | case Orange
        // CF: OverlayColourSchemeExtensions.GetHue | colourScheme | case Lime
        // CF: OverlayColourSchemeExtensions.GetHue | colourScheme | case Green
        // CF: OverlayColourSchemeExtensions.GetHue | colourScheme | case Aquamarine
        // CF: OverlayColourSchemeExtensions.GetHue | colourScheme | case Blue
        // CF: OverlayColourSchemeExtensions.GetHue | colourScheme | case Purple
        // CF: OverlayColourSchemeExtensions.GetHue | colourScheme | case Plum
        // CF: OverlayColourSchemeExtensions.GetHue | colourScheme | case Pink
        [TestCase(OverlayColourScheme.Red, 0)]
        [TestCase(OverlayColourScheme.Orange, 45)]
        [TestCase(OverlayColourScheme.Lime, 90)]
        [TestCase(OverlayColourScheme.Green, 125)]
        [TestCase(OverlayColourScheme.Aquamarine, 160)]
        [TestCase(OverlayColourScheme.Blue, 200)]
        [TestCase(OverlayColourScheme.Purple, 255)]
        [TestCase(OverlayColourScheme.Plum, 320)]
        [TestCase(OverlayColourScheme.Pink, 333)]
        public void TestHues(OverlayColourScheme scheme, int hue) => Assert.That(scheme.GetHue(), Is.EqualTo(hue));

        // CF: OverlayColourSchemeExtensions.GetHue | colourScheme | default
        [Test]
        public void TestUnknownSchemeThrows() => Assert.Throws<ArgumentOutOfRangeException>(() => ((OverlayColourScheme)99).GetHue());
    }
}
