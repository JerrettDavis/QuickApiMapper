using QuickApiMapper.Persistence.Abstractions.Repositories;

namespace QuickApiMapper.Persistence.PostgreSQL.Repositories;

/// <summary>
/// PostgreSQL implementation of the Unit of Work pattern.
/// </summary>
public class UnitOfWork : UnitOfWorkBase<QuickApiMapperDbContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="integrationMappings">The integration mapping repository.</param>
    public UnitOfWork(
        QuickApiMapperDbContext context,
        IIntegrationMappingRepository integrationMappings)
        : base(context, integrationMappings)
    {
    }
}
