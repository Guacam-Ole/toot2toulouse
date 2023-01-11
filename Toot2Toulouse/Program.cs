using Microsoft.Extensions.FileProviders;

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
builder.Services.AddScoped<IMessage, Message>();



var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "Frontend")),
    RequestPath = "/Frontend"
});

app.Run();



// https://localhost:7198/Frontend/index.en.html