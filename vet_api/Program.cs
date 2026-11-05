
using MediatR;
using Microsoft.OpenApi;
using vet.Infrastructure.Configuration;
using vet.Application.UseCases.User.Querys;
using vet.Domain.DTOs;
using vet.Infrastructure.Persistence.Seeders;


var builder = WebApplication.CreateBuilder(args);

// =============================================
// 1. CONTROLLERS
// =============================================

builder.Services.AddControllers();

// =============================================
// 2. APPLICATION
// CQRS + MediatR + Handlers
// =============================================

builder.Services.AddApplicationServices();

// =============================================
// 3. INFRASTRUCTURE
// PostgreSQL + EF Core + Repositories + UnitOfWork
// =============================================

builder.Services.AddInfrastructureServices(
    builder.Configuration);

// =============================================
// 4. SWAGGER
// =============================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VetCare API",
        Version = "v1",
        Description = "API REST para la gestión del sistema veterinario VetCare."
    });
});

// =============================================
// 5. CORS
// =============================================

// Configuración para desarrollo local.
// Restringiremos los orígenes cuando conectemos el frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// =============================================
// 6. BUILD APPLICATION
// =============================================

var app = builder.Build();

// =============================================
// 7. MIDDLEWARE
// =============================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "VetCare API v1");

        options.RoutePrefix = "swagger";
        options.DocumentTitle = "VetCare - Swagger";
    });

    app.UseCors("DevelopmentCors");
}

app.UseHttpsRedirection();

// Cuando implementemos JWT:
// app.UseAuthentication();
// app.UseAuthorization();

// =============================================
// 8. CONTROLLERS
// =============================================

app.MapControllers();

// =============================================
// 9. RUN
// =============================================

await SuperAdminSeeder.SeedAsync(app.Services);
app.Run();
