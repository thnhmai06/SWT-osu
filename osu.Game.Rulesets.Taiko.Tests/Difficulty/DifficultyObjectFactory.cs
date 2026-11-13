// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour.Data;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Tests.Difficulty
{
    /// <summary>
    /// Builds linked <see cref="TaikoDifficultyHitObject"/>s headlessly.
    /// </summary>
    public static class DifficultyObjectFactory
    {
        /// <param name="notes">(delta time from the previous note, hit type) pairs.</param>
        public static List<TaikoDifficultyHitObject> Create(params (double delta, HitType type)[] notes)
        {
            var all = new List<DifficultyHitObject>();
            var centre = new List<TaikoDifficultyHitObject>();
            var rim = new List<TaikoDifficultyHitObject>();
            var noteList = new List<TaikoDifficultyHitObject>();
            var result = new List<TaikoDifficultyHitObject>();

            double time = 1000;
            var last = hit(time, HitType.Centre);

            for (int i = 0; i < notes.Length; i++)
            {
                time += notes[i].delta;
                var current = hit(time, notes[i].type);
                var obj = new TaikoDifficultyHitObject(current, last, 1, all, centre, rim, noteList, i, new ControlPointInfo(), 1);
                all.Add(obj);
                result.Add(obj);
                last = current;
            }

            return result;
        }

        /// <summary>
        /// Builds a pattern of mono streaks; each streak is (length, type).
        /// </summary>
        public static AlternatingMonoPattern Pattern(params (int length, HitType type)[] streaks)
        {
            var result = new AlternatingMonoPattern();

            foreach (var (length, type) in streaks)
            {
                var streak = new MonoStreak();
                for (int i = 0; i < length; i++)
                    streak.HitObjects.AddRange(Create((100, type)));
                result.MonoStreaks.Add(streak);
            }

            return result;
        }

        private static Hit hit(double time, HitType type)
        {
            var hit = new Hit { StartTime = time, Type = type };
            hit.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return hit;
        }
    }
}
