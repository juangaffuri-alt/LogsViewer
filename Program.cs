using Bpn.Authentication.OpenIdConnect;
using LogsViewer.Infrastructure.App;
using LogsViewer.Infrastructure.Clef;
using LogsViewer.Infrastructure.HostedServices;
using LogsViewer.Services;
using LogsViewer.Services.Contracts;
using LogsViewer.Services.Implementation;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Serilog;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddControllersWithViews();

IFeatureManagement featureManagement = builder.AddFeatureManagement();

IBackendConfigurator backendConfigurator = BackendConfiguratorFactory.Create(builder.Configuration, featureManagement);
backendConfigurator.GlobalSetup();
backendConfigurator.ConfigureServices(builder);

builder.Services.AddHostedService<StartupJobHostingService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOauthBPNAuthentication(builder.Configuration);
builder.Services.AddScoped<LogFilterPreferencesService>();
builder.Services.AddScoped<ILogService, LogService>();

var app = builder.Build();

var logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseEventMiddleware();
app.UseRouting();

app.UseAuthentication();  
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

static Task OnAuthenticationFailedHandler(AuthenticationFailedContext context)
{
    context.Response.Redirect("/Error/Unauthorized");
    context.HandleResponse();
    return Task.CompletedTask;
}

static Task OnSignedOutCallbackRedirectHandler(
    RemoteSignOutContext context)
{
    context.Response.Redirect("/Home/Index");
    context.HandleResponse();
    return Task.CompletedTask;
}