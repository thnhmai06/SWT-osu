// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Http;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.OnlinePlay.Lounge.Components;

namespace osu.Game.Tests.Online.API.Requests
{
    [TestFixture]
    public class SmallRequestsTest
    {
        // CF: GetMeRequest.Target | Ruleset?.ShortName | non-null
        [Test]
        public void TestGetMeWithRuleset() => Assert.That(RequestInspector.GetTarget(new GetMeRequest(new OsuRuleset().RulesetInfo)), Is.EqualTo("me/osu"));

        // CF: GetMeRequest.Target | Ruleset?.ShortName | null
        [Test]
        public void TestGetMeWithoutRuleset() => Assert.That(RequestInspector.GetTarget(new GetMeRequest()), Is.EqualTo("me/"));

        // CF: GetBeatmapSetRequest.Target | Type == SetId | TRUE
        [Test]
        public void TestBeatmapSetBySetId() => Assert.That(RequestInspector.GetTarget(new GetBeatmapSetRequest(5)), Is.EqualTo("beatmapsets/5"));

        // CF: GetBeatmapSetRequest.Target | Type == SetId | FALSE
        [Test]
        public void TestBeatmapSetByBeatmapId() => Assert.That(RequestInspector.GetTarget(new GetBeatmapSetRequest(5, BeatmapSetLookupType.BeatmapId)), Is.EqualTo("beatmapsets/lookup?beatmap_id=5"));

        // CF: GetBeatmapSetRequest.PostProcess | Response != null | TRUE
        // CF: GetBeatmapSetRequest.PostProcess | foreach beatmap | loop n
        [Test]
        public void TestPostProcessLinksBeatmapsToSet()
        {
            var request = new GetBeatmapSetRequest(1);
            var set = new APIBeatmapSet { Beatmaps = new[] { new APIBeatmap(), new APIBeatmap() } };

            RequestInspector.Succeed(request, set);
            typeof(GetBeatmapSetRequest).GetMethod("PostProcess", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(request, null);

            Assert.That(set.Beatmaps[0].BeatmapSet, Is.SameAs(set));
            Assert.That(set.Beatmaps[1].BeatmapSet, Is.SameAs(set));
        }

        // CF: GetBeatmapSetRequest.PostProcess | Response != null | FALSE
        // CF: GetBeatmapSetRequest.PostProcess | foreach beatmap | loop 0
        [Test]
        public void TestPostProcessWithoutResponseDoesNothing()
        {
            var request = RequestInspector.Attach(new GetBeatmapSetRequest(1));

            Assert.DoesNotThrow(() => typeof(GetBeatmapSetRequest).GetMethod("PostProcess", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(request, null));
        }

        // CF: CommentPostRequest.CreateWebRequest | ParentCommentId.HasValue | FALSE
        [Test]
        public void TestCommentPostWithoutParent()
        {
            var request = new CommentPostRequest(CommentableType.NewsPost, 3, "hello");

            Assert.That(RequestInspector.GetMethod(request), Is.EqualTo(HttpMethod.Post));
            Assert.That(RequestInspector.GetTarget(request), Is.EqualTo("comments"));
            Assert.That(RequestInspector.Values(request, "comment[commentable_type]"), Is.EqualTo(new[] { "newspost" }));
            Assert.That(RequestInspector.Values(request, "comment[commentable_id]"), Is.EqualTo(new[] { "3" }));
            Assert.That(RequestInspector.Values(request, "comment[message]"), Is.EqualTo(new[] { "hello" }));
            Assert.That(RequestInspector.Values(request, "comment[parent_id]"), Is.Empty);
        }

        // CF: CommentPostRequest.CreateWebRequest | ParentCommentId.HasValue | TRUE
        [Test]
        public void TestCommentPostWithParent()
            => Assert.That(RequestInspector.Values(new CommentPostRequest(CommentableType.Build, 3, "m", 9), "comment[parent_id]"), Is.EqualTo(new[] { "9" }));

        // CF: VerifySessionRequest.CreateWebRequest | verification key added | no throw
        [Test]
        public void TestVerifySession()
        {
            var request = new VerifySessionRequest("key");

            Assert.That(RequestInspector.GetMethod(request), Is.EqualTo(HttpMethod.Post));
            Assert.That(RequestInspector.GetTarget(request), Is.EqualTo("session/verify"));
            Assert.That(RequestInspector.Values(request, "verification_key"), Is.EqualTo(new[] { "key" }));
            Assert.That(request.RequiredVerificationMethod, Is.Null);
        }

        private static LoungeFilterCriteria criteria(RoomModeFilter mode = RoomModeFilter.Open, RoomStatusFilter? status = null, string category = "") => new LoungeFilterCriteria { Mode = mode, Status = status, Category = category };

        // CF: GetRoomsRequest.CreateWebRequest | mode != Open | FALSE
        // CF: GetRoomsRequest.CreateWebRequest | status != null | FALSE
        // CF: GetRoomsRequest.CreateWebRequest | !string.IsNullOrEmpty(category) | FALSE
        [Test]
        public void TestGetRoomsDefaults()
        {
            var request = new GetRoomsRequest(criteria());

            Assert.That(RequestInspector.GetParameters(request), Is.Empty);
            Assert.That(RequestInspector.GetTarget(request), Is.EqualTo("rooms"));
        }

        // CF: GetRoomsRequest.CreateWebRequest | mode != Open | TRUE
        // CF: GetRoomsRequest.CreateWebRequest | status != null | TRUE
        // CF: GetRoomsRequest.CreateWebRequest | !string.IsNullOrEmpty(category) | TRUE
        [Test]
        public void TestGetRoomsAllFilters()
        {
            var request = new GetRoomsRequest(criteria(RoomModeFilter.Participated, RoomStatusFilter.Playing, "spotlight"));

            Assert.That(RequestInspector.Values(request, "mode"), Is.EqualTo(new[] { "participated" }));
            Assert.That(RequestInspector.Values(request, "status"), Is.EqualTo(new[] { "playing" }));
            Assert.That(RequestInspector.Values(request, "category"), Is.EqualTo(new[] { "spotlight" }));
        }

        // CF: JoinRoomRequest.CreateWebRequest | !string.IsNullOrEmpty(Password) | FALSE
        [Test]
        public void TestJoinRoomWithoutPassword()
        {
            var request = new JoinRoomRequest(new Room(), null);

            Assert.That(RequestInspector.GetMethod(request), Is.EqualTo(HttpMethod.Put));
            Assert.That(RequestInspector.Values(request, "password"), Is.Empty);
        }

        // CF: JoinRoomRequest.CreateWebRequest | !string.IsNullOrEmpty(Password) | TRUE
        [Test]
        public void TestJoinRoomWithPassword()
            => Assert.That(RequestInspector.Values(new JoinRoomRequest(new Room(), "pw"), "password"), Is.EqualTo(new[] { "pw" }));

        // CF: JoinRoomRequest.ctor | Success copies response into room | no throw
        [Test]
        public void TestJoinRoomSuccessCopiesBackToRoom()
        {
            var room = new Room { Name = "old" };
            var request = new JoinRoomRequest(room, null);

            RequestInspector.Succeed(request, new Room { RoomID = 5, Name = "new" });

            Assert.That(room.Name, Is.EqualTo("new"));
            Assert.That(room.RoomID, Is.EqualTo(5));
        }

        // CF: IndexPlaylistScoresRequest.CreateWebRequest | Cursor != null | FALSE
        [Test]
        public void TestIndexScoresWithoutCursor()
        {
            var request = new IndexPlaylistScoresRequest(2, 3);

            Assert.That(RequestInspector.GetParameters(request), Is.Empty);
            Assert.That(RequestInspector.GetTarget(request), Is.EqualTo("rooms/2/playlist/3/scores"));
            Assert.That(request.Cursor, Is.Null);
        }

        // CF: IndexPlaylistScoresRequest.CreateWebRequest | Cursor != null | TRUE
        // CF: IndexPlaylistScoresRequest.CreateWebRequest | foreach IndexParams.Properties | loop n
        [Test]
        public void TestIndexScoresWithCursorAndParams()
        {
            var cursor = new Cursor();
            cursor.Properties["total_score"] = new JValue(99);
            var parameters = new IndexScoresParams();
            parameters.Properties["sort"] = new JValue("score_desc");

            var request = new IndexPlaylistScoresRequest(2, 3, cursor, parameters);

            Assert.That(RequestInspector.Values(request, "cursor[total_score]"), Is.EqualTo(new[] { "99" }));
            Assert.That(RequestInspector.Values(request, "sort"), Is.EqualTo(new[] { "score_desc" }));
        }

        // CF: PatchBeatmapPackageRequest.Uri | BeatmapSubmissionServiceUrl == null | FALSE
        [Test]
        public void TestPatchUri() => Assert.That(RequestInspector.GetUri(new PatchBeatmapPackageRequest(42)), Is.EqualTo("http://sub.test/beatmapsets/42"));

        // CF: PatchBeatmapPackageRequest.Uri | BeatmapSubmissionServiceUrl == null | TRUE
        // CF: PatchBeatmapPackageRequest.Uri | submission unsupported | throws NotSupportedException
        [Test]
        public void TestPatchUriWithoutServiceThrows()
        {
            var api = new Moq.Mock<osu.Game.Online.API.IAPIProvider>();
            api.SetupGet(a => a.Endpoints).Returns(new osu.Game.Online.EndpointConfiguration());
            var request = new PatchBeatmapPackageRequest(1);
            request.AttachAPI(api.Object);

            var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => typeof(osu.Game.Online.API.APIRequest).GetProperty("Uri", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(request));
            Assert.That(ex!.InnerException, Is.TypeOf<System.NotSupportedException>());
        }

        // CF: PatchBeatmapPackageRequest.Target | always | throws NotSupportedException
        [Test]
        public void TestPatchTargetThrows()
        {
            var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => RequestInspector.GetTarget(new PatchBeatmapPackageRequest(1)));
            Assert.That(ex!.InnerException, Is.TypeOf<System.NotSupportedException>());
        }

        // CF: PatchBeatmapPackageRequest.CreateWebRequest | foreach FilesChanged | loop 0
        // CF: PatchBeatmapPackageRequest.CreateWebRequest | foreach FilesDeleted | loop 0
        [Test]
        public void TestPatchWithoutFiles()
        {
            var request = new PatchBeatmapPackageRequest(1);

            Assert.That(RequestInspector.GetMethod(request), Is.EqualTo(HttpMethod.Patch));
            Assert.That(RequestInspector.GetParameters(request), Is.Empty);
        }

        // CF: PatchBeatmapPackageRequest.CreateWebRequest | foreach FilesChanged | loop n
        // CF: PatchBeatmapPackageRequest.CreateWebRequest | foreach FilesDeleted | loop n
        [Test]
        public void TestPatchWithFiles()
        {
            var request = new PatchBeatmapPackageRequest(1);
            request.FilesChanged["a.osu"] = new byte[] { 1 };
            request.FilesChanged["b.osu"] = new byte[] { 2 };
            request.FilesDeleted.Add("c.osu");

            Assert.That(RequestInspector.Values(request, "filesDeleted"), Is.EqualTo(new[] { "c.osu" }));
        }
    }
}
