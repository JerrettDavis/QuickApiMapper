using Microsoft.EntityFrameworkCore;
using QuickApiMapper.Persistence.Abstractions.Models;
using QuickApiMapper.Persistence.Abstractions.Repositories;

namespace QuickApiMapper.Persistence.PostgreSQL.Repositories;

/// <summary>
/// PostgreSQL implementation of the integration mapping repository.
/// </summary>
public class IntegrationMappingRepository : IntegrationMappingRepositoryBase<QuickApiMapperDbContext>
{
    public IntegrationMappingRepository(QuickApiMapperDbContext context)
        : base(context)
    {
    }

    protected override DbSet<IntegrationMappingEntity> IntegrationMappings => Context.IntegrationMappings;

    protected override DbSet<StaticValueEntity> StaticValues => Context.StaticValues;
}
