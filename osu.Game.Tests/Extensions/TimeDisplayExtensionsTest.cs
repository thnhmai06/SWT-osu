// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using NUnit.Framework;
using osu.Game.Extensions;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Tests.Extensions
{
    [TestFixture]
    public class TimeDisplayExtensionsTest
    {
        private static readonly TimeSpan cutoff = TimeSpan.FromSeconds(5);

        [SetUp]
        public void SetUp()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        // CF: TimeDisplayExtensions.ToEditorFormattedString | timeSpan < TimeSpan.Zero | FALSE
        [Test]
        public void TestEditorFormatPositive()
        {
            Assert.That(TimeSpan.FromMilliseconds(65050).ToEditorFormattedString(), Is.EqualTo("01:05:050"));
            Assert.That(65050d.ToEditorFormattedString(), Is.EqualTo("01:05:050"));
        }

        // CF: TimeDisplayExtensions.ToFormattedDuration | timeSpan.TotalDays >= 1 | TRUE
        [Test]
        public void TestDurationDays()
        {
            Assert.That(new TimeSpan(1, 1, 5, 10).ToFormattedDuration().ToString(), Is.EqualTo("01:01:05:10"));
        }

        // CF: TimeDisplayExtensions.ToFormattedDuration | timeSpan.TotalDays >= 1 | FALSE
        // CF: TimeDisplayExtensions.ToFormattedDuration | timeSpan.TotalHours >= 1 | TRUE
        [Test]
        public void TestDurationHours()
        {
            Assert.That(new TimeSpan(1, 5, 10).ToFormattedDuration().ToString(), Is.EqualTo("01:05:10"));
        }

        // CF: TimeDisplayExtensions.ToFormattedDuration | timeSpan.TotalHours >= 1 | FALSE
        [Test]
        public void TestDurationMinutes()
        {
            Assert.That(new TimeSpan(0, 5, 10).ToFormattedDuration().ToString(), Is.EqualTo("05:10"));
            Assert.That(10000d.ToFormattedDuration().ToString(), Is.EqualTo("00:10"));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | time.Date == default | TRUE
        [Test]
        public void TestRelativeDefaultDate()
        {
            Assert.That(new DateTimeOffset(1, 1, 1, 5, 0, 0, TimeSpan.Zero).ToShortRelativeTime(cutoff), Is.EqualTo("-"));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | time.Date == default | FALSE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | difference < lowerCutoff | TRUE
        [Test]
        public void TestRelativeBelowCutoff()
        {
            Assert.That(DateTimeOffset.Now.ToShortRelativeTime(TimeSpan.FromMinutes(1)), Is.EqualTo(CommonStrings.TimeNow.ToString()));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | difference < lowerCutoff | FALSE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | difference.TotalMinutes < 1 | TRUE
        [Test]
        public void TestRelativeSeconds()
        {
            Assert.That(DateTimeOffset.Now.AddSeconds(-30).ToShortRelativeTime(cutoff), Does.EndWith("secs").And.StartsWith("3"));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | difference.TotalMinutes < 1 | FALSE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | difference.TotalHours < 1 | TRUE
        [Test]
        public void TestRelativeMinutes()
        {
            Assert.That(DateTimeOffset.Now.AddMinutes(-30).AddSeconds(-5).ToShortRelativeTime(cutoff), Is.EqualTo("30 mins"));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | difference.TotalHours < 1 | FALSE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | difference.TotalDays < 1 | TRUE
        [Test]
        public void TestRelativeHours()
        {
            Assert.That(DateTimeOffset.Now.AddHours(-5).AddMinutes(-5).ToShortRelativeTime(cutoff), Is.EqualTo("5 hrs"));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | difference.TotalDays < 1 | FALSE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | time > now.AddMonths(-1) | TRUE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | difference.TotalDays < 2 | TRUE
        [Test]
        public void TestRelativeOneDay()
        {
            Assert.That(DateTimeOffset.Now.AddHours(-30).ToShortRelativeTime(cutoff), Is.EqualTo("1dy"));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | difference.TotalDays < 2 | FALSE
        [Test]
        public void TestRelativeSeveralDays()
        {
            Assert.That(DateTimeOffset.Now.AddDays(-10).AddHours(-1).ToShortRelativeTime(cutoff), Is.EqualTo("10dys"));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | time > now.AddMonths(-1) | FALSE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | loop months 1..11 | loop n
        // CF: TimeDisplayExtensions.ToShortRelativeTime | time > now.AddMonths(-(months + 1)) | TRUE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | months == 1 | TRUE
        [Test]
        public void TestRelativeOneMonth()
        {
            Assert.That(DateTimeOffset.Now.AddMonths(-1).AddDays(-2).ToShortRelativeTime(cutoff), Is.EqualTo("1mo"));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | time > now.AddMonths(-(months + 1)) | FALSE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | months == 1 | FALSE
        [Test]
        public void TestRelativeSeveralMonths()
        {
            Assert.That(DateTimeOffset.Now.AddMonths(-4).AddDays(-2).ToShortRelativeTime(cutoff), Is.EqualTo("4mos"));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | loop months 1..11 | loop 0
        // CF: TimeDisplayExtensions.ToShortRelativeTime | while time <= now.AddYears(-(years + 1)) | FALSE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | years == 1 | TRUE
        [Test]
        public void TestRelativeOneYear()
        {
            Assert.That(DateTimeOffset.Now.AddYears(-1).AddDays(-5).ToShortRelativeTime(cutoff), Is.EqualTo("1yr"));
        }

        // CF: TimeDisplayExtensions.ToShortRelativeTime | while time <= now.AddYears(-(years + 1)) | TRUE
        // CF: TimeDisplayExtensions.ToShortRelativeTime | years == 1 | FALSE
        [Test]
        public void TestRelativeSeveralYears()
        {
            Assert.That(DateTimeOffset.Now.AddYears(-3).AddDays(-5).ToShortRelativeTime(cutoff), Is.EqualTo("3yrs"));
        }
    }
}
