using Kr.Common.Extensions;
using Kr.Common.Infrastructure.Datastore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kr.Common.Infrastructure.Datastore;

public abstract class BaseContext<T>(DbContextOptions<T> options,
                    IOptions<DbSettings> dbSettings) : DbContext(options) where T : DbContext
{
    private readonly DbSettings? _dbSettings = dbSettings?.Value;
  
  
    public async override Task<int> SaveChangesAsync(CancellationToken token = default)
    {
        //TODO: Implement any pre-save logic here, such as auditing or event notification.
        await NotifyChanges();
        return await base.SaveChangesAsync(token);
    }

    public abstract Task NotifyChanges();
}
