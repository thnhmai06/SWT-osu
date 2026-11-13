// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Tests.Online.API.Requests.Responses
{
    [TestFixture]
    public class APIBeatmapTest
    {
        private static APIBeatmap create(params (long id, string name)[] related) => new APIBeatmap
        {
            BeatmapSet = new APIBeatmapSet { RelatedTags = related.Select(r => new APITag { Id = r.id, Name = r.name }).ToArray() },
        };

        // CF: APIBeatmap.GetTopUserTags | TopTags == null | TRUE
        [Test]
        public void TestNoTopTags() => Assert.That(create((1, "a")).GetTopUserTags(), Is.Empty);

        // CF: APIBeatmap.GetTopUserTags | TopTags.Length == 0 | TRUE
        [Test]
        public void TestEmptyTopTags()
        {
            var beatmap = create((1, "a"));
            beatmap.TopTags = Array.Empty<APIBeatmapTag>();

            Assert.That(beatmap.GetTopUserTags(), Is.Empty);
        }

        // CF: APIBeatmap.GetTopUserTags | BeatmapSet?.RelatedTags == null | TRUE
        [Test]
        public void TestNoRelatedTags()
        {
            var beatmap = new APIBeatmap { TopTags = new[] { new APIBeatmapTag { TagId = 1, VoteCount = 10 } } };

            Assert.That(beatmap.GetTopUserTags(), Is.Empty);
            beatmap.BeatmapSet = new APIBeatmapSet();
            Assert.That(beatmap.GetTopUserTags(), Is.Empty);
        }

        // CF: APIBeatmap.GetTopUserTags | TopTags == null | FALSE
        // CF: APIBeatmap.GetTopUserTags | TopTags.Length == 0 | FALSE
        // CF: APIBeatmap.GetTopUserTags | BeatmapSet?.RelatedTags == null | FALSE
        // CF: APIBeatmap.GetTopUserTags | t.relatedTag != null | TRUE
        // CF: APIBeatmap.GetTopUserTags | !confirmedOnly | FALSE
        // CF: APIBeatmap.GetTopUserTags | t.topTag.VoteCount >= MINIMUM_USER_TAG_VOTES_FOR_DISPLAY | TRUE
        [Test]
        public void TestConfirmedTagsSortedByVotesThenName()
        {
            var beatmap = create((1, "b"), (2, "a"), (3, "c"));
            beatmap.TopTags = new[]
            {
                new APIBeatmapTag { TagId = 1, VoteCount = 7 },
                new APIBeatmapTag { TagId = 2, VoteCount = 7 },
                new APIBeatmapTag { TagId = 3, VoteCount = 9 },
            };

            var tags = beatmap.GetTopUserTags();

            Assert.That(tags.Select(t => t.Tag.Name), Is.EqualTo(new[] { "c", "a", "b" }));
            Assert.That(tags.Select(t => t.VoteCount), Is.EqualTo(new[] { 9, 7, 7 }));
        }

        // CF: APIBeatmap.GetTopUserTags | t.relatedTag != null | FALSE
        // CF: APIBeatmap.GetTopUserTags | t.topTag.VoteCount >= MINIMUM_USER_TAG_VOTES_FOR_DISPLAY | FALSE
        [Test]
        public void TestUnknownAndUnconfirmedTagsExcluded()
        {
            var beatmap = create((1, "a"), (2, "b"));
            beatmap.TopTags = new[]
            {
                new APIBeatmapTag { TagId = 1, VoteCount = APIBeatmap.MINIMUM_USER_TAG_VOTES_FOR_DISPLAY - 1 },
                new APIBeatmapTag { TagId = 2, VoteCount = APIBeatmap.MINIMUM_USER_TAG_VOTES_FOR_DISPLAY },
                new APIBeatmapTag { TagId = 99, VoteCount = 100 },
            };

            Assert.That(beatmap.GetTopUserTags().Select(t => t.Tag.Name), Is.EqualTo(new[] { "b" }));
        }

        // CF: APIBeatmap.GetTopUserTags | !confirmedOnly | TRUE
        [Test]
        public void TestUnconfirmedTagsIncludedWhenRequested()
        {
            var beatmap = create((1, "a"));
            beatmap.TopTags = new[] { new APIBeatmapTag { TagId = 1, VoteCount = 1 } };

            Assert.That(beatmap.GetTopUserTags(false).Select(t => t.Tag.Name), Is.EqualTo(new[] { "a" }));
        }

        // CF: APIBeatmap.Metadata | (BeatmapSet as IBeatmapSetInfo)?.Metadata ?? new BeatmapMetadata() | non-null
        // CF: APIBeatmap.Metadata | (BeatmapSet as IBeatmapSetInfo)?.Metadata ?? new BeatmapMetadata() | null
        [Test]
        public void TestMetadataFallsBackWhenNoSet()
        {
            Assert.That(new APIBeatmap().Metadata.Title, Is.EqualTo(string.Empty));
            Assert.That(new APIBeatmap { BeatmapSet = new APIBeatmapSet { Title = "T" } }.Metadata.Title, Is.EqualTo("T"));
        }

        // CF: APIBeatmap.Difficulty | maps difficulty fields | no throw
        [Test]
        public void TestDifficulty()
        {
            var d = new APIBeatmap { DrainRate = 1, CircleSize = 2, ApproachRate = 3, OverallDifficulty = 4 }.Difficulty;

            Assert.That((d.DrainRate, d.CircleSize, d.ApproachRate, d.OverallDifficulty), Is.EqualTo((1f, 2f, 3f, 4f)));
        }

        // CF: APIBeatmap.ObjectCounts | EndTimeObjectCount and TotalObjectCount sums | no throw
        [Test]
        public void TestObjectCounts()
        {
            var b = new APIBeatmap { CircleCount = 5, SliderCount = 3, SpinnerCount = 2 };

            Assert.That(b.EndTimeObjectCount, Is.EqualTo(5));
            Assert.That(b.TotalObjectCount, Is.EqualTo(10));
        }

        // CF: APIBeatmap.Equals | other is APIBeatmap b | TRUE
        [Test]
        public void TestEqualsByOnlineId() => Assert.That(new APIBeatmap { OnlineID = 1 }.Equals(new APIBeatmap { OnlineID = 1 }), Is.True);

        // CF: APIBeatmap.Equals | other is APIBeatmap b | FALSE
        [Test]
        public void TestNotEqualsDifferentIdOrType()
        {
            Assert.That(new APIBeatmap { OnlineID = 1 }.Equals(new APIBeatmap { OnlineID = 2 }), Is.False);
            Assert.That(new APIBeatmap { OnlineID = 1 }.Equals(new BeatmapInfo { OnlineID = 1 }), Is.False);
        }

        // CF: APIRuleset.ShortName | switch OnlineID | case 0
        // CF: APIRuleset.ShortName | switch OnlineID | case 1
        // CF: APIRuleset.ShortName | switch OnlineID | case 2
        // CF: APIRuleset.ShortName | switch OnlineID | case 3
        [Test]
        public void TestRulesetShortNames()
        {
            Assert.That(new APIBeatmap { RulesetID = 0 }.Ruleset.ShortName, Is.EqualTo("osu"));
            Assert.That(new APIBeatmap { RulesetID = 1 }.Ruleset.ShortName, Is.EqualTo("taiko"));
            Assert.That(new APIBeatmap { RulesetID = 2 }.Ruleset.ShortName, Is.EqualTo("fruits"));
            Assert.That(new APIBeatmap { RulesetID = 3 }.Ruleset.ShortName, Is.EqualTo("mania"));
        }

        // CF: APIRuleset.ShortName | switch OnlineID | default
        // CF: APIRuleset.ShortName | unknown ruleset | throws ArgumentOutOfRangeException
        [Test]
        public void TestRulesetShortNameUnknownThrows() => Assert.Throws<ArgumentOutOfRangeException>(() => _ = new APIBeatmap { RulesetID = 9 }.Ruleset.ShortName);

        // CF: APIRuleset.Equals | other is APIRuleset r | TRUE
        // CF: APIRuleset.Equals | other is APIRuleset r | FALSE
        [Test]
        public void TestRulesetEquals()
        {
            Assert.That(new APIBeatmap { RulesetID = 1 }.Ruleset.Equals(new APIBeatmap { RulesetID = 1 }.Ruleset), Is.True);
            Assert.That(new APIBeatmap { RulesetID = 1 }.Ruleset.Equals(new APIBeatmap { RulesetID = 2 }.Ruleset), Is.False);
            Assert.That(new APIBeatmap { RulesetID = 1 }.Ruleset.Equals(null), Is.False);
        }

        // CF: APIRuleset.CompareTo | other is APIRuleset ruleset | TRUE
        [Test]
        public void TestRulesetCompareTo()
            => Assert.That(new APIBeatmap { RulesetID = 1 }.Ruleset.CompareTo(new APIBeatmap { RulesetID = 2 }.Ruleset), Is.LessThan(0));

        // CF: APIRuleset.CompareTo | other is APIRuleset ruleset | FALSE
        // CF: APIRuleset.CompareTo | other of different type | throws ArgumentException
        [Test]
        public void TestRulesetCompareToOtherTypeThrows()
            => Assert.Throws<ArgumentException>(() => new APIBeatmap { RulesetID = 1 }.Ruleset.CompareTo(null));

        // CF: APIRuleset.GetHashCode | returns OnlineID | no throw
        [Test]
        public void TestRulesetHashCode() => Assert.That(new APIBeatmap { RulesetID = 3 }.Ruleset.GetHashCode(), Is.EqualTo(3));
    }
}
