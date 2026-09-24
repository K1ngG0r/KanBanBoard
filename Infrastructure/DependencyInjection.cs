using Application.Interfaces;
using Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfastructure(this IServiceCollection services)
    {
        string connectionString = "Data Source=kanban.db;";
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

        return services;
    }
}
