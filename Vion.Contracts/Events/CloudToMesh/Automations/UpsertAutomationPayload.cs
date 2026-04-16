namespace Vion.Contracts.Events.CloudToMesh.Automations
{
    [Schema("UpsertAutomationPayload")]
    public class UpsertAutomationPayload : IMessage
    {
        public required Automation Automation { get; set; }
    }
}