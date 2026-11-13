// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Difficulty.Utils;

namespace osu.Game.Tests.Rulesets.Difficulty.Utils
{
    [TestFixture]
    public class DiffUtilsTest
    {
        // CF: DiffUtils.BPMToMilliseconds | delimiter default and explicit | no throw
        // CF: DiffUtils.MillisecondsToBPM | delimiter default and explicit | no throw
        [Test]
        public void TestBpmMillisecondConversion()
        {
            Assert.That(DiffUtils.BPMToMilliseconds(120), Is.EqualTo(125));
            Assert.That(DiffUtils.BPMToMilliseconds(120, 1), Is.EqualTo(500));
            Assert.That(DiffUtils.MillisecondsToBPM(125), Is.EqualTo(120));
            Assert.That(DiffUtils.MillisecondsToBPM(500, 1), Is.EqualTo(120));
        }

        // CF: DiffUtils.Logistic | midpoint yields half of max | no throw
        [Test]
        public void TestLogistic()
        {
            Assert.That(DiffUtils.Logistic(5, 5, 1), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(DiffUtils.Logistic(5, 5, 1, 4), Is.EqualTo(2).Within(1e-12));
            Assert.That(DiffUtils.Logistic(100, 5, 1), Is.EqualTo(1).Within(1e-9));
            Assert.That(DiffUtils.Logistic(-100, 5, 1), Is.EqualTo(0).Within(1e-9));
            Assert.That(DiffUtils.Logistic(0), Is.EqualTo(0.5));
            Assert.That(DiffUtils.Logistic(0, 2), Is.EqualTo(1));
        }

        // CF: DiffUtils.Norm | foreach value | loop n
        // CF: DiffUtils.Norm | foreach value | loop 0
        [Test]
        public void TestNorm()
        {
            Assert.That(DiffUtils.Norm(2, 3, 4), Is.EqualTo(5).Within(1e-12));
            Assert.That(DiffUtils.Norm(1, 1, 2, 3), Is.EqualTo(6).Within(1e-12));
            Assert.That(DiffUtils.Norm(2), Is.Zero);
        }

        [Test]
        public void TestBellCurve()
        {
            Assert.That(DiffUtils.BellCurve(3, 3, 2), Is.EqualTo(1));
            Assert.That(DiffUtils.BellCurve(3, 3, 2, 5), Is.EqualTo(5));
            Assert.That(DiffUtils.BellCurve(100, 3, 2), Is.EqualTo(0).Within(1e-9));
        }

        // CF: DiffUtils.SmoothstepBellCurve | x > 0 | TRUE
        // CF: DiffUtils.SmoothstepBellCurve | x > 0 | FALSE
        [Test]
        public void TestSmoothstepBellCurve()
        {
            Assert.That(DiffUtils.SmoothstepBellCurve(10, 10, 4), Is.EqualTo(1));
            Assert.That(DiffUtils.SmoothstepBellCurve(12, 10, 4), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(DiffUtils.SmoothstepBellCurve(8, 10, 4), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(DiffUtils.SmoothstepBellCurve(14, 10, 4), Is.Zero);
            Assert.That(DiffUtils.SmoothstepBellCurve(6, 10, 4), Is.Zero);

            Assert.That(DiffUtils.SmoothstepBellCurve(0.5), Is.EqualTo(1));
            Assert.That(DiffUtils.SmoothstepBellCurve(0.25), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(DiffUtils.SmoothstepBellCurve(0.75), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(DiffUtils.SmoothstepBellCurve(0), Is.Zero);
            Assert.That(DiffUtils.SmoothstepBellCurve(5), Is.Zero, "out of range input clamps");
        }

        [Test]
        public void TestSmoothstepSmootherstepAndReverseLerp()
        {
            Assert.That(DiffUtils.Smoothstep(0.5, 0, 1), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(DiffUtils.Smoothstep(-1, 0, 1), Is.Zero);
            Assert.That(DiffUtils.Smoothstep(2, 0, 1), Is.EqualTo(1));

            Assert.That(DiffUtils.Smootherstep(0.5, 0, 1), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(DiffUtils.Smootherstep(-1, 0, 1), Is.Zero);
            Assert.That(DiffUtils.Smootherstep(2, 0, 1), Is.EqualTo(1));

            Assert.That(DiffUtils.ReverseLerp(15, 10, 20), Is.EqualTo(0.5));
            Assert.That(DiffUtils.ReverseLerp(5, 10, 20), Is.Zero);
            Assert.That(DiffUtils.ReverseLerp(25, 10, 20), Is.EqualTo(1));
        }

        // CF: DiffUtils.Erf | x == 0 | TRUE
        // CF: DiffUtils.Erf | double.IsPositiveInfinity(x) | TRUE
        // CF: DiffUtils.Erf | double.IsNegativeInfinity(x) | TRUE
        // CF: DiffUtils.Erf | double.IsNaN(x) | TRUE
        // CF: DiffUtils.Erf | x == 0 | FALSE
        // CF: DiffUtils.Erf | double.IsPositiveInfinity(x) | FALSE
        // CF: DiffUtils.Erf | double.IsNegativeInfinity(x) | FALSE
        // CF: DiffUtils.Erf | double.IsNaN(x) | FALSE
        // CF: DiffUtils.Erf | x >= 0 | TRUE
        // CF: DiffUtils.Erf | x >= 0 | FALSE
        [Test]
        public void TestErf()
        {
            Assert.That(DiffUtils.Erf(0), Is.Zero);
            Assert.That(DiffUtils.Erf(double.PositiveInfinity), Is.EqualTo(1));
            Assert.That(DiffUtils.Erf(double.NegativeInfinity), Is.EqualTo(-1));
            Assert.That(double.IsNaN(DiffUtils.Erf(double.NaN)), Is.True);
            Assert.That(DiffUtils.Erf(1), Is.EqualTo(0.8427007929).Within(1e-6));
            Assert.That(DiffUtils.Erf(-1), Is.EqualTo(-0.8427007929).Within(1e-6));
            Assert.That(DiffUtils.Erfc(1), Is.EqualTo(1 - 0.8427007929).Within(1e-6));
        }

        // CF: DiffUtils.ErfInv | x <= -1 | TRUE
        // CF: DiffUtils.ErfInv | x >= 1 | TRUE
        // CF: DiffUtils.ErfInv | x == 0 | TRUE
        // CF: DiffUtils.ErfInv | x <= -1 | FALSE
        // CF: DiffUtils.ErfInv | x >= 1 | FALSE
        // CF: DiffUtils.ErfInv | x == 0 | FALSE
        // CF: DiffUtils.ErfInv | x >= 0.85 | TRUE
        // CF: DiffUtils.ErfInv | x >= 0.85 | FALSE
        [Test]
        public void TestErfInv()
        {
            Assert.That(DiffUtils.ErfInv(-1), Is.EqualTo(double.NegativeInfinity));
            Assert.That(DiffUtils.ErfInv(1), Is.EqualTo(double.PositiveInfinity));
            Assert.That(DiffUtils.ErfInv(0), Is.Zero);
            Assert.That(DiffUtils.ErfInv(0.5), Is.EqualTo(0.4769362762).Within(1e-3));
            Assert.That(DiffUtils.ErfInv(-0.5), Is.EqualTo(-0.4769362762).Within(1e-3));
            Assert.That(DiffUtils.ErfInv(0.9), Is.EqualTo(1.1630871537).Within(5e-3));
            Assert.That(DiffUtils.ErfcInv(0.5), Is.EqualTo(DiffUtils.ErfInv(0.5)));
        }

        // CF: DiffUtils.Pow | exponent switch 0..5 | case 0
        // CF: DiffUtils.Pow | exponent switch | default
        [Test]
        public void TestPow()
        {
            Assert.That(DiffUtils.Pow(3.0, 0), Is.EqualTo(1));
            Assert.That(DiffUtils.Pow(3.0, 1), Is.EqualTo(3));
            Assert.That(DiffUtils.Pow(3.0, 2), Is.EqualTo(9));
            Assert.That(DiffUtils.Pow(3.0, 3), Is.EqualTo(27));
            Assert.That(DiffUtils.Pow(3.0, 4), Is.EqualTo(81));
            Assert.That(DiffUtils.Pow(3.0, 5), Is.EqualTo(243));
            Assert.That(DiffUtils.Pow(2.0, 6), Is.EqualTo(64));
            Assert.That(DiffUtils.Pow(2.0, -1), Is.EqualTo(0.5));
            Assert.That(DiffUtils.Pow(4.0, 0.5), Is.EqualTo(2));
            Assert.That(DiffUtils.SQRT2, Is.EqualTo(Math.Sqrt(2)).Within(1e-15));
        }
    }
}
