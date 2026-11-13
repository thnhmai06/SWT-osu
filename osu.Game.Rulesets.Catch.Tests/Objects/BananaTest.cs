// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Catch.Tests.Objects
{
    [TestFixture]
    public class BananaTest
    {
        // CF: Banana.getBananaColour | switch StatelessRNG.NextInt(3, RandomSeed) | case 1
        // CF: Banana.getBananaColour | switch StatelessRNG.NextInt(3, RandomSeed) | case 2
        // CF: Banana.getBananaColour | switch StatelessRNG.NextInt(3, RandomSeed) | default
        [Test]
        public void TestColourIsOneOfThreeBananaColoursAndDeterministic()
        {
            var colours = new HashSet<Color4>();

            for (int seed = 0; seed < 100; seed++)
            {
                var banana = new Banana { StartTime = seed };
                var colour = ((IHasComboInformation)banana).GetComboColour(null!);
                colours.Add(colour);

                Assert.That(((IHasComboInformation)new Banana { StartTime = seed }).GetComboColour(null!), Is.EqualTo(colour));
            }

            Assert.That(colours, Is.EquivalentTo(new[]
            {
                new Color4(255, 240, 0, 255),
                new Color4(255, 192, 0, 255),
                new Color4(214, 221, 28, 255),
            }));
        }

        [Test]
        public void TestDefaultSampleIsBananaSample()
        {
            Assert.That(new Banana().Samples, Has.Count.EqualTo(1));
            Assert.That(new Banana().Samples[0], Is.TypeOf<Banana.BananaHitSampleInfo>());
        }

        // CF: BananaHitSampleInfo.Equals | other != null | TRUE
        // CF: BananaHitSampleInfo.Equals | other != null | FALSE
        // CF: BananaHitSampleInfo.Equals | obj is BananaHitSampleInfo other | TRUE
        // CF: BananaHitSampleInfo.Equals | obj is BananaHitSampleInfo other | FALSE
        [Test]
        public void TestBananaSampleEquality()
        {
            var sample = new Banana.BananaHitSampleInfo();

            Assert.That(sample.Equals((Banana.BananaHitSampleInfo?)null), Is.False);
            Assert.That(sample.Equals(new Banana.BananaHitSampleInfo(new HitSampleInfo("x", "y"))), Is.True);
            Assert.That(EqualityComparer<object>.Default.Equals(sample, new Banana.BananaHitSampleInfo()), Is.True);
            Assert.That(EqualityComparer<object>.Default.Equals(sample, new HitSampleInfo("x")), Is.False);
            Assert.That(sample.GetHashCode(), Is.EqualTo(new Banana.BananaHitSampleInfo().GetHashCode()));
        }

        [Test]
        public void TestWithPreservesBananaSampleType()
        {
            Assert.That(new Banana.BananaHitSampleInfo().With(newVolume: 50), Is.TypeOf<Banana.BananaHitSampleInfo>());
        }
    }
}
