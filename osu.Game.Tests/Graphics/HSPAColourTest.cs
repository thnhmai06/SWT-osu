// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Graphics;
using osuTK.Graphics;

namespace osu.Game.Tests.Graphics
{
    [TestFixture]
    public class HSPAColourTest
    {
        private static void assertRoundTrip(float r, float g, float b, float a = 1f)
        {
            var original = new Color4(r, g, b, a);
            var back = new HSPAColour(original).ToColor4();

            Assert.That(back.R, Is.EqualTo(r).Within(1e-3), "R");
            Assert.That(back.G, Is.EqualTo(g).Within(1e-3), "G");
            Assert.That(back.B, Is.EqualTo(b).Within(1e-3), "B");
            Assert.That(back.A, Is.EqualTo(a).Within(1e-6), "A");
        }

        [Test]
        public void TestConstructorStoresComponents()
        {
            var c = new HSPAColour(0.1f, 0.2f, 0.3f, 0.4f);
            Assert.That((c.H, c.S, c.P, c.A), Is.EqualTo((0.1f, 0.2f, 0.3f, 0.4f)));
        }

        // CF: HSPAColour.ctor | colour.R == colour.G && colour.R == colour.B | TRUE
        // CF: HSPAColour.ctor | colour.R == colour.G | TRUE
        // CF: HSPAColour.ctor | colour.R == colour.B | TRUE
        [Test]
        public void TestGreyHasNoHueOrSaturation()
        {
            var c = new HSPAColour(new Color4(0.5f, 0.5f, 0.5f, 0.25f));
            Assert.That(c.H, Is.EqualTo(0));
            Assert.That(c.S, Is.EqualTo(0));
            Assert.That(c.P, Is.EqualTo(0.5f).Within(1e-5));
            Assert.That(c.A, Is.EqualTo(0.25f));
            assertRoundTrip(0.5f, 0.5f, 0.5f, 0.25f);
        }

        // CF: HSPAColour.ctor | colour.R == colour.G | FALSE
        // CF: HSPAColour.ctor | colour.R == colour.B | FALSE
        // CF: HSPAColour.ctor | R >= G && R >= B | TRUE
        // CF: HSPAColour.ctor | B >= G (red dominant) | FALSE
        // CF: HSPAColour.ctor | h < 1/6 (min over max > 0) | TRUE
        [Test]
        public void TestRedYellowSector() => assertRoundTrip(1f, 0.6f, 0.4f);

        // CF: HSPAColour.ctor | B >= G (red dominant) | TRUE
        // CF: HSPAColour.ctor | h >= 5/6 (min over max > 0) | TRUE
        [Test]
        public void TestRedMagentaSector() => assertRoundTrip(1f, 0.4f, 0.6f);

        // CF: HSPAColour.ctor | R >= G && R >= B | FALSE
        // CF: HSPAColour.ctor | G >= R && G >= B | TRUE
        // CF: HSPAColour.ctor | R >= B (green dominant) | TRUE
        // CF: HSPAColour.ctor | h < 2/6 (min over max > 0) | TRUE
        [Test]
        public void TestGreenYellowSector() => assertRoundTrip(0.6f, 1f, 0.4f);

        // CF: HSPAColour.ctor | R >= B (green dominant) | FALSE
        // CF: HSPAColour.ctor | h < 3/6 (min over max > 0) | TRUE
        [Test]
        public void TestGreenCyanSector() => assertRoundTrip(0.4f, 1f, 0.6f);

        // CF: HSPAColour.ctor | G >= R && G >= B | FALSE
        // CF: HSPAColour.ctor | G >= R (blue dominant) | TRUE
        // CF: HSPAColour.ctor | h < 4/6 (min over max > 0) | TRUE
        [Test]
        public void TestBlueCyanSector() => assertRoundTrip(0.4f, 0.6f, 1f);

        // CF: HSPAColour.ctor | G >= R (blue dominant) | FALSE
        // CF: HSPAColour.ctor | h < 5/6 (min over max > 0) | TRUE
        [Test]
        public void TestBluePurpleSector() => assertRoundTrip(0.6f, 0.4f, 1f);

        // CF: HSPAColour.ToColor4 | minOverMax > 0f | FALSE
        // CF: HSPAColour.ToColor4 | h < 1/6 (fully saturated) | TRUE
        [Test]
        public void TestSaturatedSector0() => assertRoundTrip(1f, 0.5f, 0f);

        // CF: HSPAColour.ToColor4 | h < 2/6 (fully saturated) | TRUE
        [Test]
        public void TestSaturatedSector1() => assertRoundTrip(0.5f, 1f, 0f);

        // CF: HSPAColour.ToColor4 | h < 3/6 (fully saturated) | TRUE
        [Test]
        public void TestSaturatedSector2() => assertRoundTrip(0f, 1f, 0.5f);

        // CF: HSPAColour.ToColor4 | h < 4/6 (fully saturated) | TRUE
        [Test]
        public void TestSaturatedSector3() => assertRoundTrip(0f, 0.5f, 1f);

        // CF: HSPAColour.ToColor4 | h < 5/6 (fully saturated) | TRUE
        [Test]
        public void TestSaturatedSector4() => assertRoundTrip(0.5f, 0f, 1f);

        // CF: HSPAColour.ToColor4 | h < 5/6 (fully saturated) | FALSE
        [Test]
        public void TestSaturatedSector5() => assertRoundTrip(1f, 0f, 0.5f);

        // CF: HSPAColour.ToColor4 | h < 1/6 (min over max > 0) | FALSE
        // CF: HSPAColour.ToColor4 | h < 2/6 (min over max > 0) | FALSE
        // CF: HSPAColour.ToColor4 | h < 3/6 (min over max > 0) | FALSE
        // CF: HSPAColour.ToColor4 | h < 4/6 (min over max > 0) | FALSE
        // CF: HSPAColour.ToColor4 | h < 5/6 (min over max > 0) | FALSE
        [Test]
        public void TestUnsaturatedSectorsCoveredByRoundTrips()
        {
            assertRoundTrip(0.9f, 0.5f, 0.2f);
            assertRoundTrip(0.2f, 0.5f, 0.9f);
        }
    }
}
