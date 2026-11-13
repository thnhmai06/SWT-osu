// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns.Legacy;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Mania.Tests.Beatmaps.Patterns.Legacy
{
    [TestFixture]
    public class PassThroughPatternGeneratorTest
    {
        private static ManiaHitObject generate(HitObject o, int columns = 4)
        {
            var patterns = new PassThroughPatternGenerator(new LegacyRandom(1), o, PatternTestHelper.LowDifficulty(), columns, new Pattern()).Generate().ToArray();
            Assert.That(patterns, Has.Length.EqualTo(1));
            return patterns[0].HitObjects.Single();
        }

        // CF: PassThroughPatternGenerator.Generate | positionData?.X ?? 0 | non-null
        // CF: PassThroughPatternGenerator.Generate | HitObject is IHasDuration | FALSE
        [Test]
        public void TestPlainObjectBecomesNoteInColumnOfX()
        {
            var note = generate(new FakeCircle { StartTime = 123, X = 300 });

            Assert.That(note, Is.TypeOf<Note>());
            Assert.That(note.StartTime, Is.EqualTo(123));
            Assert.That(note.Column, Is.EqualTo(2));
        }

        // CF: PassThroughPatternGenerator.Generate | positionData?.X ?? 0 | null
        // CF: PassThroughPatternGenerator.Generate | HitObject is IHasDuration | TRUE
        // CF: PassThroughPatternGenerator.Generate | (HitObject is IHasLegacyHitObjectType && LegacyType == Slider) | FALSE
        // CF: PassThroughPatternGenerator.Generate | HitObject is IHasLegacyHitObjectType | FALSE
        // CF: PassThroughPatternGenerator.Generate | HitObject is IHasPath | FALSE
        // CF: PassThroughPatternGenerator.Generate | (HitObject as IHasRepeats)?.NodeSamples ?? default | null
        [Test]
        public void TestDurationWithoutPositionBecomesHoldInFirstColumn()
        {
            var hold = (HoldNote)generate(new FakeNoPositionDuration { StartTime = 100, Duration = 400 });

            Assert.That(hold.Column, Is.EqualTo(0));
            Assert.That(hold.EndTime, Is.EqualTo(500));
            Assert.That(hold.PlaySlidingSamples, Is.False);
            Assert.That(hold.NodeSamples, Has.Count.EqualTo(2));
        }

        // CF: PassThroughPatternGenerator.Generate | HitObject is IHasLegacyHitObjectType | TRUE
        // CF: PassThroughPatternGenerator.Generate | hasType.LegacyType == Slider | FALSE
        [Test]
        public void TestLegacySpinnerTypeDoesNotPlaySlidingSamples()
        {
            var hold = (HoldNote)generate(new FakeDurationObject { Duration = 100, X = 0, LegacyType = LegacyHitObjectType.Spinner });
            Assert.That(hold.PlaySlidingSamples, Is.False);
        }

        // CF: PassThroughPatternGenerator.Generate | hasType.LegacyType == Slider | TRUE
        [Test]
        public void TestLegacySliderTypePlaysSlidingSamples()
        {
            var hold = (HoldNote)generate(new FakeDurationObject { Duration = 100, X = 0, LegacyType = LegacyHitObjectType.Slider });
            Assert.That(hold.PlaySlidingSamples, Is.True);
        }

        // CF: PassThroughPatternGenerator.Generate | HitObject is IHasPath | TRUE
        // CF: PassThroughPatternGenerator.Generate | (HitObject as IHasRepeats)?.NodeSamples ?? default | non-null
        [Test]
        public void TestPathObjectPlaysSlidingSamplesAndKeepsNodeSamples()
        {
            var slider = new FakeSlider(100, 2) { StartTime = 0, Duration = 500 };
            var hold = (HoldNote)generate(slider);

            Assert.That(hold.PlaySlidingSamples, Is.True);
            Assert.That(hold.NodeSamples, Is.SameAs(slider.NodeSamples));
        }
    }
}
