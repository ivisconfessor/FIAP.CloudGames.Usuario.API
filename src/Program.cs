using System.Security.Claims;
using FIAP.CloudGames.Usuario.API.Domain.Entities;
using DomainEvents = FIAP.CloudGames.Usuario.API.Domain.Events;
using FIAP.CloudGames.Usuario.API.Application.DTOs;
using FIAP.CloudGames.Usuario.API.Infrastructure.Configurations;
using FIAP.CloudGames.Usuario.API.Infrastructure.Data;
using FIAP.CloudGames.Usuario.API.Infrastructure.Services;
using FIAP.CloudGames.Usuario.API.Infrastructure.EventSourcing;
using FIAP.CloudGames.Usuario.API.Application.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using FluentValidation.AspNetCore;
using FluentValidation;
using FIAP.CloudGames.Usuario.API.Application.Validators;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Usar Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "FIAP Cloud Games - Usuários API", 
        Version = "v1",
        Description = "Microsserviço de gerenciamento de usuários e autenticação"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Registrar FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();

// Integrar todas as dependências
builder.Services.IntegrateDependencyResolver(builder.Configuration);

// Configurar Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Configurar OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FIAP.CloudGames.Usuario.API"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

var app = builder.Build();

// Seed de usuário admin para testes
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!db.Users.Any(u => u.Role == UserRole.Admin))
    {
        var admin = new User("Admin", "admin@fiap.com.br", "Admin@123", UserRole.Admin);
        db.Users.Add(admin);
        db.SaveChanges();
        
        Log.Information("Usuário admin criado com sucesso");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FIAP Cloud Games - Usuários API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// User endpoints
app.MapPost("/api/users", async (CreateUserDto dto, ApplicationDbContext db, IEventStore eventStore, ILogger<Program> logger) =>
{
    logger.LogInformation("Criando novo usuário com email: {Email}", dto.Email);
    
    if (await db.Users.AnyAsync(u => u.Email == dto.Email))
    {
        logger.LogWarning("Tentativa de criar usuário com email já existente: {Email}", dto.Email);
        return Results.BadRequest("Email já cadastrado");
    }

    var user = new User(dto.Name, dto.Email, dto.Password);
    db.Users.Add(user);
    await db.SaveChangesAsync();

    // Event Sourcing
    var userCreatedEvent = new DomainEvents.UserCreatedEvent(
        user.Id, 
        user.Name, 
        user.Email, 
        user.Role.ToString(), 
        user.CreatedAt
    );
    await eventStore.SaveEventAsync(userCreatedEvent);

    logger.LogInformation("Usuário criado com sucesso. ID: {UserId}", user.Id);

    return Results.Created($"/api/users/{user.Id}", new UserResponseDto(
        user.Id, user.Name, user.Email, user.Role.ToString(),
        user.CreatedAt, user.UpdatedAt));
})
.AllowAnonymous()
.WithName("CreateUser")
.WithOpenApi();

app.MapPost("/api/auth/login", async (LoginDto dto, ApplicationDbContext db, IJwtService jwtService, IEventStore eventStore, ILogger<Program> logger) =>
{
    logger.LogInformation("Tentativa de login para o email: {Email}", dto.Email);
    
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
    if (user == null || !user.ValidatePassword(dto.Password))
    {
        logger.LogWarning("Falha no login para o email: {Email}", dto.Email);
        return Results.Unauthorized();
    }

    var token = jwtService.GenerateToken(user);
    
    // Event Sourcing
    var userLoggedInEvent = new DomainEvents.UserLoggedInEvent(user.Id, user.Email, DateTime.UtcNow);
    await eventStore.SaveEventAsync(userLoggedInEvent);

    logger.LogInformation("Login realizado com sucesso para usuário: {UserId}", user.Id);

    return Results.Ok(new LoginResponseDto(token, new UserResponseDto(
        user.Id, user.Name, user.Email, user.Role.ToString(),
        user.CreatedAt, user.UpdatedAt)));
})
.AllowAnonymous()
.WithName("Login")
.WithOpenApi();

app.MapGet("/api/users/{id}", async (Guid id, ApplicationDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Buscando usuário com ID: {UserId}", id);
    
    var user = await db.Users.FindAsync(id);
    if (user == null)
    {
        logger.LogWarning("Usuário não encontrado. ID: {UserId}", id);
        return Results.NotFound("Usuário não encontrado");
    }

    return Results.Ok(new UserResponseDto(
        user.Id, user.Name, user.Email, user.Role.ToString(),
        user.CreatedAt, user.UpdatedAt));
})
.RequireAuthorization()
.WithName("GetUser")
.WithOpenApi();

app.MapGet("/api/users", async (ApplicationDbContext db, ILogger<Program> logger) =>
{
    logger.LogInformation("Listando todos os usuários");
    
    var users = await db.Users
        .Select(u => new UserResponseDto(
            u.Id, u.Name, u.Email, u.Role.ToString(),
            u.CreatedAt, u.UpdatedAt))
        .ToListAsync();

    return Results.Ok(users);
})
.RequireAuthorization()
.WithName("GetUsers")
.WithOpenApi();

app.MapPut("/api/users/{id}", async (Guid id, UpdateUserDto dto, ApplicationDbContext db, ClaimsPrincipal user, IEventStore eventStore, ILogger<Program> logger) =>
{
    logger.LogInformation("Atualizando usuário com ID: {UserId}", id);
    
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        logger.LogWarning("Token inválido ou sem identificador de usuário");
        return Results.Unauthorized();
    }
    
    if (id != userId && user.FindFirst(ClaimTypes.Role)?.Value != UserRole.Admin.ToString())
    {
        logger.LogWarning("Tentativa de atualizar usuário sem permissão. ID: {UserId}", id);
        return Results.Forbid();
    }

    var userEntity = await db.Users.FindAsync(id);
    if (userEntity == null)
    {
        logger.LogWarning("Usuário não encontrado para atualização. ID: {UserId}", id);
        return Results.NotFound("Usuário não encontrado");
    }

    userEntity.Update(dto.Name, dto.Email);
    await db.SaveChangesAsync();

    // Event Sourcing
    var userUpdatedEvent = new DomainEvents.UserUpdatedEvent(
        userEntity.Id,
        userEntity.Name,
        userEntity.Email,
        userEntity.UpdatedAt ?? DateTime.UtcNow
    );
    await eventStore.SaveEventAsync(userUpdatedEvent);

    logger.LogInformation("Usuário atualizado com sucesso. ID: {UserId}", id);

    return Results.Ok(new UserResponseDto(
        userEntity.Id, userEntity.Name, userEntity.Email, userEntity.Role.ToString(),
        userEntity.CreatedAt, userEntity.UpdatedAt));
})
.RequireAuthorization()
.WithName("UpdateUser")
.WithOpenApi();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", service = "usuarios", timestamp = DateTime.UtcNow }))
.AllowAnonymous()
.WithName("HealthCheck")
.WithOpenApi();

app.MapGet("/api/events/{aggregateId}", async (Guid aggregateId, IEventStore eventStore, ILogger<Program> logger) =>
{
    logger.LogInformation("Buscando eventos para aggregate ID: {AggregateId}", aggregateId);
    
    var events = await eventStore.GetEventsAsync(aggregateId);
    return Results.Ok(events);
})
.RequireAuthorization()
.WithName("GetEvents")
.WithOpenApi();

Log.Information("Iniciando FIAP.CloudGames.Usuario.API...");

app.Urls.Add("http://*:8080");

app.Run();
