using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace QuickApiMapper.Persistence.Abstractions.Repositories;

/// <summary>
/// Base implementation of the Unit of Work pattern.
/// Provides common transaction management logic for all database providers.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public abstract class UnitOfWorkBase<TContext> : IUnitOfWork
    where TContext : DbContext
{
    /// <summary>
    /// Gets the database context.
    /// </summary>
    protected readonly TContext Context;

    private IDbContextTransaction? _transaction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkBase{TContext}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="integrationMappings">The integration mapping repository.</param>
    protected UnitOfWorkBase(
        TContext context,
        IIntegrationMappingRepository integrationMappings)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        IntegrationMappings = integrationMappings ?? throw new ArgumentNullException(nameof(integrationMappings));
    }

    /// <inheritdoc />
    public IIntegrationMappingRepository IntegrationMappings { get; }

    /// <inheritdoc />
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // Dispose any existing transaction before creating a new one
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
            throw new InvalidOperationException(
                "A transaction is already in progress. Commit or rollback the current transaction before starting a new one.");
        }

#pragma warning disable IDISP003 // False positive - transaction is disposed and nulled before reaching this line when not null
        _transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
#pragma warning restore IDISP003
    }

    /// <inheritdoc />
    public virtual async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await Context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public virtual async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the unit of work and its resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _disposed = true;
        }
    }
}
