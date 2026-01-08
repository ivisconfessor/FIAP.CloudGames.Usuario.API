using System.Text;
using FIAP.CloudGames.Usuario.API.Infrastructure.Data;
using FIAP.CloudGames.Usuario.API.Infrastructure.Services;
using FIAP.CloudGames.Usuario.API.Infrastructure.EventSourcing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FIAP.CloudGames.Usuario.API.Infrastructure.Configurations;

public static class DependencyResolverConfigurationExtensions
{
    public static void IntegrateDependencyResolver(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuração do DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("FIAPCloudGamesUsuarios"));

        // Configuração do JWT
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
            };
        });

        // Configuração da Autorização
        services.AddAuthorization();

        // Registro dos serviços
        services.AddScoped<IJwtService, JwtService>();
        services.AddSingleton<IEventStore, EventStore>();
        
        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
    }
}
