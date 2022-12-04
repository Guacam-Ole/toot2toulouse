using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<ITootConfiguration, TootConfiguration>();
builder.Services.AddScoped<ITwitter, Twitter>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



