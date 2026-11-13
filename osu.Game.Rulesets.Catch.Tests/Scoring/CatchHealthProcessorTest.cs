// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Judgements;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Catch.Tests.Scoring
{
    [TestFixture]
    public class CatchHealthProcessorTest
    {
        private static CatchHealthProcessor createProcessor(CatchHitObject hitObject, double startingHealth, float drainRate = 5)
        {
            var processor = new CatchHealthProcessor(0);
            var beatmap = new CatchBeatmap { HitObjects = { hitObject } };
            beatmap.Difficulty.DrainRate = drainRate;
            processor.ApplyBeatmap(beatmap);
            processor.Health.Value = startingHealth;
            return processor;
        }

        private static void apply(CatchHealthProcessor processor, CatchHitObject hitObject, HitResult type)
        {
            var result = new CatchJudgementResult(hitObject, hitObject.CreateJudgement()) { Type = type };
            processor.ApplyResult(result);
        }

        // CF: CheckDefaultFailCondition | result.Type == SmallTickMiss | TRUE -> never fails
        [Test]
        public void TestTinyDropletMissCannotFail()
        {
            var tiny = new TinyDroplet();
            var processor = createProcessor(tiny, 0);
            apply(processor, tiny, HitResult.SmallTickMiss);

            Assert.That(processor.HasFailed, Is.False);
        }

        // CF: CheckDefaultFailCondition | result.HitObject is BananaShower | TRUE -> never fails
        [Test]
        public void TestBananaShowerMissCannotFail()
        {
            var shower = new BananaShower();
            var processor = createProcessor(shower, 0);
            apply(processor, shower, HitResult.Miss);

            Assert.That(processor.HasFailed, Is.False);
        }

        // CF: CheckDefaultFailCondition | SmallTickMiss FALSE, BananaShower FALSE -> defers to base (fails at zero health)
        [Test]
        public void TestFruitMissAtLowHealthFails()
        {
            var fruit = new Fruit();
            var processor = createProcessor(fruit, 0.01);
            apply(processor, fruit, HitResult.Miss);

            Assert.That(processor.HasFailed, Is.True);
        }

        // CF: CheckDefaultFailCondition | defers to base -> no fail when result is max result
        [Test]
        public void TestFruitHitDoesNotFail()
        {
            var fruit = new Fruit();
            var processor = createProcessor(fruit, 0.01);
            apply(processor, fruit, HitResult.Great);

            Assert.That(processor.HasFailed, Is.False);
        }

        // CF: GetHealthIncreaseFor | case SmallTickMiss | returns 0
        [Test]
        public void TestSmallTickMissLeavesHealthUnchanged()
        {
            var droplet = new TinyDroplet();
            var processor = createProcessor(droplet, 0.5);
            apply(processor, droplet, HitResult.SmallTickMiss);

            Assert.That(processor.Health.Value, Is.EqualTo(0.5));
        }

        // CF: GetHealthIncreaseFor | cases SmallTickHit / LargeTickHit / Great / LargeBonus | positive increase
        [TestCase(HitResult.SmallTickHit)]
        [TestCase(HitResult.LargeTickHit)]
        [TestCase(HitResult.Great)]
        [TestCase(HitResult.LargeBonus)]
        public void TestHitResultsIncreaseHealth(HitResult type)
        {
            var droplet = new Droplet();
            var processor = createProcessor(droplet, 0.5);
            apply(processor, droplet, type);

            Assert.That(processor.Health.Value, Is.GreaterThan(0.5));
        }

        // CF: GetHealthIncreaseFor | cases LargeTickMiss / Miss | negative increase
        [TestCase(HitResult.LargeTickMiss)]
        [TestCase(HitResult.Miss)]
        public void TestMissResultsDecreaseHealth(HitResult type)
        {
            var droplet = new Droplet();
            var processor = createProcessor(droplet, 0.5);
            apply(processor, droplet, type);

            Assert.That(processor.Health.Value, Is.LessThan(0.5));
        }

        // CF: GetHealthIncreaseFor | Miss penalty scales with DrainRate (DifficultyRange)
        [Test]
        public void TestMissPenaltyGrowsWithDrainRate()
        {
            var easy = new Droplet();
            var easyProcessor = createProcessor(easy, 0.5, 0);
            apply(easyProcessor, easy, HitResult.Miss);

            var hard = new Droplet();
            var hardProcessor = createProcessor(hard, 0.5, 10);
            apply(hardProcessor, hard, HitResult.Miss);

            Assert.That(hardProcessor.Health.Value, Is.LessThan(easyProcessor.Health.Value));
        }

        // CF: GetHealthIncreaseFor | switch with no matching case (e.g. Ok) | falls through, increase = 0
        [Test]
        public void TestUnhandledResultLeavesHealthUnchanged()
        {
            var droplet = new Droplet();
            var processor = createProcessor(droplet, 0.5);
            apply(processor, droplet, HitResult.Ok);

            Assert.That(processor.Health.Value, Is.EqualTo(0.5));
        }
    }
}
