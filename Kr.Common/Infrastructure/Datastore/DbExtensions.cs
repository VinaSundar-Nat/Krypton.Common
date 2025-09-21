using System;
using System.Linq.Expressions;
using System.Reflection;
using Kr.Common.Extensions;
using Kr.Common.Infrastructure.Datastore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Kr.Common.Infrastructure.Datastore;

public static class DbExtensions
{

    public static void ApplyAllConfigurations(this ModelBuilder modelBuilder, Type requestor)
    {
        var applyConfigurationMethodInfo = modelBuilder
            .GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .First(m => m.Name.Equals("ApplyConfiguration", StringComparison.OrdinalIgnoreCase));

        var associatedTypes = requestor.Assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

        var instances = associatedTypes
                        .Select(i =>
                        (
                            mpt: i.GetInterfaces()?.FirstOrDefault(a => a.GetTypeInfo().IsGenericType == true)
                                        ?.GetGenericArguments()[0],
                            mptO: Activator.CreateInstance(i)
                        )).ToList();


        instances.ForEach(it =>
        {
            if (it.mpt != null)
            {
                var concRegister = applyConfigurationMethodInfo.MakeGenericMethod(it.mpt);
                concRegister.Invoke(modelBuilder, [it.mptO]);
            }
        });
    }

    public static void DbNpgCContextSettings<T>(this IServiceCollection services, IConfiguration configuration, string source)
        where T : DbContext
    {
        DbSettings dbSettings = new();
        configuration.GetSection(source).Bind(dbSettings);

        if (!dbSettings?.IsValid ?? true)
            ArgumentNullException.ThrowIfNull(dbSettings,
               $"Error :{nameof(DbSettings)} configuration is invalid.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(dbSettings!.ConnectionString);
        dataSourceBuilder.UseNetTopologySuite();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<T>((options) =>
        {
            options.UseNpgsql(dbSettings.ConnectionString,
                o => o.UseNetTopologySuite());
            options.EnableSensitiveDataLogging();
        });
    }

    public static void DbNpgContextPoolSettings<T>(this IServiceCollection services, IConfiguration configuration, string source)
        where T : DbContext
    {
        var dbSettings = new DbSettings();
        configuration.GetSection(source).Bind(dbSettings);

        if (!dbSettings?.IsValid ?? true)
            ArgumentNullException.ThrowIfNull(dbSettings,
               $"Error :{nameof(DbSettings)} configuration is invalid.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(dbSettings!.ConnectionString);
        dataSourceBuilder.UseNetTopologySuite();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContextPool<T>((serviceProvider, options) =>
        {
            options.UseNpgsql(dbSettings.ConnectionString,
                o => o.UseNetTopologySuite());
            options.EnableSensitiveDataLogging();
        });
    }

    public static int PageTo(this int index, int take) => (index - 1) * take;   
}
