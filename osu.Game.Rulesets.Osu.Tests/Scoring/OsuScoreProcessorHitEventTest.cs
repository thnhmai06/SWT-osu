// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Scoring
{
    [TestFixture]
    public class OsuScoreProcessorHitEventTest
    {
        private sealed class Exposed : OsuScoreProcessor
        {
            public HitEvent Create(JudgementResult result) => CreateHitEvent(result);
        }

        // `result as OsuHitCircleJudgementResult` is non-null: cursor position is copied into the event.
        [Test]
        public void TestCircleResult_CarriesCursorPosition()
        {
            var circle = new HitCircle();
            var result = new OsuHitCircleJudgementResult(circle, new OsuJudgement()) { CursorPositionAtHit = new Vector2(12, 34) };

            Assert.That(new Exposed().Create(result).Position, Is.EqualTo(new Vector2(12, 34)));
        }

        // `as` yields null (other result type): position stays null.
        [Test]
        public void TestNonCircleResult_HasNoPosition()
        {
            var result = new JudgementResult(new HitCircle(), new OsuJudgement());

            Assert.That(new Exposed().Create(result).Position, Is.Null);
        }

        // Circle result whose cursor position is unset: null position (nullable propagates).
        [Test]
        public void TestCircleResultWithoutCursor_HasNoPosition()
        {
            var result = new OsuHitCircleJudgementResult(new HitCircle(), new OsuJudgement());

            Assert.That(new Exposed().Create(result).Position, Is.Null);
        }
    }
}
