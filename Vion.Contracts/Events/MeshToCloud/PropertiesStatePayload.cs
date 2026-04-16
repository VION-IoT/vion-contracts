using System.Collections.Generic;

namespace Vion.Contracts.Events.MeshToCloud
{
    public readonly record struct PropertiesStatePayload(List<PropertyState> PropertiesState);

    public readonly record struct PropertyState(string PropertyIdentifier, object Value);
}