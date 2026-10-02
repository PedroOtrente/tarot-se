namespace TarotSE.Domain.Entidades;

/// <summary>Camada 1 — Contexto (R01–R03).</summary>
public class Ambito
{
    public int Id { get; set; }
    public string CodigoRegra { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
}

/// <summary>Camada 2 — Posição (R04–R06).</summary>
public class Posicao
{
    public int Id { get; set; }
    public string CodigoRegra { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
}

/// <summary>Camada 2 — Tipo de tiragem (R07–R09).</summary>
public class TipoTiragem
{
    public int Id { get; set; }
    public string CodigoRegra { get; set; } = "";
    public string Nome { get; set; } = "";
    public int QtdCartas { get; set; }
    public string Descricao { get; set; } = "";
    public List<TipoTiragemPosicao> Posicoes { get; set; } = new();

    public IReadOnlyList<Posicao> PosicoesOrdenadas() =>
        Posicoes.OrderBy(p => p.Ordem).Select(p => p.Posicao).ToList();
}

public class TipoTiragemPosicao
{
    public int TipoTiragemId { get; set; }
    public int PosicaoId { get; set; }
    public int Ordem { get; set; }
    public Posicao Posicao { get; set; } = null!;
}

public class Carta
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public int NumeroArcano { get; set; }
    public string Numeral { get; set; } = "";
    /// <summary>R07–R20 para os 14 arcanos aprofundados; nulo para os 8 de ficha resumida.</summary>
    public string? CodigoRegra { get; set; }
    public bool Aprofundada { get; set; }
    /// <summary>Significado geral (Seção 9.3) ou ficha resumida (Seção 9.4) — usado pelo fallback R26.</summary>
    public string DescricaoGeral { get; set; } = "";
    public int CategoriaSugeridaId { get; set; }
    public string? FatoCodigo { get; set; }
    public int? CategoriaSeFalsoId { get; set; }
    public string? NuanceCategoria { get; set; }

    public CategoriaResultado CategoriaSugerida { get; set; } = null!;
    public CategoriaResultado? CategoriaSeFalso { get; set; }
    public Fato? Fato { get; set; }
}

/// <summary>Informação que o sistema pergunta ao usuário só quando uma carta da tiragem depende dela.</summary>
public class Fato
{
    public string Codigo { get; set; } = "";
    public string Pergunta { get; set; } = "";
    public string Fonte { get; set; } = "";
}

public class CategoriaResultado
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
}

/// <summary>Camada 3 — significado da carta num âmbito/posição (R07–R20). AmbitoId/PosicaoId nulos = "qualquer".</summary>
public class Significado
{
    public int Id { get; set; }
    public int CartaId { get; set; }
    public int? AmbitoId { get; set; }
    public int? PosicaoId { get; set; }
    public string Texto { get; set; } = "";
    /// <summary>Nulo = usa a categoria sugerida da carta.</summary>
    public int? CategoriaSugeridaId { get; set; }

    public Carta Carta { get; set; } = null!;
    public Ambito? Ambito { get; set; }
    public Posicao? Posicao { get; set; }
    public CategoriaResultado? CategoriaSugerida { get; set; }

    public string CodigoRegra => Carta.CodigoRegra ?? "R??";

    /// <summary>Especificidade: carta+âmbito+posição (3) &gt; carta+âmbito (2) &gt; carta+posição (1) &gt; só carta (0).</summary>
    public int Especificidade => (AmbitoId.HasValue ? 2 : 0) + (PosicaoId.HasValue ? 1 : 0);

    public bool AplicaSe(int cartaId, int ambitoId, int posicaoId) =>
        CartaId == cartaId
        && (AmbitoId is null || AmbitoId == ambitoId)
        && (PosicaoId is null || PosicaoId == posicaoId);
}

public enum TipoRegra { COMPOSTA, OBSERVACAO, CONSOLIDACAO }

/// <summary>Camadas 4 e 5 — combinação de cartas (COMPOSTA / OBSERVACAO) ou consolidação geral.</summary>
public class Regra
{
    public int Id { get; set; }
    public string CodigoRegra { get; set; } = "";
    public TipoRegra Tipo { get; set; }
    public string Descricao { get; set; } = "";
    /// <summary>1 = mais alta. Nula para OBSERVACAO (não decide categoria).</summary>
    public int? Prioridade { get; set; }
    public string CondicaoJson { get; set; } = "";
    public int? CategoriaResultadoId { get; set; }
    /// <summary>Quando a categoria é nula: nome da carta ou da posição de onde herdar.</summary>
    public string? HerdaCategoriaDe { get; set; }
    public string Texto { get; set; } = "";
    public string? TextoComplemento { get; set; }
    public string Fonte { get; set; } = "";
    public CategoriaResultado? CategoriaResultado { get; set; }

    public bool DecideCategoria => Tipo != TipoRegra.OBSERVACAO;
}

public class Tiragem
{
    public int Id { get; set; }
    public int AmbitoId { get; set; }
    public int TipoTiragemId { get; set; }
    public DateTime DataHora { get; set; }
    public string? FatosJson { get; set; }
    public string? Anotacao { get; set; }
    public Ambito Ambito { get; set; } = null!;
    public TipoTiragem TipoTiragem { get; set; } = null!;
    public List<TiragemCarta> Cartas { get; set; } = new();
    public ResultadoTiragem? Resultado { get; set; }
}

public class TiragemCarta
{
    public int Id { get; set; }
    public int TiragemId { get; set; }
    public int PosicaoId { get; set; }
    public int CartaId { get; set; }
    public int? SignificadoId { get; set; }
    public int? CategoriaAplicadaId { get; set; }
    public string TextoAplicado { get; set; } = "";
    public Posicao Posicao { get; set; } = null!;
    public Carta Carta { get; set; } = null!;
    public Significado? Significado { get; set; }
    public CategoriaResultado? CategoriaAplicada { get; set; }
}

public class ResultadoTiragem
{
    public int Id { get; set; }
    public int TiragemId { get; set; }
    public int CategoriaResultadoId { get; set; }
    public int RegraId { get; set; }
    public string? Combinacoes { get; set; }
    public string TextoFinal { get; set; } = "";
    public CategoriaResultado CategoriaResultado { get; set; } = null!;
    public Regra Regra { get; set; } = null!;
}
