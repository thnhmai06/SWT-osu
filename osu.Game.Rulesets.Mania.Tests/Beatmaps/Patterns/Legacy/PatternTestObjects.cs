// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Mania.Beatmaps.Patterns;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Mania.Tests.Beatmaps.Patterns.Legacy
{
    internal class FakeCircle : HitObject, IHasPosition
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2 Position
        {
            get => new Vector2(X, Y);
            set { X = value.X; Y = value.Y; }
        }

        public FakeCircle With(params string[] sampleNames)
        {
            foreach (string n in sampleNames)
                Samples.Add(new HitSampleInfo(n));
            return this;
        }
    }

    internal class FakeDurationObject : HitObject, IHasDuration, IHasXPosition, IHasLegacyHitObjectType
    {
        public double Duration { get; set; }
        public double EndTime => StartTime + Duration;
        public float X { get; set; }
        public LegacyHitObjectType LegacyType { get; init; } = LegacyHitObjectType.Spinner;
    }

    internal class FakeNoPositionDuration : HitObject, IHasDuration
    {
        public double Duration { get; set; }
        public double EndTime => StartTime + Duration;
    }

    internal class FakeSlider : FakeCircle, IHasPathWithRepeats
    {
        public double Duration { get; set; }
        public double EndTime => StartTime + Duration;
        public double Distance => Path.Distance;
        public SliderPath Path { get; }
        public int RepeatCount { get; set; }
        public IList<IList<HitSampleInfo>> NodeSamples { get; }

        public FakeSlider(double distance, int spans)
        {
            Path = new SliderPath(PathType.LINEAR, [Vector2.Zero, new Vector2((float)distance, 0)], distance);
            RepeatCount = spans - 1;
            NodeSamples = Enumerable.Range(0, spans + 1).Select(_ => (IList<HitSampleInfo>)new List<HitSampleInfo>()).ToList();
        }
    }

    internal static class PatternTestHelper
    {
        /// <summary>A beatmap whose conversion difficulty is well below 2.</summary>
        public static IBeatmap LowDifficulty() => create(0, 0, 2, 100_000);

        /// <summary>A beatmap whose conversion difficulty is capped at 12 (above 6.5).</summary>
        public static IBeatmap HighDifficulty() => create(10, 10, 400, 20_000);

        private static IBeatmap create(float dr, float ar, int objectCount, double lastTime)
        {
            var b = new Beatmap<HitObject> { Difficulty = new BeatmapDifficulty { DrainRate = dr, ApproachRate = ar, SliderMultiplier = 1 } };
            for (int i = 0; i < objectCount; i++)
                b.HitObjects.Add(new Note { StartTime = lastTime * i / (objectCount - 1) });
            return b;
        }

        public static Pattern PatternOf(params int[] columns)
        {
            var p = new Pattern();
            foreach (int c in columns)
                p.Add(new Note { Column = c });
            return p;
        }

        public static int[] Columns(Pattern p) => p.HitObjects.Select(h => h.Column).ToArray();
    }
}
