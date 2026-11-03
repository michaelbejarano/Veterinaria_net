using Microsoft.EntityFrameworkCore;
using vet.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<VetCareContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("VetCareConnection")
    )
);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();