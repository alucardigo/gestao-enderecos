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

// Persistência (EF Core + SQL Server). Connection string via appsettings/User-Secrets/env.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Usuário atual (Scoped) + hash de senha nativo (PBKDF2 do framework) + serviços de domínio.
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddScoped<EnderecoService>();
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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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

// Cria o schema (se necessário) e popula dados de demonstração — exceto sob testes,
// que provisionam o próprio banco SQLite.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
    await DbSeeder.SeedAsync(db, hasher);
}

app.Run();

// Exposto para os testes de integração (WebApplicationFactory).
public partial class Program;
