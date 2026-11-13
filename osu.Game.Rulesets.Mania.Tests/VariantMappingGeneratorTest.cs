// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Input.Bindings;

namespace osu.Game.Rulesets.Mania.Tests
{
    [TestFixture]
    public class VariantMappingGeneratorTest
    {
        private static VariantMappingGenerator create() => new VariantMappingGenerator
        {
            LeftKeys = [InputKey.A, InputKey.S, InputKey.D, InputKey.F],
            RightKeys = [InputKey.J, InputKey.K, InputKey.L, InputKey.Semicolon],
            SpecialKey = InputKey.Space,
            ActionStart = ManiaAction.Key1
        };

        // CF: VariantMappingGenerator.GenerateKeyBindingsFor | for left keys | loop 0
        // CF: VariantMappingGenerator.GenerateKeyBindingsFor | columns % 2 == 1 | TRUE
        // CF: VariantMappingGenerator.GenerateKeyBindingsFor | for right keys | loop 0
        [Test]
        public void TestSingleColumnUsesOnlySpecialKey()
        {
            var bindings = create().GenerateKeyBindingsFor(1).ToArray();

            Assert.That(bindings, Has.Length.EqualTo(2));
            Assert.That(bindings[0].KeyCombination.Keys, Is.EqualTo(new[] { InputKey.Space }));
            Assert.That((ManiaAction)bindings[0].Action, Is.EqualTo(ManiaAction.Key1));
            Assert.That(bindings[1].KeyCombination.Keys, Is.EqualTo(new[] { InputKey.None }));
            Assert.That((ManiaAction)bindings[1].Action, Is.EqualTo(ManiaAction.Key1));
        }

        // CF: VariantMappingGenerator.GenerateKeyBindingsFor | for left keys | loop n
        // CF: VariantMappingGenerator.GenerateKeyBindingsFor | for right keys | loop n
        // CF: VariantMappingGenerator.GenerateKeyBindingsFor | columns % 2 == 1 | FALSE
        [Test]
        public void TestEvenColumnsUseInnermostLeftThenFirstRightKeys()
        {
            var bindings = create().GenerateKeyBindingsFor(4).ToArray();

            Assert.That(bindings, Has.Length.EqualTo(8));
            Assert.That(bindings.Where((_, i) => i % 2 == 0).Select(b => b.KeyCombination.Keys[0]),
                Is.EqualTo(new[] { InputKey.D, InputKey.F, InputKey.J, InputKey.K }));
            Assert.That(bindings.Where((_, i) => i % 2 == 0).Select(b => (ManiaAction)b.Action),
                Is.EqualTo(new[] { ManiaAction.Key1, ManiaAction.Key2, ManiaAction.Key3, ManiaAction.Key4 }));
        }

        // CF: VariantMappingGenerator.GenerateKeyBindingsFor | columns % 2 == 1 | TRUE
        [Test]
        public void TestOddColumnsPutSpecialKeyInMiddle()
        {
            var bindings = create().GenerateKeyBindingsFor(5).ToArray();

            Assert.That(bindings.Where((_, i) => i % 2 == 0).Select(b => b.KeyCombination.Keys[0]),
                Is.EqualTo(new[] { InputKey.D, InputKey.F, InputKey.Space, InputKey.J, InputKey.K }));
        }
    }
}
