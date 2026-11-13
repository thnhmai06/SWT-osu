// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables.Connections;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Objects.Drawables.Connections
{
    [TestFixture]
    public class FollowPointLifetimeEntryTest
    {
        private static HitCircle circle(double time, float x, bool newCombo = false)
        {
            var c = new HitCircle { StartTime = time, Position = new Vector2(x, 100), NewCombo = newCombo };
            c.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return c;
        }

        [Test]
        public void TestInitialLifetimeStartsAtObjectStartTime()
        {
            var entry = new FollowPointLifetimeEntry(circle(1000, 0));

            Assert.That(entry.LifetimeStart, Is.EqualTo(1000));
            Assert.That(entry.End, Is.Null);
        }

        // CF: FollowPointLifetimeEntry.refreshLifetimes | End == null | TRUE
        // CF: FollowPointLifetimeEntry.refreshLifetimes | End.NewCombo | TRUE
        [Test]
        public void TestNewComboEndHidesConnection()
        {
            var start = circle(1000, 0);
            var entry = new FollowPointLifetimeEntry(start) { End = circle(2000, 200, newCombo: true) };

            Assert.That(entry.LifetimeEnd, Is.EqualTo(entry.LifetimeStart));
        }

        // CF: FollowPointLifetimeEntry.refreshLifetimes | Start is Spinner | TRUE
        // CF: FollowPointLifetimeEntry.refreshLifetimes | End is Spinner | TRUE
        [Test]
        public void TestSpinnersHideConnection()
        {
            var spinner = new Spinner { StartTime = 1000, Duration = 500 };
            spinner.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            var fromSpinner = new FollowPointLifetimeEntry(spinner) { End = circle(3000, 200) };
            var toSpinner = new FollowPointLifetimeEntry(circle(1000, 0)) { End = spinner };

            Assert.That(fromSpinner.LifetimeEnd, Is.EqualTo(fromSpinner.LifetimeStart));
            Assert.That(toSpinner.LifetimeEnd, Is.EqualTo(toSpinner.LifetimeStart));
        }

        // CF: FollowPointLifetimeEntry.refreshLifetimes | End == null | FALSE
        // CF: FollowPointLifetimeEntry.refreshLifetimes | End.NewCombo | FALSE
        // CF: FollowPointLifetimeEntry.refreshLifetimes | Start is Spinner | FALSE
        // CF: FollowPointLifetimeEntry.refreshLifetimes | End is Spinner | FALSE
        [Test]
        public void TestRegularConnectionIsVisibleBeforeStartAndRaisesInvalidated()
        {
            int invalidated = 0;
            var entry = new FollowPointLifetimeEntry(circle(1000, 0));
            entry.Invalidated += () => invalidated++;

            entry.End = circle(2000, 300);

            Assert.That(invalidated, Is.EqualTo(1));
            Assert.That(entry.LifetimeStart, Is.LessThan(1000));
            Assert.That(entry.LifetimeEnd, Is.EqualTo(double.MaxValue));
        }

        // CF: FollowPointLifetimeEntry.bindEvents | End == null | FALSE
        [Test]
        public void TestPositionChangeRefreshesLifetime()
        {
            int invalidated = 0;
            var start = circle(1000, 0);
            var end = circle(2000, 300);
            var entry = new FollowPointLifetimeEntry(start) { End = end };
            entry.Invalidated += () => invalidated++;

            end.Position = new Vector2(100, 100);

            Assert.That(invalidated, Is.EqualTo(1));
        }

        // CF: FollowPointLifetimeEntry.UnbindEvents | wasBound | TRUE
        [Test]
        public void TestUnbindStopsRefreshing()
        {
            int invalidated = 0;
            var end = circle(2000, 300);
            var entry = new FollowPointLifetimeEntry(circle(1000, 0)) { End = end };
            entry.Invalidated += () => invalidated++;

            entry.UnbindEvents();
            end.Position = new Vector2(100, 100);

            Assert.That(invalidated, Is.EqualTo(0));
        }

        // CF: FollowPointLifetimeEntry.UnbindEvents | wasBound | FALSE
        [Test]
        public void TestUnbindWithoutBindingIsNoOp()
        {
            Assert.DoesNotThrow(() => new FollowPointLifetimeEntry(circle(1000, 0)).UnbindEvents());
        }

        [Test]
        public void TestReplacingEndRebindsEvents()
        {
            int invalidated = 0;
            var oldEnd = circle(2000, 300);
            var newEnd = circle(2500, 400);
            var entry = new FollowPointLifetimeEntry(circle(1000, 0)) { End = oldEnd };
            entry.Invalidated += () => invalidated++;

            entry.End = newEnd;
            int afterReplace = invalidated;
            oldEnd.Position = new Vector2(10, 10);

            Assert.That(invalidated, Is.EqualTo(afterReplace));

            newEnd.Position = new Vector2(10, 10);
            Assert.That(invalidated, Is.EqualTo(afterReplace + 1));
        }
    }
}
