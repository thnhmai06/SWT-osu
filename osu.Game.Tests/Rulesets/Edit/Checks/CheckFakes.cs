// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Audio.Track;
using osu.Game.Beatmaps;
using osu.Game.Models;
using osu.Game.Rulesets.Edit;
using osu.Game.Storyboards;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    /// <summary>
    /// Working beatmap serving in-memory streams keyed by storage path, and an optional fixed-bitrate track.
    /// </summary>
    internal class StreamWorkingBeatmap : TestWorkingBeatmap
    {
        private readonly Dictionary<string, byte[]> streams;
        private readonly Track? track;

        public StreamWorkingBeatmap(IBeatmap beatmap, Storyboard? storyboard = null, Dictionary<string, byte[]>? streams = null, Track? track = null)
            : base(beatmap, storyboard)
        {
            this.streams = streams ?? new Dictionary<string, byte[]>();
            this.track = track;
        }

        public override Stream? GetStream(string storagePath) => storagePath != null && streams.TryGetValue(storagePath, out var data) ? new MemoryStream(data) : null;

        protected override Track? GetBeatmapTrack() => track;
    }

    internal class BitrateTrack : Track
    {
        private readonly int? bitrate;

        public BitrateTrack(double length, int? bitrate)
            : base("fake")
        {
            Length = length;
            this.bitrate = bitrate;
        }

        public override int? Bitrate => bitrate;

        public override double CurrentTime => 0;
        public override bool IsRunning => false;
        public override bool Seek(double seek) => true;
        public override Task<bool> SeekAsync(double seek) => Task.FromResult(true);
        public override void Start() { }
        public override Task StartAsync() => Task.CompletedTask;
        public override void Stop() { }
        public override Task StopAsync() => Task.CompletedTask;
    }

    internal static class CheckFakes
    {
        /// <summary>
        /// Attaches a set with the given files (filename -> content) to the beatmap.
        /// Returns the storage path map to feed <see cref="StreamWorkingBeatmap"/>.
        /// </summary>
        public static Dictionary<string, byte[]> AttachFiles(IBeatmap beatmap, IDictionary<string, byte[]?> files)
        {
            var set = new BeatmapSetInfo();
            var streams = new Dictionary<string, byte[]>();

            int i = 0;

            foreach (var (name, content) in files)
            {
                // fixed-width fake hash, long enough for GetStoragePath's Remove(2)
                var file = new RealmFile { Hash = $"{i++:x2}abcdef0123456789" };
                set.Files.Add(new RealmNamedFileUsage(file, name));
                if (content != null) streams[Path.Combine(file.Hash.Remove(1), file.Hash.Remove(2), file.Hash)] = content;
            }

            beatmap.BeatmapInfo.BeatmapSet = set;
            return streams;
        }

        public static BeatmapVerifierContext Context(IBeatmap beatmap, IDictionary<string, byte[]?> files, Storyboard? storyboard = null, Track? track = null)
        {
            var streams = AttachFiles(beatmap, files);
            var working = new StreamWorkingBeatmap(beatmap, storyboard, streams, track);
            if (track != null) working.LoadTrack();
            return new BeatmapVerifierContext(beatmap, working);
        }

        public static BeatmapVerifierContext.VerifiedBeatmap Verified(IBeatmap beatmap, Storyboard? storyboard = null)
            => new BeatmapVerifierContext.VerifiedBeatmap(new TestWorkingBeatmap(beatmap, storyboard), beatmap);
    }
}
