using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Projectors.EFCore;

public static class EfCoreProjectionExtensions
{
    public static IServiceCollection AddEfCoreProjector<TDbContext>(this IServiceCollection services
        , ServiceLifetime dbServiceLifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        var contextProvider = typeof(ContextProvider<>).MakeGenericType(typeof(TDbContext));
        services.Add(new ServiceDescriptor(typeof(IODataDbContextProvider), contextProvider, dbServiceLifetime));

        return services;
    }

}
