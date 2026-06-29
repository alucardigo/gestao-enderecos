using System.Text.Encodings.Web;
using System.Text.Unicode;
using GestaoEnderecos.Data;
using GestaoEnderecos.Models;
using GestaoEnderecos.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

// MVC + acesso ao HttpContext (necessário para o filtro global por usuário).
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Permite que acentos do português saiam como UTF-8 no HTML, em vez de entidades numéricas.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// Persistência (EF Core). Provider configurável: SQL Server (padrão) ou SQLite. O SQLite é útil
// para ambientes leves/demos (ex.: hosts ARM, onde não há imagem de SQL Server) sem mudar o código.
// Connection string via appsettings/User-Secrets/variável de ambiente.
var dbProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// Usuário atual (Scoped) + hash de senha nativo (PBKDF2 do framework) + serviços de domínio.
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<EnderecoService>();
builder.Services.AddScoped<EnderecoImportService>();
builder.Services.AddSingleton<CsvExporter>();

// Cliente HTTP tipado para o ViaCEP (pool de conexões via IHttpClientFactory + timeout curto).
builder.Services.AddHttpClient<IViaCepService, ViaCepService>(client =>
{
    client.BaseAddress = new Uri("https://viacep.com.br/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Autenticação por cookie (sem ASP.NET Core Identity).
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Em produção o cookie de sessão só trafega por HTTPS. Em dev/testes (HTTP local/TestServer)
        // segue a requisição, para o login funcionar sem HTTPS configurado.
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Páginas amigáveis para códigos de status (ex.: 404 ao acessar id inexistente/alheio).
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// A ordem importa: autenticação antes da autorização.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Enderecos}/{action=Index}/{id?}");

// Conveniência de dev/demo: cria o schema (se necessário) e popula dados de demonstração —
// exceto sob testes, que provisionam o próprio banco SQLite. Em produção, prefira aplicar o
// script DDL (db/scripts/01-create-tables.sql) ou migrações ao pipeline de implantação.
if (!app.Environment.IsEnvironment("Testing"))
{
    await InicializarBancoAsync(app);
}

app.Run();

// Cria o schema e popula os dados de demonstração, com retentativas — no Docker o SQL Server
// pode levar alguns segundos para aceitar conexões mesmo após o container subir.
static async Task InicializarBancoAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxTentativas = 12;
    for (var tentativa = 1; ; tentativa++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
            await DbSeeder.SeedAsync(db, hasher);
            return;
        }
        catch (Exception ex) when (tentativa < maxTentativas)
        {
            logger.LogWarning(ex, "Banco indisponível (tentativa {Tentativa}/{Max}); aguardando 3s…",
                tentativa, maxTentativas);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

// Exposto para os testes de integração (WebApplicationFactory).
public partial class Program;
