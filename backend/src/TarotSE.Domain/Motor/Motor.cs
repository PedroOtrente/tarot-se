using System.Text.Json.Nodes;
using TarotSE.Domain.Entidades;

namespace TarotSE.Domain.Motor;

/// <summary>Violação de regra de negócio na entrada (vira HTTP 400).</summary>
public class EntradaInvalidaException(string mensagem) : Exception(mensagem);

/// <summary>Erro na própria base de conhecimento (ex.: condicao_json malformada).</summary>
public class BaseConhecimentoInvalidaException(string mensagem) : Exception(mensagem);

/// <summary>Snapshot da base de conhecimento carregada do banco.</summary>
public sealed class BaseConhecimento
{
    public const string CodigoFallback = "R26";
    public IReadOnlyList<Significado> Significados { get; }
    /// <summary>Regras que decidem a categoria (COMPOSTA e CONSOLIDACAO), em ordem de prioridade.</summary>
    public IReadOnlyList<Regra> RegrasDeDecisao { get; }
    /// <summary>Regras que só acrescentam texto à leitura (OBSERVACAO).</summary>
    public IReadOnlyList<Regra> Observacoes { get; }

    public BaseConhecimento(IEnumerable<Significado> significados, IEnumerable<Regra> regras)
    {
        Significados = significados.ToList();
        var todas = regras.ToList();
        RegrasDeDecisao = todas.Where(r => r.DecideCategoria).OrderBy(r => r.Prioridade).ToList();
        Observacoes = todas.Where(r => !r.DecideCategoria).OrderBy(r => r.CodigoRegra.Length).ThenBy(r => r.CodigoRegra).ToList();

        var empates = Significados.GroupBy(s => (s.CartaId, s.AmbitoId, s.PosicaoId)).Where(g => g.Count() > 1).ToList();
        if (empates.Count > 0)
            throw new BaseConhecimentoInvalidaException("Significados repetidos para a mesma carta/âmbito/posição.");
        if (RegrasDeDecisao.Any(r => r.Prioridade is null))
            throw new BaseConhecimentoInvalidaException("Regra de decisão sem prioridade.");
        if (RegrasDeDecisao.Select(r => r.Prioridade).Distinct().Count() != RegrasDeDecisao.Count)
            throw new BaseConhecimentoInvalidaException("Duas regras de decisão têm a mesma prioridade.");
    }
}

public sealed record CartaNaPosicao(Posicao Posicao, Carta Carta);

public sealed record InterpretacaoCarta(
    Posicao Posicao, Carta Carta, Significado? Significado, string Texto, CategoriaResultado Categoria, string? FatoUsado)
{
    public string CodigoRegra => Significado?.CodigoRegra ?? BaseConhecimento.CodigoFallback;
    public bool Fallback => Significado is null;
}

/// <summary>Uma combinação de cartas reconhecida na tiragem (regra composta ou observação).</summary>
public sealed record CombinacaoIdentificada(string CodigoRegra, string Texto, string? Categoria, bool DefiniuResultado);

/// <summary>Uma linha do rastro de explicação ("como o sistema chegou a esta conclusão").</summary>
public sealed record PassoRastro(string CodigoRegra, int Prioridade, bool Disparou, string Observacao);

public sealed record ResultadoInferencia(
    IReadOnlyList<InterpretacaoCarta> Detalhes,
    Regra RegraAplicada,
    CategoriaResultado Categoria,
    string? Complemento,
    IReadOnlyList<CombinacaoIdentificada> Combinacoes,
    string TextoFinal,
    IReadOnlyList<PassoRastro> Rastro);

/// <summary>
/// Motor de inferência por encadeamento para frente.
/// Camada 3: cada carta recebe o significado MAIS ESPECÍFICO (ou o fallback R26).
/// Camadas 4–5: todas as regras de combinação/consolidação são avaliadas; as verdadeiras formam o
/// conjunto de conflito e vence a de maior PRIORIDADE (1 = mais alta).
/// </summary>
public sealed class MotorInferencia
{
    public ResultadoInferencia Inferir(BaseConhecimento bc, Ambito ambito, TipoTiragem tipo,
        IReadOnlyList<CartaNaPosicao> cartas, IReadOnlyDictionary<string, bool>? fatos = null)
    {
        fatos ??= new Dictionary<string, bool>();
        Validar(tipo, cartas, fatos);

        var ordem = tipo.PosicoesOrdenadas().Select(p => p.Id).ToList();
        var detalhes = cartas
            .OrderBy(c => ordem.IndexOf(c.Posicao.Id))
            .Select(c => InterpretarCarta(bc, ambito, c, fatos))
            .ToList();

        // Observações: só acrescentam texto.
        var combinacoes = new List<CombinacaoIdentificada>();
        foreach (var obs in bc.Observacoes)
            if (Avaliar(Condicao(obs), detalhes, obs.CodigoRegra))
                combinacoes.Add(new(obs.CodigoRegra, obs.Texto, null, false));

        // Regras de decisão: conjunto de conflito + resolução por prioridade.
        var rastro = new List<PassoRastro>();
        (Regra Regra, CategoriaResultado Categoria, string? Nuance)? vencedora = null;
        foreach (var regra in bc.RegrasDeDecisao)
        {
            var prioridade = regra.Prioridade!.Value;
            if (!Avaliar(Condicao(regra), detalhes, regra.CodigoRegra))
            {
                rastro.Add(new(regra.CodigoRegra, prioridade, false, "Condição falsa."));
                continue;
            }

            var (categoria, nuance) = CategoriaDa(regra, detalhes);
            if (categoria is null)
            {
                rastro.Add(new(regra.CodigoRegra, prioridade, false, "Condição verdadeira, mas não há categoria a herdar."));
                continue;
            }

            if (regra.Tipo == TipoRegra.COMPOSTA)
                combinacoes.Add(new(regra.CodigoRegra, regra.Texto, categoria.Nome, vencedora is null));

            if (vencedora is null)
            {
                vencedora = (regra, categoria, nuance);
                rastro.Add(new(regra.CodigoRegra, prioridade, true, $"Disparou → {categoria.Nome} (maior prioridade no conjunto de conflito)."));
            }
            else
            {
                rastro.Add(new(regra.CodigoRegra, prioridade, false,
                    $"Também verdadeira ({categoria.Nome}), mas descartada: {vencedora.Value.Regra.CodigoRegra} tem prioridade maior."));
            }
        }

        if (vencedora is not { } v)
            throw new BaseConhecimentoInvalidaException("Nenhuma regra de decisão disparou: falta a regra padrão (condição {\"sempre\":true}).");

        var complemento = v.Regra.TextoComplemento ?? v.Nuance;
        var ordenadas = combinacoes.OrderBy(c => int.Parse(c.CodigoRegra[1..])).ToList();
        var texto = MontarTexto(ambito, detalhes, ordenadas, v.Categoria, complemento);
        return new ResultadoInferencia(detalhes, v.Regra, v.Categoria, complemento, ordenadas, texto, rastro);
    }

    private static void Validar(TipoTiragem tipo, IReadOnlyList<CartaNaPosicao> cartas, IReadOnlyDictionary<string, bool> fatos)
    {
        var exigidas = tipo.PosicoesOrdenadas().Select(p => p.Id).OrderBy(i => i).ToList();
        var informadas = cartas.Select(c => c.Posicao.Id).OrderBy(i => i).ToList();
        if (!exigidas.SequenceEqual(informadas))
        {
            var nomes = string.Join(", ", tipo.PosicoesOrdenadas().Select(p => p.Nome));
            throw new EntradaInvalidaException($"A tiragem \"{tipo.Nome}\" ({tipo.CodigoRegra}) exige exatamente as posições: {nomes}.");
        }
        if (cartas.Select(c => c.Carta.Id).Distinct().Count() != cartas.Count)
            throw new EntradaInvalidaException("A mesma carta não pode aparecer duas vezes na tiragem.");

        foreach (var c in cartas.Where(c => c.Carta.FatoCodigo is not null))
            if (!fatos.ContainsKey(c.Carta.FatoCodigo!))
                throw new EntradaInvalidaException(
                    $"A carta \"{c.Carta.Nome}\" depende de uma resposta sua: {c.Carta.Fato?.Pergunta ?? c.Carta.FatoCodigo}");
    }

    /// <summary>Categoria sugerida da carta, resolvendo o fato quando houver (ex.: A Justiça).</summary>
    private static (CategoriaResultado Categoria, string? FatoUsado) CategoriaDaCarta(Carta carta, IReadOnlyDictionary<string, bool> fatos)
    {
        if (carta.FatoCodigo is null) return (carta.CategoriaSugerida, null);
        var verdadeiro = fatos[carta.FatoCodigo];
        var cat = verdadeiro ? carta.CategoriaSugerida : carta.CategoriaSeFalso
            ?? throw new BaseConhecimentoInvalidaException($"{carta.Nome}: falta categoria_se_falso.");
        return (cat, $"{carta.FatoCodigo} = {(verdadeiro ? "sim" : "não")}");
    }

    private static InterpretacaoCarta InterpretarCarta(BaseConhecimento bc, Ambito ambito, CartaNaPosicao c, IReadOnlyDictionary<string, bool> fatos)
    {
        var escolhido = bc.Significados
            .Where(s => s.AplicaSe(c.Carta.Id, ambito.Id, c.Posicao.Id))
            .OrderByDescending(s => s.Especificidade)
            .FirstOrDefault();

        var (catCarta, fato) = CategoriaDaCarta(c.Carta, fatos);
        return escolhido is null
            ? new InterpretacaoCarta(c.Posicao, c.Carta, null, c.Carta.DescricaoGeral, catCarta, fato)   // R26 — ficha resumida
            : new InterpretacaoCarta(c.Posicao, c.Carta, escolhido, escolhido.Texto, escolhido.CategoriaSugerida ?? catCarta, fato);
    }

    private static (CategoriaResultado?, string?) CategoriaDa(Regra regra, IReadOnlyList<InterpretacaoCarta> detalhes)
    {
        if (regra.CategoriaResultado is not null) return (regra.CategoriaResultado, null);
        var origem = detalhes.FirstOrDefault(d => d.Carta.Nome == regra.HerdaCategoriaDe || d.Posicao.Nome == regra.HerdaCategoriaDe);
        return (origem?.Categoria, origem?.Carta.NuanceCategoria);
    }

    private static JsonNode Condicao(Regra r)
    {
        try { return JsonNode.Parse(r.CondicaoJson) ?? throw new FormatException(); }
        catch (Exception) { throw new BaseConhecimentoInvalidaException($"{r.CodigoRegra}: condicao_json inválida."); }
    }

    // ------------------------------------------------------------------
    // Avaliador da gramática de condicao_json:
    //   {"todas":[...]}  {"alguma":[...]}  {"sempre":true}
    //   {"posicao":[...]?, "carta_em":[...]}       existe carta (nessas posições) com esse nome
    //   {"posicao":[...]?, "categoria_em":[...]}   existe carta (nessas posições) com essa categoria
    //   sem "posicao" = qualquer posição da tiragem
    // ------------------------------------------------------------------
    private static bool Avaliar(JsonNode no, IReadOnlyList<InterpretacaoCarta> fatos, string codigo)
    {
        if (no is not JsonObject obj)
            throw new BaseConhecimentoInvalidaException($"{codigo}: cada condição deve ser um objeto JSON.");

        if (obj["sempre"] is JsonNode sempre) return sempre.GetValue<bool>();
        if (obj["todas"] is JsonArray todas) return todas.All(n => Avaliar(n!, fatos, codigo));
        if (obj["alguma"] is JsonArray alguma) return alguma.Any(n => Avaliar(n!, fatos, codigo));

        var alvo = obj["posicao"] is JsonArray posicoes
            ? fatos.Where(f => posicoes.Select(p => p!.GetValue<string>()).Contains(f.Posicao.Nome)).ToList()
            : fatos.ToList();

        if (obj["carta_em"] is JsonArray cartas)
        {
            var nomes = cartas.Select(x => x!.GetValue<string>()).ToHashSet();
            return alvo.Any(f => nomes.Contains(f.Carta.Nome));
        }
        if (obj["categoria_em"] is JsonArray cats)
        {
            var codigos = cats.Select(x => x!.GetValue<string>()).ToHashSet();
            return alvo.Any(f => codigos.Contains(f.Categoria.Codigo));
        }
        throw new BaseConhecimentoInvalidaException($"{codigo}: condição não reconhecida: {obj.ToJsonString()}");
    }

    private static string MontarTexto(Ambito ambito, IReadOnlyList<InterpretacaoCarta> detalhes,
        IReadOnlyList<CombinacaoIdentificada> combinacoes, CategoriaResultado cat, string? complemento)
    {
        var rotulos = new Dictionary<string, string>
        {
            ["Significador"] = "Situação atual",
            ["Movimento"] = "Tendência em curso",
            ["Resultado"] = "Desfecho provável",
        };
        var partes = detalhes.Select(d =>
            $"{rotulos.GetValueOrDefault(d.Posicao.Nome, d.Posicao.Nome)} ({d.Carta.Nome}): {d.Texto.TrimEnd('.')}.").ToList();
        foreach (var c in combinacoes)
            partes.Add($"Combinação ({c.CodigoRegra}): {c.Texto.TrimEnd('.')}.");
        partes.Add(complemento is null
            ? $"Conclusão ({ambito.Nome}): {cat.Nome}. {cat.Descricao}"
            : $"Conclusão ({ambito.Nome}): {cat.Nome} — {complemento}. {cat.Descricao}");
        return string.Join(" ", partes);
    }
}
