using dnd_helper.Application.DependencyInjection;
using dnd_helper.Infrastructure.DependencyInjection;
using dnd_helper.Infrastructure.Seeding;
using dnd_helper.Presentation.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPresentation()
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        await initializer.InitializeAsync();
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Database initialization failed.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
app.MapApiEndpoints();

app.Run();
