// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;

namespace osu.Game.Tests.Online.Chat
{
    [TestFixture]
    public class MessageFormatterTest
    {
        private string originalRoot = null!;

        [SetUp]
        public void SetUp()
        {
            originalRoot = MessageFormatter.WebsiteRootUrl;
            MessageFormatter.WebsiteRootUrl = "osu.ppy.sh";
        }

        [TearDown]
        public void TearDown() => MessageFormatter.WebsiteRootUrl = originalRoot;

        private static void assertDetails(string url, LinkAction action, object? argument)
        {
            var details = MessageFormatter.GetLinkDetails(url);
            Assert.That(details.Action, Is.EqualTo(action));
            if (argument != null)
                Assert.That(details.Argument, Is.EqualTo(argument));
        }

        // CF: MessageFormatter.WebsiteRootUrl | set value is trimmed and reduced to domain | no throw
        [Test]
        public void TestWebsiteRootUrlSetterKeepsOnlyDomain()
        {
            MessageFormatter.WebsiteRootUrl = "https://example.com/";
            Assert.That(MessageFormatter.WebsiteRootUrl, Is.EqualTo("example.com"));
        }

        // CF: MessageFormatter.GetLinkDetails | switch scheme | case http
        // CF: MessageFormatter.GetLinkDetails | args.Length > 3 | FALSE
        [Test]
        public void TestHttpTooShortIsExternal() => assertDetails("https://osu.ppy.sh/b", LinkAction.External, "https://osu.ppy.sh/b");

        // CF: MessageFormatter.GetLinkDetails | switch scheme | case https
        // CF: MessageFormatter.GetLinkDetails | args.Length > 3 | TRUE
        // CF: MessageFormatter.GetLinkDetails | args[1].EndsWith(WebsiteRootUrl) | FALSE
        [Test]
        public void TestForeignHostIsExternal() => assertDetails("https://example.com/b/1234", LinkAction.External, "https://example.com/b/1234");

        // CF: MessageFormatter.GetLinkDetails | args[1].EndsWith(WebsiteRootUrl) | TRUE
        // CF: MessageFormatter.GetLinkDetails | switch args[2] | case b
        // CF: MessageFormatter.GetLinkDetails | int.TryParse(trimmed) beatmap | TRUE
        [Test]
        public void TestBeatmapLink() => assertDetails("https://osu.ppy.sh/b/1234?m=0", LinkAction.OpenBeatmap, "1234");

        // CF: MessageFormatter.GetLinkDetails | switch args[2] | case beatmaps
        // CF: MessageFormatter.GetLinkDetails | int.TryParse(trimmed) beatmap | FALSE
        [Test]
        public void TestBeatmapLinkNonNumericIsExternal() => assertDetails("https://osu.ppy.sh/beatmaps/abc", LinkAction.External, "https://osu.ppy.sh/beatmaps/abc");

        // CF: MessageFormatter.GetLinkDetails | switch args[2] | case s
        // CF: MessageFormatter.GetLinkDetails | mainArg == discussions | FALSE
        // CF: MessageFormatter.GetLinkDetails | args.Length > 4 | FALSE
        // CF: MessageFormatter.GetLinkDetails | int.TryParse(trimmed) set | TRUE
        [Test]
        public void TestBeatmapSetLink() => assertDetails("https://osu.ppy.sh/s/55#osu", LinkAction.OpenBeatmapSet, "55");

        // CF: MessageFormatter.GetLinkDetails | switch args[2] | case beatmapsets
        // CF: MessageFormatter.GetLinkDetails | args.Length > 4 | TRUE
        // CF: MessageFormatter.GetLinkDetails | int.TryParse(args[4]) | TRUE
        [Test]
        public void TestBeatmapSetLinkWithDifficulty() => assertDetails("https://osu.ppy.sh/beatmapsets/1154158#osu/2768184", LinkAction.OpenBeatmap, "2768184");

        // CF: MessageFormatter.GetLinkDetails | switch args[2] | case d
        // CF: MessageFormatter.GetLinkDetails | mainArg == discussions | TRUE
        [Test]
        public void TestBeatmapSetDiscussionIsExternal() => assertDetails("https://osu.ppy.sh/d/discussions", LinkAction.External, "https://osu.ppy.sh/d/discussions");

        // CF: MessageFormatter.GetLinkDetails | int.TryParse(args[4]) | FALSE
        // CF: MessageFormatter.GetLinkDetails | int.TryParse(trimmed) set | TRUE
        [Test]
        public void TestBeatmapSetLinkWithNonNumericExtraSegment() => assertDetails("https://osu.ppy.sh/beatmapsets/77/xyz", LinkAction.OpenBeatmapSet, "77");

        // CF: MessageFormatter.GetLinkDetails | int.TryParse(trimmed) set | FALSE
        [Test]
        public void TestBeatmapSetLinkNonNumericIsExternal() => assertDetails("https://osu.ppy.sh/s/abc", LinkAction.External, "https://osu.ppy.sh/s/abc");

        // CF: MessageFormatter.GetLinkDetails | switch args[2] | case u
        // CF: MessageFormatter.getUserLink | int.TryParse(argument) | TRUE
        [Test]
        public void TestUserLinkById()
        {
            var details = MessageFormatter.GetLinkDetails("https://osu.ppy.sh/u/123");
            Assert.That(details.Action, Is.EqualTo(LinkAction.OpenUserProfile));
            Assert.That(((APIUser)details.Argument).Id, Is.EqualTo(123));
        }

        // CF: MessageFormatter.GetLinkDetails | switch args[2] | case users
        // CF: MessageFormatter.getUserLink | int.TryParse(argument) | FALSE
        [Test]
        public void TestUserLinkByName()
        {
            var details = MessageFormatter.GetLinkDetails("https://osu.ppy.sh/users/peppy");
            Assert.That(details.Action, Is.EqualTo(LinkAction.OpenUserProfile));
            Assert.That(((APIUser)details.Argument).Username, Is.EqualTo("peppy"));
        }

        // CF: MessageFormatter.GetLinkDetails | switch args[2] | case wiki
        [Test]
        public void TestWikiLink() => assertDetails("https://osu.ppy.sh/wiki/Performance_Points/x", LinkAction.OpenWiki, "Performance_Points/x");

        // CF: MessageFormatter.GetLinkDetails | switch args[2] | case home
        // CF: MessageFormatter.GetLinkDetails | mainArg != changelog | TRUE
        [Test]
        public void TestHomeNonChangelogIsExternal() => assertDetails("https://osu.ppy.sh/home/news", LinkAction.External, "https://osu.ppy.sh/home/news");

        // CF: MessageFormatter.GetLinkDetails | mainArg != changelog | FALSE
        // CF: MessageFormatter.GetLinkDetails | switch args.Length changelog | case 4
        [Test]
        public void TestChangelogRoot() => assertDetails("https://osu.ppy.sh/home/changelog", LinkAction.OpenChangelog, string.Empty);

        // CF: MessageFormatter.GetLinkDetails | switch args.Length changelog | case 6
        [Test]
        public void TestChangelogBuild() => assertDetails("https://osu.ppy.sh/home/changelog/lazer/2021.1006", LinkAction.OpenChangelog, "lazer/2021.1006");

        // CF: MessageFormatter.GetLinkDetails | switch args.Length changelog | default
        [Test]
        public void TestChangelogOtherLengthIsExternal() => assertDetails("https://osu.ppy.sh/home/changelog/lazer", LinkAction.External, "https://osu.ppy.sh/home/changelog/lazer");

        // CF: MessageFormatter.GetLinkDetails | switch args[2] | case multiplayer
        // CF: MessageFormatter.GetLinkDetails | mainArg != rooms | TRUE
        [Test]
        public void TestMultiplayerNonRoomsIsExternal() => assertDetails("https://osu.ppy.sh/multiplayer/x", LinkAction.External, "https://osu.ppy.sh/multiplayer/x");

        // CF: MessageFormatter.GetLinkDetails | mainArg != rooms | FALSE
        // CF: MessageFormatter.GetLinkDetails | args.Length == 5 | TRUE
        [Test]
        public void TestMultiplayerRoomLink() => assertDetails("https://osu.ppy.sh/multiplayer/rooms/42", LinkAction.JoinRoom, "42");

        // CF: MessageFormatter.GetLinkDetails | args.Length == 5 | FALSE
        [Test]
        public void TestMultiplayerRoomsWithoutIdIsExternal() => assertDetails("https://osu.ppy.sh/multiplayer/rooms", LinkAction.External, "https://osu.ppy.sh/multiplayer/rooms");

        // CF: MessageFormatter.GetLinkDetails | switch args[2] | default
        [Test]
        public void TestUnknownWebsitePathIsExternal() => assertDetails("https://osu.ppy.sh/foo/bar", LinkAction.External, "https://osu.ppy.sh/foo/bar");

        // CF: MessageFormatter.GetLinkDetails | switch scheme | case osu
        // CF: MessageFormatter.GetLinkDetails | args.Length < 3 | TRUE
        [Test]
        public void TestOsuSchemeTooShortIsExternal() => assertDetails("osu://chan", LinkAction.External, "osu://chan");

        // CF: MessageFormatter.GetLinkDetails | args.Length < 3 | FALSE
        // CF: MessageFormatter.GetLinkDetails | switch args[1] | case chan
        [Test]
        public void TestOsuChannel() => assertDetails("osu://chan/%23osu", LinkAction.OpenChannel, "#osu");

        // CF: MessageFormatter.GetLinkDetails | switch args[1] | case edit
        [Test]
        public void TestOsuEdit() => assertDetails("osu://edit/00:12:345", LinkAction.OpenEditorTimestamp, "00:12:345");

        // CF: MessageFormatter.GetLinkDetails | switch args[1] | case b
        [Test]
        public void TestOsuBeatmap() => assertDetails("osu://b/9", LinkAction.OpenBeatmap, "9");

        // CF: MessageFormatter.GetLinkDetails | switch args[1] | case s
        [Test]
        public void TestOsuBeatmapSet() => assertDetails("osu://s/9", LinkAction.OpenBeatmapSet, "9");

        // CF: MessageFormatter.GetLinkDetails | switch args[1] | case dl
        [Test]
        public void TestOsuDownload() => assertDetails("osu://dl/9", LinkAction.OpenBeatmapSet, "9");

        // CF: MessageFormatter.GetLinkDetails | switch args[1] | case spectate
        [Test]
        public void TestOsuSpectate() => assertDetails("osu://spectate/9", LinkAction.Spectate, "9");

        // CF: MessageFormatter.GetLinkDetails | switch args[1] | case u
        [Test]
        public void TestOsuUser()
        {
            var details = MessageFormatter.GetLinkDetails("osu://u/5");
            Assert.That(details.Action, Is.EqualTo(LinkAction.OpenUserProfile));
            Assert.That(((APIUser)details.Argument).Id, Is.EqualTo(5));
        }

        // CF: MessageFormatter.GetLinkDetails | switch args[1] | case room
        [Test]
        public void TestOsuRoom() => assertDetails("osu://room/9", LinkAction.JoinRoom, "9");

        // CF: MessageFormatter.GetLinkDetails | switch args[1] | default
        [Test]
        public void TestOsuUnknownIsExternal() => assertDetails("osu://zzz/9", LinkAction.External, "osu://zzz/9");

        // CF: MessageFormatter.GetLinkDetails | switch scheme | default
        [Test]
        public void TestUnknownSchemeIsExternal() => assertDetails("ftp://host/a/b", LinkAction.External, "ftp://host/a/b");

        // CF: MessageFormatter.handleMatches | matches loop | loop 1
        // CF: MessageFormatter.handleMatches | escapeChars != null | TRUE
        // CF: MessageFormatter.handleMatches | result.Links.Exists overlap | FALSE
        [Test]
        public void TestFormatNewStyleLink()
        {
            var result = MessageFormatter.FormatText("[https://osu.ppy.sh/b/1234 Beatmap]");

            Assert.That(result.Text, Is.EqualTo("Beatmap"));
            Assert.That(result.Links, Has.Count.EqualTo(1));
            Assert.That(result.Links[0].Index, Is.EqualTo(0));
            Assert.That(result.Links[0].Length, Is.EqualTo(7));
            Assert.That(result.Links[0].Url, Is.EqualTo("https://osu.ppy.sh/b/1234"));
            Assert.That(result.Links[0].Action, Is.EqualTo(LinkAction.OpenBeatmap));
        }

        // CF: MessageFormatter.handleMatches | escaped brackets unescaped | TRUE
        [Test]
        public void TestFormatEscapedBracketsAreUnescaped()
        {
            var result = MessageFormatter.FormatText(@"[https://osu.ppy.sh/b/1 a \[b\]]");

            Assert.That(result.Text, Is.EqualTo("a [b]"));
        }

        // CF: MessageFormatter.handleMatches | markdown link matched | loop 1
        [Test]
        public void TestFormatMarkdownLink()
        {
            var result = MessageFormatter.FormatText("see [test](https://osu.ppy.sh/b/1234) now");

            Assert.That(result.Text, Is.EqualTo("see test now"));
            Assert.That(result.Links.Single().Index, Is.EqualTo(4));
            Assert.That(result.Links.Single().Length, Is.EqualTo(4));
        }

        // CF: MessageFormatter.handleMatches | old-style link matched | loop 1
        [Test]
        public void TestFormatOldStyleLink()
        {
            var result = MessageFormatter.FormatText("(test)[https://osu.ppy.sh/b/1234]");

            Assert.That(result.Text, Is.EqualTo("test"));
            Assert.That(result.Links.Single().Url, Is.EqualTo("https://osu.ppy.sh/b/1234"));
        }

        // CF: MessageFormatter.handleMatches | wiki link matched | loop 1
        [Test]
        public void TestFormatWikiLink()
        {
            var result = MessageFormatter.FormatText("[[Performance Points]]");

            Assert.That(result.Text, Is.EqualTo("Performance Points"));
            Assert.That(result.Links.Single().Action, Is.EqualTo(LinkAction.OpenWiki));
            Assert.That(result.Links.Single().Argument, Is.EqualTo("Performance Points"));
        }

        // CF: MessageFormatter.handleMatches | displayText.Length == 0 or linkText.Length == 0 | TRUE
        [Test]
        public void TestFormatBlankDisplaySkipsMatch()
        {
            var result = MessageFormatter.FormatText("[[ ]]");

            Assert.That(result.Text, Is.EqualTo("[[ ]]"));
            Assert.That(result.Links, Is.Empty);
        }

        // CF: MessageFormatter.handleMatches | no regex match | loop 0
        // CF: MessageFormatter.handleAdvanced | no match | loop 0
        [Test]
        public void TestFormatPlainTextHasNoLinks()
        {
            var result = MessageFormatter.FormatText("hello world");

            Assert.That(result.Text, Is.EqualTo("hello world"));
            Assert.That(result.Links, Is.Empty);
        }

        // CF: MessageFormatter.handleAdvanced | match | loop 1
        // CF: MessageFormatter.handleAdvanced | existing links do not overlap | TRUE
        [Test]
        public void TestFormatBareLink()
        {
            var result = MessageFormatter.FormatText("go to https://osu.ppy.sh/b/1234 ok");

            Assert.That(result.Text, Is.EqualTo("go to https://osu.ppy.sh/b/1234 ok"));
            Assert.That(result.Links.Single().Index, Is.EqualTo(6));
            Assert.That(result.Links.Single().Length, Is.EqualTo("https://osu.ppy.sh/b/1234".Length));
            Assert.That(result.Links.Single().Action, Is.EqualTo(LinkAction.OpenBeatmap));
        }

        // CF: MessageFormatter.handleAdvanced | existing links do not overlap | FALSE
        [Test]
        public void TestFormatBareLinkInsideFormattedLinkIsNotDuplicated()
        {
            var result = MessageFormatter.FormatText("[https://osu.ppy.sh/b/1 https://osu.ppy.sh/b/2]");

            Assert.That(result.Links, Has.Count.EqualTo(1));
            Assert.That(result.Links[0].Url, Is.EqualTo("https://osu.ppy.sh/b/1"));
        }

        // CF: MessageFormatter.handleMatches | linkActionOverride ?? details.Action | non-null
        [Test]
        public void TestFormatEditorTimestamp()
        {
            var result = MessageFormatter.FormatText("at 00:12:345 ok");

            Assert.That(result.Links.Single().Action, Is.EqualTo(LinkAction.OpenEditorTimestamp));
            Assert.That(result.Links.Single().Index, Is.EqualTo(3));
            Assert.That(result.Links.Single().Length, Is.EqualTo(9));
        }

        // CF: MessageFormatter.handleMatches | channel matched | loop 1
        [Test]
        public void TestFormatChannel()
        {
            var result = MessageFormatter.FormatText("join #osu please");

            Assert.That(result.Links.Single().Action, Is.EqualTo(LinkAction.OpenChannel));
            Assert.That(result.Links.Single().Argument, Is.EqualTo("#osu"));
            Assert.That(result.Links.Single().Index, Is.EqualTo(5));
            Assert.That(result.Links.Single().Length, Is.EqualTo(4));
        }

        // CF: MessageFormatter.handleMatches | result.Links.Exists overlap | TRUE
        [Test]
        public void TestFormatOverlappingMatchIsIgnored()
        {
            var result = MessageFormatter.FormatText("[https://osu.ppy.sh/b/1 #osu]");

            Assert.That(result.Text, Is.EqualTo("#osu"));
            Assert.That(result.Links.Single().Action, Is.EqualTo(LinkAction.OpenBeatmap));
        }

        // CF: MessageFormatter.handleMatches | matches loop | loop n
        [Test]
        public void TestFormatMultipleLinksKeepCorrectOffsets()
        {
            var result = MessageFormatter.FormatText("[https://osu.ppy.sh/b/1 a] and [https://osu.ppy.sh/b/2 b]");

            Assert.That(result.Text, Is.EqualTo("a and b"));
            Assert.That(result.Links.Select(l => l.Index), Is.EqualTo(new[] { 0, 6 }));
        }

        // CF: MessageFormatter.handleMatches | l.Index > index | TRUE
        [Test]
        public void TestFormatShiftsLaterLinksWhenEarlierTextShrinks()
        {
            var result = MessageFormatter.FormatText("[x](https://osu.ppy.sh/b/1) [https://osu.ppy.sh/b/2 y]");

            Assert.That(result.Text, Is.EqualTo("x y"));
            Assert.That(result.Links.Select(l => l.Index), Is.EqualTo(new[] { 0, 2 }));
        }

        // CF: MessageFormatter.handleMatches | l.Index > index | FALSE
        [Test]
        public void TestFormatDoesNotShiftEarlierLinks()
        {
            var result = MessageFormatter.FormatText("[https://osu.ppy.sh/b/1 a] [x](https://osu.ppy.sh/b/2)");

            Assert.That(result.Text, Is.EqualTo("a x"));
            Assert.That(result.Links.Select(l => l.Index), Is.EqualTo(new[] { 0, 2 }));
        }

        // CF: MessageFormatter.format | emoji replaced | TRUE
        [Test]
        public void TestFormatEmojiReplaced()
        {
            var result = MessageFormatter.FormatText("hi 😀");

            Assert.That(result.Text, Is.EqualTo("hi [emoji]"));
            Assert.That(result.OriginalText, Is.EqualTo("hi 😀"));
        }

        // CF: MessageFormatter.FormatMessage | sets display content and sorted links | no throw
        [Test]
        public void TestFormatMessage()
        {
            var message = new Message(1) { Content = "#osu [[Foo]]" };

            var formatted = MessageFormatter.FormatMessage(message);

            Assert.That(formatted, Is.SameAs(message));
            Assert.That(message.DisplayContent, Is.EqualTo("#osu Foo"));
            Assert.That(message.Links.Select(l => l.Index), Is.EqualTo(new[] { 0, 5 }));
        }

        // CF: Link.Overlaps | Index < otherIndex + otherLength | TRUE
        // CF: Link.Overlaps | otherIndex < Index + Length | TRUE
        [Test]
        public void TestLinkOverlaps() => Assert.That(new Link("a", 5, 5, LinkAction.External, "").Overlaps(8, 5), Is.True);

        // CF: Link.Overlaps | Index < otherIndex + otherLength | FALSE
        [Test]
        public void TestLinkBeforeOtherDoesNotOverlap() => Assert.That(new Link("a", 0, 5, LinkAction.External, "").Overlaps(5, 5), Is.False);

        // CF: Link.Overlaps | otherIndex < Index + Length | FALSE
        [Test]
        public void TestLinkAfterOtherDoesNotOverlap() => Assert.That(new Link("a", 10, 5, LinkAction.External, "").Overlaps(0, 10), Is.False);

        // CF: Link.CompareTo | Index > otherLink.Index | TRUE
        // CF: Link.CompareTo | Index > otherLink.Index | FALSE
        [Test]
        public void TestLinkCompareTo()
        {
            var a = new Link("a", 1, 1, LinkAction.External, "");
            var b = new Link("b", 2, 1, LinkAction.External, "");

            Assert.That(b.CompareTo(a), Is.EqualTo(1));
            Assert.That(a.CompareTo(b), Is.EqualTo(-1));
        }
    }
}
