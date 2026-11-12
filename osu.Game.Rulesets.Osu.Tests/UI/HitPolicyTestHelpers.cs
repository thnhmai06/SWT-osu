// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Osu.Tests.UI
{
    /// <summary>
    /// Hand-written fakes shared by the hit policy tests. None of them require a game host.
    /// </summary>
    internal static class HitPolicyTestHelpers
    {
        public static DrawableHitCircle Circle(double startTime, bool judged = false, int stackHeight = 0)
        {
            var drawable = new DrawableHitCircle(new HitCircle { StartTime = startTime, StackHeight = stackHeight });

            if (judged)
                drawable.Result.Type = HitResult.Great;

            return drawable;
        }

        public static NonBlockingDrawable NonBlocking(double startTime, bool judged = false)
        {
            var drawable = new NonBlockingDrawable(new HitCircle { StartTime = startTime });

            if (judged)
                drawable.Result.Type = HitResult.Great;

            return drawable;
        }

        public static FakeContainer Container(params DrawableHitObject[] alive) => new FakeContainer(alive);

        public sealed class FakeContainer : IHitObjectContainer
        {
            private readonly List<DrawableHitObject> alive;

            public FakeContainer(IEnumerable<DrawableHitObject> alive)
            {
                this.alive = alive.ToList();
            }

            public IEnumerable<DrawableHitObject> Objects => alive;

            public IEnumerable<DrawableHitObject> AliveObjects => alive;
        }

        /// <summary>
        /// A <see cref="DrawableOsuHitObject"/> which is not a <see cref="DrawableHitCircle"/> (so it can never block future hits).
        /// </summary>
        public sealed partial class NonBlockingDrawable : DrawableOsuHitObject
        {
            public NonBlockingDrawable(OsuHitObject hitObject)
                : base(hitObject)
            {
            }
        }
    }
}
