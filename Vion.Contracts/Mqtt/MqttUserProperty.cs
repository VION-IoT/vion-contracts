namespace Vion.Contracts.Mqtt
{
    public record MqttUserProperty(string Name, string Value)
    {
        // ToString override to return a string representation for logging etc.
        public override string ToString()
        {
            return $"{Name}: {Value}";
        }
    }
}