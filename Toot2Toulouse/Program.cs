using Newtonsoft.Json;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<ConfigReader>();
builder.Services.AddScoped<ITwitter, Twitter>();
builder.Services.AddScoped<IMastodon, Mastodon>();
builder.Services.AddScoped<IToulouse, Toulouse>();
builder.Services.AddScoped<INotification, Notification>();

var app = builder.Build();



// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



