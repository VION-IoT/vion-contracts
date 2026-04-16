namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("SetPropertyPayload")]
    public record SetPropertyPayload(object Value, string Type) : IMessage;
}