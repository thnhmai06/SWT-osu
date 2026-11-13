// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Tests.Skinning
{
    [TestFixture]
    public class SerialisedDrawableInfoTest
    {
        // CF: SerialisedDrawableInfo.ctor | AutoSizeAxes.HasFlag(Axes.X) != true | TRUE
        // CF: SerialisedDrawableInfo.ctor | AutoSizeAxes.HasFlag(Axes.Y) != true | TRUE
        // CF: SerialisedDrawableInfo.ctor | component is ISerialisableDrawable | FALSE
        // CF: SerialisedDrawableInfo.ctor | component is Container<Drawable> | FALSE
        [Test]
        public void TestCaptureSimpleDrawable()
        {
            var info = new Box { Position = new Vector2(1, 2), Rotation = 30, Scale = new Vector2(2), Width = 10, Height = 20, Anchor = Anchor.Centre, Origin = Anchor.BottomRight }.CreateSerialisedInfo();

            Assert.That(info.Type, Is.EqualTo(typeof(Box)));
            Assert.That(info.Position, Is.EqualTo(new Vector2(1, 2)));
            Assert.That(info.Rotation, Is.EqualTo(30));
            Assert.That(info.Scale, Is.EqualTo(new Vector2(2)));
            Assert.That(info.Width, Is.EqualTo(10));
            Assert.That(info.Height, Is.EqualTo(20));
            Assert.That(info.Anchor, Is.EqualTo(Anchor.Centre));
            Assert.That(info.Origin, Is.EqualTo(Anchor.BottomRight));
            Assert.That(info.UsesFixedAnchor, Is.False);
            Assert.That(info.Children, Is.Empty);
        }

        // CF: SerialisedDrawableInfo.ctor | AutoSizeAxes.HasFlag(Axes.X) != true | FALSE
        // CF: SerialisedDrawableInfo.ctor | AutoSizeAxes.HasFlag(Axes.Y) != true | FALSE
        [Test]
        public void TestAutoSizedAxesAreNotCaptured()
        {
            var x = new Container { AutoSizeAxes = Axes.X, Height = 5 }.CreateSerialisedInfo();
            Assert.That(x.Width, Is.Null);
            Assert.That(x.Height, Is.EqualTo(5));

            var y = new Container { AutoSizeAxes = Axes.Y, Width = 7 }.CreateSerialisedInfo();
            Assert.That(y.Width, Is.EqualTo(7));
            Assert.That(y.Height, Is.Null);
        }

        // CF: SerialisedDrawableInfo.ctor | component is ISerialisableDrawable | TRUE
        // CF: SerialisedDrawableInfo.ctor | loop over settings source properties | loop 1
        [Test]
        public void TestCaptureSerialisableDrawableSettings()
        {
            var info = new TestSerialisable { UsesFixedAnchor = true, Value = { Value = 4 } }.CreateSerialisedInfo();

            Assert.That(info.UsesFixedAnchor, Is.True);
            Assert.That(info.Settings["value"], Is.EqualTo(4));
        }

        // CF: SerialisedDrawableInfo.ctor | component is Container<Drawable> | TRUE
        // CF: SerialisedDrawableInfo.ctor | loop over serialisable children | loop n
        [Test]
        public void TestCaptureChildrenOnlyIfSerialisable()
        {
            var container = new Container<Drawable> { Children = new Drawable[] { new TestSerialisable(), new Box(), new TestSerialisable() } };
            Assert.That(container.CreateSerialisedInfo().Children, Has.Count.EqualTo(2));
        }

        // CF: SerialisedDrawableInfo.CreateInstance | try body | no throw
        [Test]
        public void TestCreateInstanceRestoresState()
        {
            var info = new SerialisedDrawableInfo { Type = typeof(Box), Position = new Vector2(3, 4), Width = 8, Height = 9, Scale = new Vector2(2) };
            var box = (Box)info.CreateInstance();

            Assert.That(box.Position, Is.EqualTo(new Vector2(3, 4)));
            Assert.That(box.Width, Is.EqualTo(8));
            Assert.That(box.Height, Is.EqualTo(9));
            Assert.That(box.Scale, Is.EqualTo(new Vector2(2)));
        }

        // CF: SerialisedDrawableInfo.CreateInstance | catch Exception | TRUE
        [Test]
        public void TestCreateInstanceFailureReturnsEmpty()
        {
            var info = new SerialisedDrawableInfo { Type = typeof(Drawable) };
            Assert.That(info.CreateInstance(), Is.Not.Null);
        }

        // CF: SerialisableDrawableExtensions.ApplySerialisedInfo | drawableInfo.Width is float width && width != 0 | FALSE
        // CF: SerialisableDrawableExtensions.ApplySerialisedInfo | drawableInfo.Height is float height && height != 0 | FALSE
        [Test]
        public void TestZeroOrMissingSizeIgnored()
        {
            var box = new Box { Width = 3, Height = 4 };
            box.ApplySerialisedInfo(new SerialisedDrawableInfo { Width = 0, Height = null });
            Assert.That(box.Width, Is.EqualTo(3));
            Assert.That(box.Height, Is.EqualTo(4));
        }

        // CF: SerialisableDrawableExtensions.ApplySerialisedInfo | width != 0 && component is not auto-sized in X | TRUE
        // CF: SerialisableDrawableExtensions.ApplySerialisedInfo | component is CompositeDrawable with AutoSizeAxes X | TRUE
        [Test]
        public void TestAutoSizedAxisNotOverwritten()
        {
            var container = new Container { AutoSizeAxes = Axes.X };
            var originalHeight = container.Height;
            container.ApplySerialisedInfo(new SerialisedDrawableInfo { Width = 50, Height = 60 });

            Assert.That(container.AutoSizeAxes, Is.EqualTo(Axes.X));
            Assert.That(container.Height, Is.EqualTo(60));
            Assert.That(container.Height, Is.Not.EqualTo(originalHeight));
        }

        // CF: SerialisableDrawableExtensions.ApplySerialisedInfo | component is ISerialisableDrawable | TRUE
        // CF: SerialisableDrawableExtensions.ApplySerialisedInfo | drawableInfo.Settings.TryGetValue | TRUE
        // CF: SerialisableDrawableExtensions.ApplySerialisedInfo | component is Container | FALSE
        [Test]
        public void TestApplySettingsAndFixedAnchor()
        {
            var component = new TestSerialisable();
            component.ApplySerialisedInfo(new SerialisedDrawableInfo { UsesFixedAnchor = true, Settings = new Dictionary<string, object> { ["value"] = 7 } });

            Assert.That(component.UsesFixedAnchor, Is.True);
            Assert.That(component.Value.Value, Is.EqualTo(7));
        }

        // CF: SerialisableDrawableExtensions.ApplySerialisedInfo | drawableInfo.Settings.TryGetValue | FALSE
        [Test]
        public void TestMissingSettingKeepsCurrentValue()
        {
            var component = new TestSerialisable { Value = { Value = 3 } };
            component.ApplySerialisedInfo(new SerialisedDrawableInfo());
            Assert.That(component.Value.Value, Is.EqualTo(3));
        }

        // CF: SerialisableDrawableExtensions.ApplySerialisedInfo | component is Container | TRUE
        // CF: SerialisableDrawableExtensions.ApplySerialisedInfo | loop over drawableInfo.Children | loop n
        [Test]
        public void TestChildrenAreRecreated()
        {
            var container = new Container();
            var info = new SerialisedDrawableInfo();
            info.Children.Add(new SerialisedDrawableInfo { Type = typeof(Box) });
            info.Children.Add(new SerialisedDrawableInfo { Type = typeof(Box) });

            container.ApplySerialisedInfo(info);
            Assert.That(container.Children, Has.Count.EqualTo(2));
        }

        private partial class TestSerialisable : Box, ISerialisableDrawable
        {
            [SettingSource("Value")]
            public BindableInt Value { get; } = new BindableInt(1) { MinValue = 0, MaxValue = 10 };

            public bool UsesFixedAnchor { get; set; }
        }
    }
}
