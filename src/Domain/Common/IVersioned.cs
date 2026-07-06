namespace Domain.Common;

public interface IVersioned
{
    int RowVersion { get; }
}
