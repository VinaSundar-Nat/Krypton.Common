using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kr.Common.Infrastructure.Datastore;

public abstract class BaseRepository<TEntity>(ILogger<BaseRepository<TEntity>> Logger , DbContext Context)
    where TEntity : BaseEntity<TEntity>
{
    protected readonly DbSet<TEntity> DBset = Context.Set<TEntity>();
    protected async Task<int> SaveAsync(CancellationToken cancellationToken = default) => await Context.SaveChangesAsync(cancellationToken);
    protected int SaveChanges() => Context.SaveChanges();

    public async Task<bool> Create(TEntity entity, CancellationToken cancellationToken = default)
    {
        DBset.Add(entity);
        var response = await Context.SaveChangesAsync(cancellationToken);
        return response > 0;
    }

    public async Task<bool> Update(TEntity entity, CancellationToken cancellationToken = default)
    {
        DBset.Update(entity);
        var response = await Context.SaveChangesAsync(cancellationToken);
        return response > 0;
    }

    public async Task<bool> Delete(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (Context.Entry(entity).State == EntityState.Detached)        
            DBset.Attach(entity);        

        DBset.Remove(entity);

        var response = await Context.SaveChangesAsync(cancellationToken);
        return response > 0;
    }

    public async Task BulkUpsertAsync(IList<TEntity> inserts, IList<TEntity> updates,
           IList<TEntity> deletes, CancellationToken cancellationToken = default)
    {
        if ((inserts?.Count ?? 0) + (updates?.Count ?? 0) + (deletes?.Count ?? 0) == 0)
            return;

        using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Process inserts
            if (inserts?.Any() ?? false)
            {
                Context.AddRange(inserts);
            }

            // Process updates
            if (updates?.Any() ?? false)
            {
                foreach (var entity in updates)
                {
                    DBset.Update(entity);
                }
            }

            // Process deletes
            if (deletes?.Any() ?? false)
            {
                foreach (var entity in deletes)
                {
                    if (Context.Entry(entity).State == EntityState.Detached)
                        DBset.Attach(entity);

                    DBset.Remove(entity);
                }
            }

            var affectedRows = await Context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Logger.LogInformation("Bulk upsert completed successfully. Affected rows: {AffectedRows}", affectedRows);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "Error performing bulk upsert for entity type {EntityType}", typeof(TEntity).Name);
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    public async Task<ICollection<TEntity>> BulkCreateAsync(ICollection<TEntity> entityCollection, CancellationToken cancellationToken = default)
    {
        if (entityCollection?.Any() != true)
            return [];

        using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            Context.AddRange(entityCollection);
            var inserted = await Context.SaveChangesAsync(cancellationToken);

            if (inserted != entityCollection.Count)
            {
                throw new DbUpdateException($"Error performing bulk create - expected {entityCollection.Count} insertions, but only {inserted} were completed for {nameof(TEntity)}");
            }

            await transaction.CommitAsync(cancellationToken);
            Logger.LogInformation("Bulk create completed successfully. Inserted {Count} entities of type {EntityType}",
                entityCollection.Count, typeof(TEntity).Name);

            return entityCollection;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "Error performing bulk create for entity type {EntityType}", typeof(TEntity).Name);
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    public async Task BulkDeleteAsync(IList<TEntity> entityCollection, CancellationToken cancellationToken = default)
    {
        if (entityCollection?.Any() != true)
            return;

        using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var entity in entityCollection)
            {
                DBset.Remove(entity);
            }

            var deletedCount = await Context.SaveChangesAsync(cancellationToken);

            if (deletedCount != entityCollection.Count)
            {
                throw new DbUpdateException($"Error performing bulk delete - expected {entityCollection.Count} deletions, but only {deletedCount} were completed for {nameof(TEntity)}");
            }

            await transaction.CommitAsync(cancellationToken);
            Logger.LogInformation("Bulk delete completed successfully. Deleted {Count} entities of type {EntityType}",
                entityCollection.Count, typeof(TEntity).Name);

            return;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "Error performing bulk delete for entity type {EntityType}", typeof(TEntity).Name);
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    // Additional helper method for better bulk operations
    public async virtual Task<bool> BulkUpdateAsync(IList<TEntity> entityCollection, CancellationToken cancellationToken = default)
    {
        if (entityCollection?.Any() != true)
            return true;

        using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var entity in entityCollection)
            {
                DBset.Update(entity);
            }

            var updatedCount = await Context.SaveChangesAsync(cancellationToken);

            if (updatedCount != entityCollection.Count)
            {
                throw new DbUpdateException($"Error performing bulk update - expected {entityCollection.Count} updates, but only {updatedCount} were completed for {nameof(TEntity)}");
            }

            await transaction.CommitAsync(cancellationToken);
            Logger.LogInformation("Bulk update completed successfully. Updated {Count} entities of type {EntityType}",
                entityCollection.Count, typeof(TEntity).Name);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "Error performing bulk update for entity type {EntityType}", typeof(TEntity).Name);
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }
}

