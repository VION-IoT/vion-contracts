using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.MeshToCloud
{
    [Schema("MeasuringPointsStatePayload")]
    public class MeasuringPointsStatePayload : IMessage
    {
        public required List<ServiceProvider> ServiceProviders { get; set; } = [];

        public required DateTime MeasuredAtUtc { get; set; }

        public class ServiceProvider
        {
            public required string Identifier { get; set; }

            public required List<Service> Services { get; set; } = [];
        }

        public class Service
        {
            public required string Identifier { get; set; }

            public required List<MeasuringPoint> MeasuringPoints { get; set; } = [];

            public readonly record struct MeasuringPoint(string Identifier, object Value);
        }
    }
}