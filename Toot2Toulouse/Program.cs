using Autofac.Core;

using Microsoft.Extensions.FileProviders;

using Toot2Toulouse;
using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

});

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddScoped<ConfigReader>(cr=>new ConfigReader(builder.Environment.ContentRootPath+"\\Properties"));
builder.Services.AddScoped<ITwitter, Twitter>();
builder.Services.AddScoped<IMastodon, Mastodon>();
builder.Services.AddScoped<IToulouse, Toulouse>();
builder.Services.AddScoped<INotification, Notification>();
builder.Services.AddScoped<IMessage, Message>();
builder.Services.AddScoped<IDatabase, Database>(db => new Database(db.GetService<ILogger<Database>>(), db.GetService<ConfigReader>(), Path.Combine(builder.Environment.ContentRootPath, "Data")));
builder.Services.AddScoped<IUser, User>();

builder.Services.AddScoped<ICookies, Cookies>();
builder.Services.AddScoped<IMastodonClientAuthentication, MastodonClientAuthentication>();
builder.Services.AddScoped<ITwitterClientAuthentication, TwitterClientAuthentication>();


builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddFile("data/app.log", append: true);
});
var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseSession();

app.MapControllers();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "Frontend")),
    RequestPath = "/Frontend"
});

app.Run();