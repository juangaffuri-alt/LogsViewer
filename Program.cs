using LogsViewer.Infrastructure.App;
using LogsViewer.Infrastructure.Clef;
using LogsViewer.Infrastructure.HostedServices;
using LogsViewer.Services.Contracts;
using LogsViewer.Services.Implementation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

IFeatureManagement featureManagement = builder.AddFeatureManagement();

IBackendConfigurator backendConfigurator = BackendConfiguratorFactory.Create(builder.Configuration, featureManagement);
backendConfigurator.GlobalSetup();
backendConfigurator.ConfigureServices(builder);

builder.Services.AddHostedService<StartupJobHostingService>();

builder.Services.AddScoped<ILogService, LogService>();

var app = builder.Build();



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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();