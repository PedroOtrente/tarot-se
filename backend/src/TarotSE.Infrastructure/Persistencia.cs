using Microsoft.EntityFrameworkCore;
using TarotSE.Application;
using TarotSE.Domain.Entidades;

namespace TarotSE.Infrastructure;

public class TarotDbContext(DbContextOptions<TarotDbContext> options) : DbContext(options)
{
    public DbSet<Ambito> Ambitos => Set<Ambito>();
    public DbSet<Posicao> Posicoes => Set<Posicao>();
    public DbSet<TipoTiragem> TiposTiragem => Set<TipoTiragem>();
    public DbSet<Carta> Cartas => Set<Carta>();
    public DbSet<CategoriaResultado> Categorias => Set<CategoriaResultado>();
    public DbSet<Significado> Significados => Set<Significado>();
    public DbSet<Regra> Regras => Set<Regra>();
    public DbSet<Tiragem> Tiragens => Set<Tiragem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Tabelas no singular, como no script database/01_schema.sql (colunas em snake_case pela convenção).
        b.Entity<Ambito>().ToTable("ambito").Property(x => x.Id).ValueGeneratedNever();
        b.Entity<Posicao>().ToTable("posicao").Property(x => x.Id).ValueGeneratedNever();
        b.Entity<Fato>(e => { e.ToTable("fato"); e.HasKey(x => x.Codigo); });
        b.Entity<Carta>(e =>
        {
            e.ToTable("carta");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasOne(x => x.CategoriaSugerida).WithMany().HasForeignKey(x => x.CategoriaSugeridaId);
            e.HasOne(x => x.CategoriaSeFalso).WithMany().HasForeignKey(x => x.CategoriaSeFalsoId);
            e.HasOne(x => x.Fato).WithMany().HasForeignKey(x => x.FatoCodigo);
        });
        b.Entity<CategoriaResultado>().ToTable("categoria_resultado").Property(x => x.Id).ValueGeneratedNever();

        b.Entity<TipoTiragem>(e =>
        {
            e.ToTable("tipo_tiragem");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasMany(x => x.Posicoes).WithOne().HasForeignKey(x => x.TipoTiragemId);
        });
        b.Entity<TipoTiragemPosicao>(e =>
        {
            e.ToTable("tipo_tiragem_posicao");
            e.HasKey(x => new { x.TipoTiragemId, x.PosicaoId });
        });

        b.Entity<Significado>(e =>
        {
            e.ToTable("significado");
            e.HasOne(x => x.CategoriaSugerida).WithMany().HasForeignKey(x => x.CategoriaSugeridaId);
        });
        b.Entity<Regra>(e =>
        {
            e.ToTable("regra");
            e.Property(x => x.CondicaoJson).HasColumnType("json");
            e.Property(x => x.Tipo).HasConversion<string>();
        });

        b.Entity<Tiragem>(e =>
        {
            e.ToTable("tiragem");
            e.Property(x => x.FatosJson).HasColumnType("json");
            e.HasMany(x => x.Cartas).WithOne().HasForeignKey(x => x.TiragemId);
            e.HasOne(x => x.Resultado).WithOne().HasForeignKey<ResultadoTiragem>(x => x.TiragemId);
        });
        b.Entity<TiragemCarta>(e =>
        {
            e.ToTable("tiragem_carta");
            e.HasOne(x => x.CategoriaAplicada).WithMany().HasForeignKey(x => x.CategoriaAplicadaId);
        });
        b.Entity<ResultadoTiragem>().ToTable("resultado_tiragem");
    }
}

public class CatalogoRepositorio(TarotDbContext db) : ICatalogoRepositorio
{
    public Task<List<Ambito>> ListarAmbitosAsync() => db.Ambitos.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
    public Task<List<Posicao>> ListarPosicoesAsync() => db.Posicoes.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
    public Task<List<TipoTiragem>> ListarTiposTiragemAsync() =>
        db.TiposTiragem.AsNoTracking().Include(t => t.Posicoes).ThenInclude(p => p.Posicao).OrderBy(x => x.Id).ToListAsync();
    public Task<List<Carta>> ListarCartasAsync() => db.Cartas.AsNoTracking()
        .Include(c => c.CategoriaSugerida).Include(c => c.CategoriaSeFalso).Include(c => c.Fato)
        .OrderBy(x => x.NumeroArcano).ToListAsync();
    public Task<List<CategoriaResultado>> ListarCategoriasAsync() => db.Categorias.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
}

public class BaseConhecimentoRepositorio(TarotDbContext db) : IBaseConhecimentoRepositorio
{
    public Task<List<Significado>> ListarSignificadosAsync() =>
        db.Significados.AsNoTracking()
            .Include(s => s.Carta).ThenInclude(c => c.CategoriaSugerida)
            .Include(s => s.Ambito).Include(s => s.Posicao).Include(s => s.CategoriaSugerida)
            .OrderBy(s => s.Id).ToListAsync();

    public Task<List<Regra>> ListarRegrasAsync() =>
        db.Regras.AsNoTracking().Include(r => r.CategoriaResultado).OrderBy(r => r.Prioridade).ToListAsync();
}

public class TiragemRepositorio(TarotDbContext db) : ITiragemRepositorio
{
    public async Task AdicionarAsync(Tiragem tiragem)
    {
        db.Tiragens.Add(tiragem);
        await db.SaveChangesAsync();
    }

    private IQueryable<Tiragem> Completa() => db.Tiragens.AsNoTracking()
        .Include(t => t.Ambito)
        .Include(t => t.TipoTiragem).ThenInclude(tt => tt.Posicoes).ThenInclude(p => p.Posicao)
        .Include(t => t.Cartas).ThenInclude(c => c.Carta)
        .Include(t => t.Cartas).ThenInclude(c => c.Posicao)
        .Include(t => t.Cartas).ThenInclude(c => c.Significado)
        .Include(t => t.Cartas).ThenInclude(c => c.CategoriaAplicada)
        .Include(t => t.Resultado!).ThenInclude(r => r.CategoriaResultado)
        .Include(t => t.Resultado!).ThenInclude(r => r.Regra)
        .AsSplitQuery();

    public Task<Tiragem?> ObterAsync(int id) => Completa().FirstOrDefaultAsync(t => t.Id == id);
    public Task<List<Tiragem>> ListarRecentesAsync(int limite) => Completa().OrderByDescending(t => t.Id).Take(limite).ToListAsync();
}
