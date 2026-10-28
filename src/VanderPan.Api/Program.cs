using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VanderPan.Api.Data;
using VanderPan.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// Configuração do DbContext com suporte a MySQL e fallback SQLite
var provider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";

builder.Services.AddDbContext<VanderPanDbContext>(options =>
{
    if (provider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
    {
        var mySqlConn = builder.Configuration.GetConnectionString("MySql");
        options.UseMySql(mySqlConn, ServerVersion.AutoDetect(mySqlConn));
    }
    else
    {
        var sqliteConn = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=vanderpan.db";
        options.UseSqlite(sqliteConn);
    }
});

// JSON e Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSingleton<SessionTokenStore>();
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>("Bearer", _ => { });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// OpenAPI
builder.Services.AddOpenApi();

// CORS para Blazor Web e MAUI App
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Inicialização do Banco de Dados e Carga Inicial (Seed Data)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<VanderPanDbContext>();
        DbInitializer.Initialize(context);
        app.Logger.LogInformation("Banco de dados VanderPan inicializado com sucesso.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Erro ao inicializar o banco de dados VanderPan.");
    }
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (IWebHostEnvironment environment) => Results.Ok(new
{
    api = "VanderPan",
    status = "online",
    openApi = environment.IsDevelopment() ? "/openapi/v1.json" : null
})).AllowAnonymous();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapControllers();

app.Run();
