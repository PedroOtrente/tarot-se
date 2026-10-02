using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TarotSE.Application;
using TarotSE.Domain.Motor;
using TarotSE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("TarotSE")
    ?? throw new InvalidOperationException("ConnectionStrings:TarotSE não configurada.");
builder.Services.AddDbContext<TarotDbContext>(o => o
    .UseMySql(conn, new MySqlServerVersion(new Version(8, 0, 36)))
    .UseSnakeCaseNamingConvention());

// Repository
builder.Services.AddScoped<ICatalogoRepositorio, CatalogoRepositorio>();
builder.Services.AddScoped<IBaseConhecimentoRepositorio, BaseConhecimentoRepositorio>();
builder.Services.AddScoped<ITiragemRepositorio, TiragemRepositorio>();
// Service + Domínio
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<TiragemService>();
builder.Services.AddScoped<RegrasService>();
builder.Services.AddSingleton<MotorInferencia>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Sistema Especialista — Tarô", Version = "v1" }));
builder.Services.AddProblemDetails();
// Aceita o front-end rodando em qualquer porta da própria máquina (4200, 4201...) e as origens extras do appsettings.
var origensExtras = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .SetIsOriginAllowed(origem => origensExtras.Contains(origem)
        || (Uri.TryCreate(origem, UriKind.Absolute, out var u) && (u.Host == "localhost" || u.Host == "127.0.0.1")))
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Converte exceções de negócio em respostas HTTP (400/404/500) no formato ProblemDetails.
app.UseExceptionHandler(e => e.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var problemaBanco = DiagnosticoBanco.Explicar(ex);
    var (status, titulo) = ex switch
    {
        EntradaInvalidaException => (400, "Entrada inválida"),
        NaoEncontradoException => (404, "Não encontrado"),
        BaseConhecimentoInvalidaException => (500, "Base de conhecimento inconsistente"),
        _ when problemaBanco is not null => (500, "Problema no banco de dados"),
        _ => (500, "Erro interno"),
    };
    // 400/404 são respostas normais (entrada inválida): registra em uma linha. 500 registra o erro completo.
    var log = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("TarotSE");
    if (problemaBanco is not null) log.LogError("{Rota}: {Problema} (erro original: {Erro})", ctx.Request.Path, problemaBanco, ex?.Message);
    else if (status == 500) log.LogError(ex, "Erro ao processar {Rota}", ctx.Request.Path);
    else log.LogInformation("{Status} {Rota}: {Mensagem}", status, ctx.Request.Path, ex?.Message);
    ctx.Response.StatusCode = status;
    await ctx.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = status, Title = titulo,
        Detail = problemaBanco
            ?? (status == 500 && ex is not BaseConhecimentoInvalidaException ? "Erro inesperado no servidor." : ex?.Message),
    });
}));

// Na subida, confere se o banco existe e está na versão desta API — e avisa em português se não estiver.
await DiagnosticoBanco.VerificarAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();
app.Run();

public partial class Program;

/// <summary>Traduz os erros mais comuns de banco em instruções de como resolver.</summary>
static class DiagnosticoBanco
{
    const string Scripts = "rode database/01_schema.sql, 02_seed.sql e 03_usuario.sql de novo " +
                           "(no Docker: docker compose down -v e depois docker compose up -d --build)";

    public static string? Explicar(Exception? ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is not System.Data.Common.DbException && e.GetType().Name != "MySqlException") continue;
            var m = e.Message;
            if (m.Contains("doesn't exist") || m.Contains("Unknown column"))
                return $"O banco está desatualizado em relação a esta versão do sistema: {Scripts}.";
            if (m.Contains("Unknown database"))
                return $"O banco tarot_se não existe: {Scripts}.";
            if (m.Contains("Access denied"))
                return "O MySQL recusou o usuário tarot: rode database/03_usuario.sql.";
            if (m.Contains("Unable to connect") || m.Contains("Connect Timeout"))
                return "Não consegui conectar no MySQL. Confira se ele está ligado e se o host/porta em appsettings.json estão certos.";
            return $"Erro do MySQL: {m}";
        }
        return null;
    }

    public static async Task VerificarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TarotDbContext>();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TarotSE");
        // No Docker o MySQL pode demorar alguns segundos para aceitar conexões: tenta por até ~60 s.
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                var regras = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM regra WHERE codigo_regra = 'R56'").SingleAsync();
                await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM fato").SingleAsync();
                if (regras == 1) { log.LogInformation("Banco OK: base de regras R01–R56 carregada."); return; }
                log.LogError("ATENÇÃO: o banco não tem a base de regras desta versão. {Acao}", "Rode os 3 scripts de database/ de novo.");
                return;
            }
            catch (Exception ex)
            {
                var explicacao = Explicar(ex) ?? ex.Message;
                if (explicacao.StartsWith("Não consegui conectar") && tentativa < 10)
                {
                    await Task.Delay(3000);
                    continue;
                }
                log.LogError("ATENÇÃO: {Explicacao}", explicacao);
                return;
            }
        }
    }
}
