namespace Vion.Contracts.Events
{
    /// <summary>
    ///     Markup interface for all messages sent over the message bus.
    ///     If you implement this class you MUST specify the attribute ApiMessageInfoAttribute along with the interface,
    ///     Otherwise you'll receive runtime exception on startup!
    /// </summary>
    public interface IMessage
    {
    }
}