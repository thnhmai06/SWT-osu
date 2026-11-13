// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Extensions;
using osu.Game.Skinning;

namespace osu.Game.Tests.Extensions
{
    [TestFixture]
    public class SmallExtensionsTest
    {
        private CultureInfo previousCulture = null!;

        [SetUp]
        public void SetUp()
        {
            previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = previousCulture;

        // CF: NumberFormattingExtensions.ToStandardFormattedString | asPercentage | FALSE
        [Test]
        public void TestPlainFormatting()
        {
            Assert.That(1.2345.ToStandardFormattedString(2), Is.EqualTo("1.23"));
            Assert.That(1234.5.ToStandardFormattedString(1), Is.EqualTo("1,234.5"));
            Assert.That(7.ToStandardFormattedString(0), Is.EqualTo("7"));
        }

        [Test]
        public void TestNegativeFormatting()
        {
            Assert.That((-1.5).ToStandardFormattedString(1), Is.EqualTo("-1.5"));
            Assert.That((-0.001).ToStandardFormattedString(2), Is.EqualTo("0"));
        }

        // CF: NumberFormattingExtensions.ToStandardFormattedString | asPercentage | TRUE
        // CF: NumberFormattingExtensions.ToStandardFormattedString | value is int | TRUE
        // CF: NumberFormattingExtensions.ToStandardFormattedString | value is int | FALSE
        [Test]
        public void TestPercentFormatting()
        {
            Assert.That(50.ToStandardFormattedString(0, true), Does.EndWith("%"));
            Assert.That(0.5.ToStandardFormattedString(2, true), Does.EndWith("%"));
        }

        [Test]
        public void TestNormalise() => Assert.That(NumberFormattingExtensions.Normalise(1.23456m, 3), Is.EqualTo(1.235m));

        // CF: TypeExtensions.GetInvariantInstantiationInfo | assemblyQualifiedName == null | FALSE
        [Test]
        public void TestInstantiationInfoStripsVersion()
        {
            string info = typeof(string).GetInvariantInstantiationInfo();
            Assert.That(info, Does.StartWith("System.String,"));
            Assert.That(info.Split(','), Has.Length.EqualTo(2));
        }

        // CF: TypeExtensions.GetInvariantInstantiationInfo | assemblyQualifiedName == null | TRUE
        [Test]
        public void TestInstantiationInfoGenericParameterThrows()
        {
            var genericParameter = typeof(List<>).GetGenericArguments()[0];
            Assert.Throws<ArgumentException>(() => genericParameter.GetInvariantInstantiationInfo());
        }

        // CF: TaskExtensions.ContinueWithSequential | cancellationToken.IsCancellationRequested (outer) | FALSE
        // CF: TaskExtensions.ContinueWithSequential | continuationTask succeeded | no throw
        [Test]
        public async Task TestSequentialRunsAfterPrevious()
        {
            var order = new List<int>();
            var first = Task.Run(() => { Thread.Sleep(50); order.Add(1); });

            await first.ContinueWithSequential(() => order.Add(2));
            Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
        }

        // CF: TaskExtensions.ContinueWithSequential | cancellationToken.IsCancellationRequested (outer) | TRUE
        [Test]
        public void TestSequentialCancelledBeforeStart()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            bool ran = false;
            var task = Task.CompletedTask.ContinueWithSequential(() => ran = true, cts.Token);

            Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
            Assert.That(ran, Is.False);
        }

        // CF: TaskExtensions.ContinueWithSequential | continuationTask.IsFaulted | TRUE
        [Test]
        public void TestSequentialPropagatesFault()
        {
            var task = Task.CompletedTask.ContinueWithSequential((Action)(() => throw new InvalidOperationException("boom")));
            var ex = Assert.ThrowsAsync<AggregateException>(async () => await task);
            Assert.That(ex!.Flatten().InnerException, Is.TypeOf<InvalidOperationException>());
        }

        // CF: TaskExtensions.ContinueWithSequential | continuationTask.IsCanceled | TRUE
        [Test]
        public void TestSequentialPropagatesCancellationOfContinuation()
        {
            var task = Task.CompletedTask.ContinueWithSequential(() => Task.FromCanceled(new CancellationToken(true)));
            Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        }

        // CF: SkinUtils.As | value | null
        // CF: SkinUtils.As | value | non-null
        [Test]
        public void TestSkinUtilsAs()
        {
            Assert.That(SkinUtils.As<int>(null), Is.Null);
            var bindable = new Bindable<int>(3);
            Assert.That(SkinUtils.As<int>(bindable), Is.SameAs(bindable));
        }
    }
}
