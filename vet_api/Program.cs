
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using vet_api.Configurate;
using vet.Infrastructure.Configurate;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler(options =>
{
    options.ExceptionHandler = async context =>
    {
        var exception = context.Features
            .Get<IExceptionHandlerFeature>()?.Error;

        var statusCode = exception switch
        {
            DbUpdateException dbException
                when dbException.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.ExclusionViolation,
                    ConstraintName: "appointments_no_overlap"
                } => StatusCodes.Status409Conflict,

            ArgumentException => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;

        var message = statusCode switch
        {
            StatusCodes.Status409Conflict =>
                "El veterinario ya tiene una cita en ese horario.",

            StatusCodes.Status400BadRequest =>
                exception?.Message ?? "Solicitud inválida.",

            _ => "Ocurrió un error interno del servidor."
        };

        await Results.Problem(
            statusCode: statusCode,
            title: statusCode switch
            {
                400 => "Solicitud inválida",
                409 => "Conflicto de horario",
                _ => "Error interno"
            },
            detail: message
        ).ExecuteAsync(context);
    };
});


// =============================================
// 1. CONTROLLERS
// =============================================

builder.Services.AddControllers();

// =============================================
// 2. APPLICATION
// =============================================

builder.Services.AddApplicationServices();

// =============================================
// 3. INFRASTRUCTURE
// =============================================

builder.Services.AddInfrastructureServices(
    builder.Configuration);

// =============================================
// 4. AUTENTICACIÓN JWT
// =============================================

var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException(
        "No se encontró Jwt:SecretKey.");

if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "La clave JWT debe tener al menos 32 bytes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],

                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

// =============================================
// 5. SWAGGER + JWT BEARER
// =============================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VetCare API",
        Version = "v1",
        Description =
            "API REST para la gestión del sistema veterinario VetCare."
    });

    // Definir autenticación JWT para Swagger
    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "Introduce tu token JWT generado en /api/auth/login."
        });

    // Aplicar el esquema Bearer a las operaciones
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "Bearer", document)] = []
        });
});

// =============================================
// 6. CORS
// =============================================

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
// 7. BUILD
// =============================================

var app = builder.Build();


app.UseExceptionHandler();

// =============================================
// 8. MIDDLEWARE
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

app.UseAuthentication();
app.UseAuthorization();

// =============================================
// 9. CONTROLLERS
// =============================================

app.MapControllers();

// =============================================
// 10. RUN
// =============================================

app.Run();
