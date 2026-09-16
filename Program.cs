using LogsViewer.Services.Contracts;
using LogsViewer.Services.Implementation;
using Raven.Client.Documents;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ILogService, LogService>();

var ravenDbSettings = builder.Configuration.GetSection("RavenDb");
builder.Services.AddSingleton<IDocumentStore>(new DocumentStore
{
    Urls = ravenDbSettings["Urls"]?.Split(",") ?? new[] { "http://localhost:8080" },
    Database = ravenDbSettings["Database"]
}.Initialize());

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
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
