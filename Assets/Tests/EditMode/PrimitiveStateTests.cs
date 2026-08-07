using System.Reflection;
using NUnit.Framework;
using Shift.Core;

namespace Shift.Tests.EditMode
{
    public class PrimitiveStateTests
    {
        /// <summary>
        /// Guards the replication strategy. NGO's NetworkVariable&lt;T&gt; requires an unmanaged
        /// type, so the day someone adds a GameObject reference "just for convenience" this test
        /// fails instead of the netcode quietly becoming impossible.
        /// </summary>
        [Test]
        public void EveryField_IsAnUnmanagedValueType()
        {
            FieldInfo[] fields = typeof(PrimitiveState)
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotEmpty(fields);

            foreach (FieldInfo field in fields)
            {
                Assert.IsTrue(field.FieldType.IsValueType,
                    $"PrimitiveState.{field.Name} is a reference type ({field.FieldType.Name}). " +
                    "The struct must stay unmanaged so it can be network-replicated.");
            }
        }

        [Test]
        public void Normal_MatchesUnityStockPhysics()
        {
            Assert.AreEqual(MassClass.Normal, PrimitiveState.Normal.Mass);
            Assert.AreEqual(PrimitiveState.NormalFriction, PrimitiveState.Normal.Friction);
            Assert.AreEqual(0f, PrimitiveState.Normal.Bounce);
            Assert.AreEqual(1f, PrimitiveState.Normal.GravityStrength);
            Assert.AreEqual(1f, PrimitiveState.Normal.TimeRate);
            Assert.IsFalse(PrimitiveState.Normal.Adhesion);
            Assert.IsFalse(PrimitiveState.Normal.Ghost);
        }

        /// <summary>Every scale being exactly 1.0 at Normal is the mechanism that makes the motor
        /// refactor provably feel-neutral.</summary>
        [Test]
        public void MassProfile_Normal_IsIdentity()
        {
            MassProfile normal = MassProfile.For(MassClass.Normal);

            Assert.AreEqual(MassProfile.NormalKilograms, normal.Kilograms);
            Assert.AreEqual(1f, normal.SpeedScale);
            Assert.AreEqual(1f, normal.JumpScale);
            Assert.AreEqual(1f, normal.ThrustScale);
            Assert.AreEqual(1f, normal.CarryScale);
        }

        [Test]
        public void MassProfile_HeavierIsSlowerAndCarriesMore()
        {
            MassProfile light = MassProfile.For(MassClass.Light);
            MassProfile heavy = MassProfile.For(MassClass.Heavy);

            Assert.Less(heavy.SpeedScale, light.SpeedScale);
            Assert.Less(heavy.JumpScale, light.JumpScale);
            Assert.Less(heavy.ThrustScale, light.ThrustScale);
            Assert.Greater(heavy.CarryScale, light.CarryScale);
        }
    }
}
