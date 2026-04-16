namespace Vion.Contracts.Events.CloudToMesh
{
    [Schema("InstallationTopicRetrievedPayload")]
    public record InstallationTopicRetrievedPayload(string InstallationTopic) : IMessage;
}