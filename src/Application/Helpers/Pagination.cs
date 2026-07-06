namespace Application.Helpers;

public static class Pagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    // Clamp caller-supplied paging into a safe range and derive the EF skip/take.
    public static (int Page, int Skip, int Take) Normalize(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return (page, (page - 1) * pageSize, pageSize);
    }
}
