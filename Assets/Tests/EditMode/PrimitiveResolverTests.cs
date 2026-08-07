using System.Collections.Generic;
using NUnit.Framework;
using Shift.Core;
using UnityEngine;

namespace Shift.Tests.EditMode
{
    public class PrimitiveResolverTests
    {
        private static PrimitiveOverride Nothing => PrimitiveOverride.Empty;

        [Test]
        public void EmptyStack_ReproducesBaselineExactly()
        {
            PrimitiveState resolved = PrimitiveResolver.Resolve(
                PrimitiveState.Normal, Nothing, null, Nothing);

            Assert.IsTrue(resolved.Equals(PrimitiveState.Normal),
                "An empty override stack must leave the baseline untouched, or the engine is not feel-neutral.");
        }

        /// <summary>The direct guarantee behind "a modifier is a row of values with holes".</summary>
        [Test]
        public void PartialOverride_LeavesUnsetPrimitivesAtBaseline()
        {
            PrimitiveOverride spider = Nothing;
            spider.Set = PrimitiveMask.Adhesion | PrimitiveMask.Mass;
            spider.Adhesion = true;
            spider.Mass = MassClass.Light;

            PrimitiveState resolved = PrimitiveResolver.Resolve(
                PrimitiveState.Normal, Nothing, null, spider);

            Assert.AreEqual(MassClass.Light, resolved.Mass);
            Assert.IsTrue(resolved.Adhesion);

            Assert.AreEqual(PrimitiveState.Normal.Friction, resolved.Friction);
            Assert.AreEqual(PrimitiveState.Normal.Bounce, resolved.Bounce);
            Assert.AreEqual(PrimitiveState.Normal.GravityDirection, resolved.GravityDirection);
            Assert.AreEqual(PrimitiveState.Normal.GravityStrength, resolved.GravityStrength);
            Assert.AreEqual(PrimitiveState.Normal.Ghost, resolved.Ghost);
            Assert.AreEqual(PrimitiveState.Normal.Charge, resolved.Charge);
            Assert.AreEqual(PrimitiveState.Normal.TimeRate, resolved.TimeRate);
        }

        [Test]
        public void ActiveModifier_BeatsField()
        {
            PrimitiveOverride lowGravityField = Nothing;
            lowGravityField.Set = PrimitiveMask.Gravity;
            lowGravityField.GravityDirection = Vector3.down;
            lowGravityField.GravityStrength = 0.3f;

            PrimitiveOverride heavyGravityModifier = Nothing;
            heavyGravityModifier.Set = PrimitiveMask.Gravity;
            heavyGravityModifier.GravityDirection = Vector3.down;
            heavyGravityModifier.GravityStrength = 2f;

            PrimitiveState resolved = PrimitiveResolver.Resolve(
                PrimitiveState.Normal,
                Nothing,
                new List<PrimitiveOverride> { lowGravityField },
                heavyGravityModifier);

            Assert.AreEqual(2f, resolved.GravityStrength,
                "The player's deliberate choice must not be silently vetoed by world state.");
        }

        [Test]
        public void Field_BeatsIntrinsic()
        {
            PrimitiveOverride leadCrate = Nothing;
            leadCrate.Set = PrimitiveMask.Mass;
            leadCrate.Mass = MassClass.Heavy;

            PrimitiveOverride featherField = Nothing;
            featherField.Set = PrimitiveMask.Mass;
            featherField.Mass = MassClass.Tiny;

            PrimitiveState resolved = PrimitiveResolver.Resolve(
                PrimitiveState.Normal,
                leadCrate,
                new List<PrimitiveOverride> { featherField },
                Nothing);

            Assert.AreEqual(MassClass.Tiny, resolved.Mass);
        }

        [Test]
        public void Fields_ApplyInListOrder_LastWins()
        {
            PrimitiveOverride first = Nothing;
            first.Set = PrimitiveMask.TimeRate;
            first.TimeRate = 0.5f;

            PrimitiveOverride second = Nothing;
            second.Set = PrimitiveMask.TimeRate;
            second.TimeRate = 1.5f;

            PrimitiveState resolved = PrimitiveResolver.Resolve(
                PrimitiveState.Normal,
                Nothing,
                new List<PrimitiveOverride> { first, second },
                Nothing);

            Assert.AreEqual(1.5f, resolved.TimeRate);
        }

        [Test]
        public void Resolve_ClampsOutOfRangeValues()
        {
            PrimitiveOverride absurd = Nothing;
            absurd.Set = PrimitiveMask.Bounce | PrimitiveMask.TimeRate | PrimitiveMask.Friction;
            absurd.Bounce = 5f;
            absurd.TimeRate = 99f;
            absurd.Friction = -3f;

            PrimitiveState resolved = PrimitiveResolver.Resolve(
                PrimitiveState.Normal, Nothing, null, absurd);

            Assert.AreEqual(PrimitiveState.MaxBounce, resolved.Bounce);
            Assert.AreEqual(PrimitiveState.MaxTimeRate, resolved.TimeRate);
            Assert.AreEqual(0f, resolved.Friction);
        }

        [Test]
        public void Resolve_ZeroGravityDirection_FallsBackToDown()
        {
            PrimitiveOverride broken = Nothing;
            broken.Set = PrimitiveMask.Gravity;
            broken.GravityDirection = Vector3.zero;
            broken.GravityStrength = 1f;

            PrimitiveState resolved = PrimitiveResolver.Resolve(
                PrimitiveState.Normal, Nothing, null, broken);

            Assert.AreEqual(Vector3.down, resolved.GravityDirection);
        }

        [Test]
        public void LegalMask_MatchesTheDocumentedAppliesToColumn()
        {
            Assert.AreEqual(PrimitiveMask.Gravity | PrimitiveMask.TimeRate,
                PrimitiveSubjects.LegalMask(PrimitiveSubject.Field));

            Assert.AreEqual(PrimitiveMask.Friction | PrimitiveMask.Bounce | PrimitiveMask.Charge,
                PrimitiveSubjects.LegalMask(PrimitiveSubject.Surface));

            Assert.AreEqual(PrimitiveMask.None,
                PrimitiveSubjects.IllegalBits(PrimitiveSubject.Player, PrimitiveMask.All));

            Assert.AreEqual(PrimitiveMask.Adhesion,
                PrimitiveSubjects.IllegalBits(PrimitiveSubject.Prop, PrimitiveMask.All));
        }
    }
}
