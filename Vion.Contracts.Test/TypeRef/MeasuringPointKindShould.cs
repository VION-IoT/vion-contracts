using System;
using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.TypeRef
{
    [TestClass]
    public class MeasuringPointKindShould
    {
        [TestMethod]
        public void DefineThreeValues()
        {
            var values = Enum.GetValues<MeasuringPointKind>();
            Assert.HasCount(3, values);
        }

        // Wire-integer values are part of the contract: cloud-api persists them by ordinal,
        // and a change is a breaking schema migration. DataRow defeats the MSTEST0032 analyzer,
        // which flags Assert.AreEqual on two compile-time constants as always-true.
        [TestMethod]
        [DataRow(MeasuringPointKind.Measurement, 0)]
        [DataRow(MeasuringPointKind.Total, 1)]
        [DataRow(MeasuringPointKind.TotalIncreasing, 2)]
        public void PinWireIntegerValue(MeasuringPointKind kind, int expected)
        {
            Assert.AreEqual(expected, (int)kind);
        }
    }
}
