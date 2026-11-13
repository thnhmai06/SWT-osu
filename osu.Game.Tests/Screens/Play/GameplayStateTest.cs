// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Storyboards;

namespace osu.Game.Tests.Screens.Play
{
    [TestFixture]
    public class GameplayStateTest
    {
        private static IBeatmap beatmap()
        {
            var b = new Beatmap<HitCircle> { BeatmapInfo = { Ruleset = new OsuRuleset().RulesetInfo } };
            b.HitObjects.Add(new HitCircle { StartTime = 1000 });
            return b;
        }

        // CF: GameplayState.ctor | score ?? new Score | null
        // CF: GameplayState.ctor | mods ?? Array.Empty | null
        // CF: GameplayState.ctor | scoreProcessor ?? CreateScoreProcessor | null
        // CF: GameplayState.ctor | healthProcessor ?? CreateHealthProcessor | null
        // CF: GameplayState.ctor | storyboard ?? new Storyboard | null
        // CF: GameplayState.ctor | localUserPlayingState != null | FALSE
        [Test]
        public void TestDefaultsAreCreated()
        {
            var ruleset = new OsuRuleset();
            var b = beatmap();
            var state = new GameplayState(b, ruleset);

            Assert.That(state.Score.ScoreInfo.BeatmapInfo, Is.SameAs(b.BeatmapInfo));
            Assert.That(state.Score.ScoreInfo.Ruleset, Is.EqualTo(ruleset.RulesetInfo));
            Assert.That(state.Mods, Is.Empty);
            Assert.That(state.ScoreProcessor, Is.Not.Null);
            Assert.That(state.HealthProcessor, Is.Not.Null);
            Assert.That(state.Storyboard, Is.Not.Null);
            Assert.That(state.PlayingState.Value, Is.EqualTo(LocalUserPlayingState.NotPlaying));
        }

        // CF: GameplayState.ctor | score ?? new Score | non-null
        // CF: GameplayState.ctor | mods ?? Array.Empty | non-null
        // CF: GameplayState.ctor | scoreProcessor ?? CreateScoreProcessor | non-null
        // CF: GameplayState.ctor | healthProcessor ?? CreateHealthProcessor | non-null
        // CF: GameplayState.ctor | storyboard ?? new Storyboard | non-null
        // CF: GameplayState.ctor | localUserPlayingState != null | TRUE
        [Test]
        public void TestProvidedValuesAreUsed()
        {
            var ruleset = new OsuRuleset();
            var score = new Score();
            var mods = new Mod[] { new OsuModHidden() };
            var scoreProcessor = ruleset.CreateScoreProcessor();
            var healthProcessor = ruleset.CreateHealthProcessor(0);
            var storyboard = new Storyboard();
            var playing = new Bindable<LocalUserPlayingState>(LocalUserPlayingState.Break);

            var state = new GameplayState(beatmap(), ruleset, mods, score, scoreProcessor, healthProcessor, storyboard, playing);

            Assert.That(state.Score, Is.SameAs(score));
            Assert.That(state.Mods, Is.SameAs(mods));
            Assert.That(state.ScoreProcessor, Is.SameAs(scoreProcessor));
            Assert.That(state.HealthProcessor, Is.SameAs(healthProcessor));
            Assert.That(state.Storyboard, Is.SameAs(storyboard));
            Assert.That(state.PlayingState.Value, Is.EqualTo(LocalUserPlayingState.Break));

            playing.Value = LocalUserPlayingState.Playing;
            Assert.That(state.PlayingState.Value, Is.EqualTo(LocalUserPlayingState.Playing));
        }

        // CF: GameplayState.HasCompleted | HasPassed || HasFailed || HasQuit | FALSE
        [Test]
        public void TestNotCompletedByDefault() => Assert.That(new GameplayState(beatmap(), new OsuRuleset()).HasCompleted, Is.False);

        // CF: GameplayState.HasCompleted | HasPassed | TRUE
        // CF: GameplayState.HasCompleted | HasFailed | TRUE
        // CF: GameplayState.HasCompleted | HasQuit | TRUE
        [Test]
        public void TestCompletedByAnyFlag()
        {
            Assert.That(new GameplayState(beatmap(), new OsuRuleset()) { HasPassed = true }.HasCompleted, Is.True);
            Assert.That(new GameplayState(beatmap(), new OsuRuleset()) { HasFailed = true }.HasCompleted, Is.True);
            Assert.That(new GameplayState(beatmap(), new OsuRuleset()) { HasQuit = true }.HasCompleted, Is.True);
        }

        [Test]
        public void TestApplyResultAndSamples()
        {
            var state = new GameplayState(beatmap(), new OsuRuleset());
            var hitObject = new HitCircle();
            var result = new JudgementResult(hitObject, hitObject.Judgement);
            var samples = new ISampleInfo[] { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) };

            state.ApplyResult(result);
            state.ApplySamples(samples);

            Assert.That(state.LastJudgementResult.Value, Is.SameAs(result));
            Assert.That(state.LastPlayedSamples.Value, Is.SameAs(samples));
        }
    }
}
