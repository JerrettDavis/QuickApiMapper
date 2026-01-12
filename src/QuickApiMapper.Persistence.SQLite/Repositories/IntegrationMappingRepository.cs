using Microsoft.EntityFrameworkCore;
using QuickApiMapper.Persistence.Abstractions.Models;
using QuickApiMapper.Persistence.Abstractions.Repositories;

namespace QuickApiMapper.Persistence.SQLite.Repositories;

/// <summary>
/// SQLite implementation of the integration mapping repository.
/// </summary>
public class IntegrationMappingRepository : IntegrationMappingRepositoryBase<QuickApiMapperSqliteDbContext>
{
    public IntegrationMappingRepository(QuickApiMapperSqliteDbContext context)
        : base(context)
    {
    }

    protected override DbSet<IntegrationMappingEntity> IntegrationMappings => Context.IntegrationMappings;

    protected override DbSet<StaticValueEntity> StaticValues => Context.StaticValues;
}
