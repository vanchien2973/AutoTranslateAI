namespace Domain.Exceptions;

public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string entity, object from, object to)
        : base($"Invalid state transition on {entity}: cannot move from '{from}' to '{to}'.")
    {
        Entity = entity;
        From = from.ToString() ?? string.Empty;
        To = to.ToString() ?? string.Empty;
    }

    public string Entity { get; }
    public string From { get; }
    public string To { get; }
}
