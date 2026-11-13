// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Scoring;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Mania.Tests.Scoring
{
    [TestFixture]
    public class ManiaHealthProcessorTest
    {
        private const float drain_rate = 5;

        private class ExposedHealthProcessor : ManiaHealthProcessor
        {
            public ExposedHealthProcessor()
                : base(0)
            {
            }

            public double Increase(HitObject hitObject, HitResult result) => GetHealthIncreaseFor(hitObject, result);

            public double Multiplier => HpMultiplierNormal;
        }

        private static ExposedHealthProcessor create()
        {
            var processor = new ExposedHealthProcessor();
            processor.ApplyBeatmap(new ManiaBeatmap(new StageDefinition(4))
            {
                Difficulty = new BeatmapDifficulty { DrainRate = (float)drain_rate },
                HitObjects =
                {
                    new Note { StartTime = 0 },
                    new Note { StartTime = 1000 },
                }
            });
            return processor;
        }

        // CF: ManiaHealthProcessor.ComputeDrainRate | return 0 (no passive drain) | non-null
        [Test]
        public void TestNoPassiveDrain()
        {
            Assert.That(create().DrainRate, Is.Zero);
        }

        // CF: ManiaHealthProcessor.EnumerateTopLevelHitObjects | Beatmap.HitObjects | non-null
        // CF: ManiaHealthProcessor.EnumerateNestedHitObjects | hitObject.NestedHitObjects | non-null
        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | case Miss | case Miss
        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | case HeadNote/TailNote (inner switch) | case HeadNote
        [Test]
        public void TestMissOnHeadNote()
        {
            Assert.That(create().Increase(new HeadNote(), HitResult.Miss), Is.EqualTo(-(drain_rate + 1) * 0.00375).Within(1e-12));
        }

        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | case HeadNote/TailNote (inner switch) | case TailNote
        [Test]
        public void TestMissOnTailNote()
        {
            Assert.That(create().Increase(new TailNote(), HitResult.Miss), Is.EqualTo(-(drain_rate + 1) * 0.00375).Within(1e-12));
        }

        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | inner switch (hitObject) | default
        [Test]
        public void TestMissOnNote()
        {
            Assert.That(create().Increase(new Note(), HitResult.Miss), Is.EqualTo(-(drain_rate + 1) * 0.0075).Within(1e-12));
        }

        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | case Meh | case Meh
        [Test]
        public void TestMeh()
        {
            Assert.That(create().Increase(new Note(), HitResult.Meh), Is.EqualTo(-(drain_rate + 1) * 0.0016).Within(1e-12));
        }

        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | case Ok | case Ok
        [Test]
        public void TestOk()
        {
            Assert.That(create().Increase(new Note(), HitResult.Ok), Is.Zero);
        }

        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | case Good | case Good
        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | case Great | case Great
        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | case Perfect | case Perfect
        [Test]
        public void TestPositiveResultsScaledByMultiplier()
        {
            var p = create();
            double m = p.Multiplier;

            Assert.That(p.Increase(new Note(), HitResult.Good), Is.EqualTo(m * (0.004 - drain_rate * 0.0004)).Within(1e-12));
            Assert.That(p.Increase(new Note(), HitResult.Great), Is.EqualTo(m * (0.005 - drain_rate * 0.0005)).Within(1e-12));
            Assert.That(p.Increase(new Note(), HitResult.Perfect), Is.EqualTo(m * (0.0055 - drain_rate * 0.0005)).Within(1e-12));
        }

        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | switch(result) no case matches | default
        [Test]
        public void TestUnhandledResultGivesNoIncrease()
        {
            Assert.That(create().Increase(new Note(), HitResult.None), Is.Zero);
        }

        private static readonly object[][] fail_cases =
        [
            // hitobject, starting HP
            [new Note(), 0.01],
            [new HeadNote(), 0.01],
            [new TailNote(), 0.01],
        ];

        // CF: ManiaHealthProcessor.GetHealthIncreaseFor | case Miss | case Miss
        [TestCaseSource(nameof(fail_cases))]
        public void TestFailAfterMinResult(ManiaHitObject hitObject, double startingHealth)
        {
            var processor = new ManiaHealthProcessor(0);
            processor.ApplyBeatmap(new ManiaBeatmap(new StageDefinition(4)) { HitObjects = { hitObject } });
            processor.Health.Value = startingHealth;

            var result = new JudgementResult(hitObject, hitObject.CreateJudgement()) { Type = HitResult.Miss };
            processor.ApplyResult(result);

            Assert.That(processor.HasFailed, Is.True);
        }
    }
}
