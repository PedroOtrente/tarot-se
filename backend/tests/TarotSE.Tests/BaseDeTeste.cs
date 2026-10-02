using System.Text.Json.Nodes;
using TarotSE.Domain.Entidades;
using TarotSE.Domain.Motor;

namespace TarotSE.Tests;

/// <summary>
/// Monta a base de conhecimento lendo os MESMOS arquivos que geram o seed do banco
/// (tools/conhecimento/*.csv e regras.json). Assim os testes e o banco nunca divergem.
/// </summary>
public static class BaseDeTeste
{
    public static readonly Ambito Relacionamentos = new() { Id = 1, CodigoRegra = "R01", Nome = "Relacionamentos" };
    public static readonly Ambito Profissional = new() { Id = 2, CodigoRegra = "R02", Nome = "Profissional" };
    public static readonly Ambito Academico = new() { Id = 3, CodigoRegra = "R03", Nome = "Acadêmico" };
    public static readonly Ambito[] Ambitos = [Relacionamentos, Profissional, Academico];

    public static readonly Posicao Sig = new() { Id = 1, Nome = "Significador" };
    public static readonly Posicao Mov = new() { Id = 2, Nome = "Movimento" };
    public static readonly Posicao Res = new() { Id = 3, Nome = "Resultado" };

    public static readonly Dictionary<string, CategoriaResultado> Categorias = new()
    {
        ["FAVORAVEL"] = new() { Id = 1, Codigo = "FAVORAVEL", Nome = "Favorável" },
        ["CAUTELA"] = new() { Id = 2, Codigo = "CAUTELA", Nome = "Cautela / Adiar" },
        ["MUDANCA"] = new() { Id = 3, Codigo = "MUDANCA", Nome = "Mudança Necessária" },
        ["NEUTRO"] = new() { Id = 4, Codigo = "NEUTRO", Nome = "Neutro / Mais Informação" },
    };

    public static readonly TipoTiragem Uma = Tipo(1, "R27", Sig);
    public static readonly TipoTiragem Duas = Tipo(2, "R28", Sig, Mov);
    public static readonly TipoTiragem Tres = Tipo(3, "R29", Sig, Mov, Res);

    public static readonly Dictionary<string, Carta> Cartas;
    private static readonly List<Significado> _significados;
    private static readonly List<Regra> _regras;

    static BaseDeTeste()
    {
        var pasta = PastaConhecimento();
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(pasta, "regras.json")))!;
        var fatos = json["fatos"]!.AsArray().Select(f => new Fato
        {
            Codigo = f!["codigo"]!.GetValue<string>(), Pergunta = f["pergunta"]!.GetValue<string>(),
        }).ToDictionary(f => f.Codigo);

        Cartas = Csv(Path.Combine(pasta, "cartas.csv"), 9).Select(c =>
        {
            var n = int.Parse(c[0]);
            return new Carta
            {
                Id = n + 1, NumeroArcano = n, Nome = c[1], CodigoRegra = Nulo(c[2]), Aprofundada = c[3] == "sim",
                CategoriaSugerida = Categorias[c[4]], CategoriaSugeridaId = Categorias[c[4]].Id,
                FatoCodigo = Nulo(c[5]), Fato = Nulo(c[5]) is { } f ? fatos[f] : null,
                CategoriaSeFalso = Nulo(c[6]) is { } k ? Categorias[k] : null,
                NuanceCategoria = Nulo(c[7]), DescricaoGeral = c[8],
            };
        }).ToDictionary(c => c.Nome);

        var id = 0;
        var ambitos = Ambitos.ToDictionary(a => a.Nome);
        var posicoes = new[] { Sig, Mov, Res }.ToDictionary(p => p.Nome);
        _significados = Csv(Path.Combine(pasta, "significados.csv"), 4).Select(s => new Significado
        {
            Id = ++id, Carta = Cartas[s[0]], CartaId = Cartas[s[0]].Id,
            Ambito = s[1] == "qualquer" ? null : ambitos[s[1]], AmbitoId = s[1] == "qualquer" ? null : ambitos[s[1]].Id,
            Posicao = s[2] == "qualquer" ? null : posicoes[s[2]], PosicaoId = s[2] == "qualquer" ? null : posicoes[s[2]].Id,
            Texto = s[3],
        }).ToList();

        _regras = json["regras"]!.AsArray().Select((r, i) =>
        {
            var cat = r!["categoria"]?.GetValue<string>();
            return new Regra
            {
                Id = 100 + i, CodigoRegra = r["codigo"]!.GetValue<string>(),
                Tipo = Enum.Parse<TipoRegra>(r["tipo"]!.GetValue<string>()),
                Prioridade = r["prioridade"]?.GetValue<int>(),
                CondicaoJson = r["condicao"]!.ToJsonString(),
                CategoriaResultado = cat is null ? null : Categorias[cat],
                HerdaCategoriaDe = r["herda_de"]?.GetValue<string>(),
                Texto = r["texto"]!.GetValue<string>(),
                TextoComplemento = r["complemento"]?.GetValue<string>(),
            };
        }).ToList();
    }

    public static List<Significado> Significados() => _significados.ToList();
    public static List<Regra> Regras() => _regras.Select(r => new Regra
    {
        Id = r.Id, CodigoRegra = r.CodigoRegra, Tipo = r.Tipo, Prioridade = r.Prioridade, CondicaoJson = r.CondicaoJson,
        CategoriaResultado = r.CategoriaResultado, HerdaCategoriaDe = r.HerdaCategoriaDe, Texto = r.Texto, TextoComplemento = r.TextoComplemento,
    }).ToList();
    public static BaseConhecimento Base() => new(Significados(), Regras());

    public static List<CartaNaPosicao> Entrada(TipoTiragem tipo, params string[] cartas) =>
        tipo.PosicoesOrdenadas().Zip(cartas, (p, c) => new CartaNaPosicao(p, Cartas[c])).ToList();

    private static TipoTiragem Tipo(int id, string cod, params Posicao[] ps) => new()
    {
        Id = id, CodigoRegra = cod, Nome = $"{ps.Length} cartas", QtdCartas = ps.Length,
        Posicoes = ps.Select((p, i) => new TipoTiragemPosicao { TipoTiragemId = id, PosicaoId = p.Id, Ordem = i + 1, Posicao = p }).ToList()
    };

    private static string? Nulo(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>CSV com ';' — a última coluna (texto) pode conter ';'.</summary>
    private static IEnumerable<string[]> Csv(string arquivo, int colunas) =>
        File.ReadAllLines(arquivo).Skip(1).Where(l => l.Trim().Length > 0)
            .Select(l => l.Split(';', colunas).Select(x => x.Trim()).ToArray());

    private static string PastaConhecimento()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            var p = Path.Combine(d.FullName, "tools", "conhecimento");
            if (Directory.Exists(p)) return p;
        }
        throw new DirectoryNotFoundException("Pasta tools/conhecimento não encontrada.");
    }
}
