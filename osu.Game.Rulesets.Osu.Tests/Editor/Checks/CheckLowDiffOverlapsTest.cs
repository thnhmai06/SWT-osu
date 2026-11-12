// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using static osu.Game.Rulesets.Osu.Tests.Editor.Checks.CheckTestHelpers;

namespace osu.Game.Rulesets.Osu.Tests.Editor.Checks
{
    [TestFixture]
    public class CheckLowDiffOverlapsTest
    {
        private readonly CheckLowDiffOverlaps check = new CheckLowDiffOverlaps();

        // Radius 64 => objects overlap when closer than (64 - 5) * 2 = 118px.
        private const float apart = 300;
        private const float close = 50;

        private static System.Type? issueType(HitObject[] objects, DifficultyRating rating = DifficultyRating.Easy)
        {
            var issues = new CheckLowDiffOverlaps().Run(Context(objects, rating)).ToList();
            Assert.That(issues.Count, Is.LessThanOrEqualTo(1));
            return issues.SingleOrDefault()?.Template.GetType();
        }

        // ---- not overlapping ----

        [Test]
        public void TestNotOverlapping_VeryClose_InTime_ShouldOverlap()
        {
            // !overlapping TRUE, not a slider, deltaTime < 150
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(100, apart) }), Is.EqualTo(typeof(CheckLowDiffOverlaps.IssueTemplateShouldOverlap)));
        }

        [Test]
        public void TestNotOverlapping_BetweenThresholds_ShouldProbablyOverlap()
        {
            // 150 <= deltaTime < 175
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(160, apart) }), Is.EqualTo(typeof(CheckLowDiffOverlaps.IssueTemplateShouldProbablyOverlap)));
        }

        [Test]
        public void TestNotOverlapping_AtProbableThreshold_NoIssue()
        {
            // deltaTime == 175 is not < 175
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(175, apart) }), Is.Null);
        }

        [Test]
        public void TestNotOverlapping_AtOverlapThreshold_IsOnlyProbable()
        {
            // deltaTime == 150 is not < 150 but is < 175
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(150, apart) }), Is.EqualTo(typeof(CheckLowDiffOverlaps.IssueTemplateShouldProbablyOverlap)));
        }

        [Test]
        public void TestNotOverlapping_AfterSlider_IsNotRequired()
        {
            // `!(hitObject is Slider)` FALSE: slider-end leniency
            var slider = Slider(0, 100, 100, 100);
            var next = Circle(slider.GetEndTime() + 100, 100 + 100 + apart, 100);

            Assert.That(issueType(new HitObject[] { slider, next }), Is.Null);
        }

        // ---- overlapping ----

        [Test]
        public void TestOverlapping_LongGap_ShouldNotOverlap()
        {
            // areOverlapping TRUE && deltaTime > 250
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(300, close) }), Is.EqualTo(typeof(CheckLowDiffOverlaps.IssueTemplateShouldNotOverlap)));
        }

        [Test]
        public void TestOverlapping_AtNoOverlapThreshold_NoIssue()
        {
            // deltaTime == 250 is not > 250
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(250, close) }), Is.Null);
        }

        [Test]
        public void TestOverlapping_ShortGap_NoIssue()
        {
            // areOverlapping TRUE && deltaTime > 250 FALSE; !overlapping FALSE
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(100, close) }), Is.Null);
        }

        [Test]
        public void TestOverlapping_AfterSliderWithLongGap_StillShouldNotOverlap()
        {
            // the slider exemption only concerns missing overlaps
            var slider = Slider(0, 100, 100, 100);
            var next = Circle(slider.GetEndTime() + 300, 100 + 100 + close, 100);

            Assert.That(issueType(new HitObject[] { slider, next }), Is.EqualTo(typeof(CheckLowDiffOverlaps.IssueTemplateShouldNotOverlap)));
        }

        // ---- overlap boundary ----

        [Test]
        public void TestOverlapDistance_IsStrictlyLessThanDiameterMinusLeniency()
        {
            // 118px apart: distanceSq < diameterSq FALSE => not overlapping => ShouldOverlap; 117px => overlapping => no issue
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(100, 100 + 118) }), Is.EqualTo(typeof(CheckLowDiffOverlaps.IssueTemplateShouldOverlap)));
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(100, 100 + 117) }), Is.Null);
        }

        // ---- filtering ----

        [Test]
        public void TestObjectsFarApartInTime_AreNotVisibleTogether()
        {
            // deltaTime >= TimeFadeIn + TimePreempt (1000) => continue
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(1000, apart) }), Is.Null);
        }

        [Test]
        public void TestObjectsJustWithinVisibilityWindow_AreChecked()
        {
            // deltaTime 999 < 1000 => the pair is still evaluated (overlapping with a long gap is flagged)
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(999, close) }), Is.EqualTo(typeof(CheckLowDiffOverlaps.IssueTemplateShouldNotOverlap)));
        }

        [Test]
        public void TestSpinnerAsFirstObject_IsSkipped()
        {
            Assert.That(issueType(new HitObject[] { Spinner(0, 10), Circle(100, apart) }), Is.Null);
        }

        [Test]
        public void TestSpinnerAsNextObject_IsSkipped()
        {
            Assert.That(issueType(new HitObject[] { Circle(0), Spinner(100, 10) }), Is.Null);
        }

        [Test]
        public void TestNonOsuObjectAsFirst_IsSkipped()
        {
            Assert.That(issueType(new HitObject[] { new PlainHitObject(), Circle(100, apart) }), Is.Null);
        }

        [Test]
        public void TestNonOsuObjectAsNext_IsSkipped()
        {
            Assert.That(issueType(new HitObject[] { Circle(0), new PlainHitObject { StartTime = 100 } }), Is.Null);
        }

        [Test]
        public void TestHigherDifficulties_AreSkipped()
        {
            // InterpretedDifficulty > Easy TRUE
            Assert.That(issueType(new HitObject[] { Circle(0), Circle(100, apart) }, DifficultyRating.Normal), Is.Null);
        }

        [Test]
        public void TestSingleObject_NothingToCompare()
        {
            Assert.That(issueType(new HitObject[] { Circle(0) }), Is.Null);
        }

        [Test]
        public void TestStackedPositionsAreUsed()
        {
            // two circles 130px apart but the second is stacked towards the first, so they overlap
            var first = Circle(0, 100, 100);
            var second = Circle(100, 230, 100);
            second.StackHeight = 5; // offset = (-32, -32) => ~103px apart (< 118), whereas the raw positions are 130px apart

            Assert.That(issueType(new HitObject[] { first, second }), Is.Null);
        }
    }
}
