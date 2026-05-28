using dnd_helper.Features.Auth;
using dnd_helper.Features.Characters;
using dnd_helper.Features.Equipment;
using dnd_helper.Features.Monsters;
using dnd_helper.Features.Rooms;
using dnd_helper.Features.Rules;
using dnd_helper.Infrastructure.Persistence.Mongo;
using dnd_helper.Infrastructure.Persistence.Postgres;
using dnd_helper.Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
    return new MongoClient(options.ConnectionString);
});
builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(options.DatabaseName);
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddScoped<IRulesCatalogRepository, MongoRulesCatalogRepository>();
builder.Services.AddScoped<RuleResolutionService>();
builder.Services.AddScoped<CharacterCreationService>();
builder.Services.AddScoped<RulesDatabaseSeeder>();
builder.Services.AddScoped<ApplicationDatabaseSeeder>();
builder.Services.AddScoped<DatabaseInitializer>();

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
app.MapAuthEndpoints();
app.MapCharacterEndpoints();
app.MapRoomEndpoints();
app.MapRuleEndpoints();
app.MapEquipmentEndpoints();
app.MapMonstersEndpoints();
app.MapLegacyReferenceEndpoints();

app.Run();
