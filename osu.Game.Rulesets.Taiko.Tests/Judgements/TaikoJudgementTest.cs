// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Judgements;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Judgements
{
    [TestFixture]
    public class TaikoJudgementTest
    {
        private static double healthFor(HitResult result)
            => new JudgementResult(new Hit(), new TaikoJudgement()) { Type = result }.HealthIncrease;

        // CF: TaikoJudgement.HealthIncreaseFor | case Miss | case Miss
        [Test]
        public void TestMiss() => Assert.That(healthFor(HitResult.Miss), Is.EqualTo(-1.0));

        // CF: TaikoJudgement.HealthIncreaseFor | case Ok | case Ok
        [Test]
        public void TestOk() => Assert.That(healthFor(HitResult.Ok), Is.EqualTo(1.1));

        // CF: TaikoJudgement.HealthIncreaseFor | case Great | case Great
        [Test]
        public void TestGreat() => Assert.That(healthFor(HitResult.Great), Is.EqualTo(3.0));

        // CF: TaikoJudgement.HealthIncreaseFor | switch (result) | default
        [Test]
        public void TestOtherResultGivesNoHealth() => Assert.That(healthFor(HitResult.Meh), Is.EqualTo(0));
    }
}
