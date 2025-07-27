
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kr.Common.Infrastructure.Datastore;

public interface IUnitOfWork : IDisposable
{
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task<int> SaveAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}

public sealed class UnitOfWork(DbContext Context) : IUnitOfWork
{
  
    private IDbContextTransaction? Transaction;

    public async Task<int> SaveAsync(CancellationToken cancellationToken = default) => await Context.SaveChangesAsync(cancellationToken);
    public int SaveChanges() => Context.SaveChanges();


    public async Task BeginTransactionAsync()
    {
        Transaction = await Context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (Transaction != null)
        {
            await Transaction.CommitAsync();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (Transaction != null)
        {
            await Transaction.RollbackAsync();
        }
    }

    public void Dispose()
    {
        Transaction?.Dispose();
        Context?.Dispose();
    }  
}
