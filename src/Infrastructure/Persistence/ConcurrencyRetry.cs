using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal static class ConcurrencyRetry
{
    public static async Task SaveChangesWithRetryAsync(
        this DbContext dbContext,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxRetries)
            {
                foreach (var entry in ex.Entries)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues is null)
                    {
                        throw;
                    }

                    entry.OriginalValues.SetValues(databaseValues);
                }
            }
        }
    }
}
