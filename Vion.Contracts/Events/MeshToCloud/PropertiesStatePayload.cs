using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Vion.Contracts.Events.MeshToCloud
{
    public readonly record struct PropertiesStatePayload(List<PropertyState> PropertiesState);

    public readonly record struct PropertyState(string PropertyIdentifier, JsonNode? Value);
}
