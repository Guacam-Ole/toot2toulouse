using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<ConfigReader>();
builder.Services.AddScoped<ITwitter, Twitter>();
builder.Services.AddScoped<IMastodon, Mastodon>();
builder.Services.AddScoped<IToulouse, Toulouse>();
builder.Services.AddScoped<INotification, Notification>();
builder.Services.AddScoped<IToot, Toot>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();