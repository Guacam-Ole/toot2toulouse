using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<Toot2Toulouse.Backend.Interfaces.IConfig, Toot2Toulouse.Backend.Config>();
builder.Services.AddScoped<ITwitter, Twitter>();
builder.Services.AddScoped<App>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



