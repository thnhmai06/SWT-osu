// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Tests.Rulesets.Objects
{
    [TestFixture]
    public class HitObjectLifetimeEntryTest
    {
        private static JudgementResult result(HitObject obj, HitResult type) => new JudgementResult(obj, new Judgement()) { Type = type };

        [Test]
        public void TestInitialLifetimePrecedesStartTime()
        {
            var entry = new HitObjectLifetimeEntry(new HitCircle { StartTime = 20000 });

            Assert.That(entry.LifetimeStart, Is.EqualTo(10000));
        }

        [Test]
        public void TestLifetimeFollowsStartTimeChanges()
        {
            var circle = new HitCircle { StartTime = 20000 };
            var entry = new HitObjectLifetimeEntry(circle);

            circle.StartTime = 30000;

            Assert.That(entry.LifetimeStart, Is.EqualTo(20000));
        }

        // CF: HitObjectLifetimeEntry.Judged | Result?.HasResult ?? false | null
        [Test]
        public void TestNotJudgedWithoutResult()
        {
            var entry = new HitObjectLifetimeEntry(new HitCircle());

            Assert.That(entry.Judged, Is.False);
            Assert.That(entry.AllJudged, Is.False);
        }

        // CF: HitObjectLifetimeEntry.Judged | Result?.HasResult ?? false | FALSE
        [Test]
        public void TestNotJudgedWithResultTypeNone()
        {
            var circle = new HitCircle();
            var entry = new HitObjectLifetimeEntry(circle) { Result = result(circle, HitResult.None) };

            Assert.That(entry.Judged, Is.False);
        }

        // CF: HitObjectLifetimeEntry.AllJudged | !Judged | FALSE
        // CF: HitObjectLifetimeEntry.AllJudged | foreach entry in NestedEntries | loop 0
        [Test]
        public void TestAllJudgedWithoutNestedEntries()
        {
            var circle = new HitCircle();
            var entry = new HitObjectLifetimeEntry(circle) { Result = result(circle, HitResult.Great) };

            Assert.That(entry.Judged, Is.True);
            Assert.That(entry.AllJudged, Is.True);
        }

        // CF: HitObjectLifetimeEntry.AllJudged | !entry.AllJudged | TRUE
        [Test]
        public void TestNotAllJudgedWhileNestedEntryPending()
        {
            var parent = new HitCircle();
            var child = new HitCircle();
            var childEntry = new HitObjectLifetimeEntry(child);
            var entry = new HitObjectLifetimeEntry(parent) { Result = result(parent, HitResult.Great), NestedEntries = { childEntry } };

            Assert.That(entry.AllJudged, Is.False);
        }

        // CF: HitObjectLifetimeEntry.AllJudged | !entry.AllJudged | FALSE
        // CF: HitObjectLifetimeEntry.AllJudged | foreach entry in NestedEntries | loop n
        [Test]
        public void TestAllJudgedWhenNestedEntriesJudged()
        {
            var parent = new HitCircle();
            var child = new HitCircle();
            var childEntry = new HitObjectLifetimeEntry(child) { Result = result(child, HitResult.Great) };
            var entry = new HitObjectLifetimeEntry(parent) { Result = result(parent, HitResult.Great), NestedEntries = { childEntry } };

            Assert.That(entry.AllJudged, Is.True);
        }

        // CF: HitObjectLifetimeEntry.KeepAlive | keepAlive == value | TRUE
        // CF: HitObjectLifetimeEntry.KeepAlive | keepAlive | TRUE
        [Test]
        public void TestKeepAliveExtendsLifetimeToInfinity()
        {
            var entry = new HitObjectLifetimeEntry(new HitCircle { StartTime = 20000 });

            entry.KeepAlive = true;
            entry.KeepAlive = true;

            Assert.That(entry.LifetimeStart, Is.EqualTo(double.MinValue));
            Assert.That(entry.LifetimeEnd, Is.EqualTo(double.MaxValue));
        }

        // CF: HitObjectLifetimeEntry.KeepAlive | keepAlive | FALSE
        [Test]
        public void TestKeepAliveReleaseRestoresRealLifetime()
        {
            var circle = new HitCircle { StartTime = 20000 };
            var entry = new HitObjectLifetimeEntry(circle);
            entry.KeepAlive = true;

            circle.StartTime = 25000; // real lifetime changes while kept alive, but is not applied yet
            Assert.That(entry.LifetimeStart, Is.EqualTo(double.MinValue));

            entry.KeepAlive = false;

            Assert.That(entry.LifetimeStart, Is.EqualTo(15000));
            Assert.That(entry.LifetimeEnd, Is.EqualTo(double.MaxValue));
        }

        [Test]
        public void TestRevertResultEventRaised()
        {
            var entry = new HitObjectLifetimeEntry(new HitCircle());
            int raised = 0;
            entry.RevertResult += () => raised++;

            entry.OnRevertResult();

            Assert.That(raised, Is.EqualTo(1));
        }
    }
}
