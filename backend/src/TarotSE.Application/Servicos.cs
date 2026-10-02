using System.Text.Json;
using TarotSE.Domain.Entidades;
using TarotSE.Domain.Motor;

namespace TarotSE.Application;

public class CatalogoService(ICatalogoRepositorio repo)
{
    public async Task<List<AmbitoDto>> AmbitosAsync() =>
        (await repo.ListarAmbitosAsync()).Select(a => new AmbitoDto(a.Id, a.Nome, a.Descricao, a.CodigoRegra)).ToList();

    public async Task<List<TipoTiragemDto>> TiposTiragemAsync() =>
        (await repo.ListarTiposTiragemAsync()).Select(t => new TipoTiragemDto(t.Id, t.Nome, t.QtdCartas, t.Descricao, t.CodigoRegra,
            t.PosicoesOrdenadas().Select(p => new PosicaoDto(p.Id, p.Nome, p.Descricao)).ToList())).ToList();

    public async Task<List<CartaDto>> CartasAsync() =>
        (await repo.ListarCartasAsync()).Select(c => new CartaDto(c.Id, c.Nome, c.NumeroArcano, c.Numeral, c.CodigoRegra, c.Aprofundada,
            c.DescricaoGeral, c.CategoriaSugerida.Nome, c.Fato is null ? null : new FatoDto(c.Fato.Codigo, c.Fato.Pergunta))).ToList();
}

public class TiragemService(ICatalogoRepositorio catalogo, IBaseConhecimentoRepositorio baseRepo, ITiragemRepositorio tiragens, MotorInferencia motor)
{
    public async Task<TiragemResponse> RealizarAsync(CriarTiragemRequest req)
    {
        if (req.Cartas is null || req.Cartas.Count == 0)
            throw new EntradaInvalidaException("Informe as cartas da tiragem.");

        var ambito = (await catalogo.ListarAmbitosAsync()).FirstOrDefault(a => a.Id == req.AmbitoId)
            ?? throw new EntradaInvalidaException($"Âmbito {req.AmbitoId} não existe.");
        var tipo = (await catalogo.ListarTiposTiragemAsync()).FirstOrDefault(t => t.Id == req.TipoTiragemId)
            ?? throw new EntradaInvalidaException($"Tipo de tiragem {req.TipoTiragemId} não existe.");
        var posicoes = (await catalogo.ListarPosicoesAsync()).ToDictionary(p => p.Id);
        var cartas = (await catalogo.ListarCartasAsync()).ToDictionary(c => c.Id);

        var entrada = req.Cartas.Select(c => new CartaNaPosicao(
            posicoes.GetValueOrDefault(c.PosicaoId) ?? throw new EntradaInvalidaException($"Posição {c.PosicaoId} não existe."),
            cartas.GetValueOrDefault(c.CartaId) ?? throw new EntradaInvalidaException($"Carta {c.CartaId} não existe.")
        )).ToList();

        // Guarda só os fatos que alguma carta da tiragem realmente usa.
        var usados = entrada.Select(e => e.Carta.FatoCodigo).OfType<string>().ToHashSet();
        var fatos = (req.Fatos ?? new()).Where(f => usados.Contains(f.Key)).ToDictionary(f => f.Key, f => f.Value);

        // A base é lida a cada tiragem: alterar o banco muda o comportamento sem recompilar (RNF03).
        var bc = new BaseConhecimento(await baseRepo.ListarSignificadosAsync(), await baseRepo.ListarRegrasAsync());
        var r = motor.Inferir(bc, ambito, tipo, entrada, fatos);

        var tiragem = new Tiragem
        {
            AmbitoId = ambito.Id,
            TipoTiragemId = tipo.Id,
            DataHora = DateTime.Now,
            FatosJson = fatos.Count == 0 ? null : JsonSerializer.Serialize(fatos),
            Anotacao = string.IsNullOrWhiteSpace(req.Anotacao) ? null : req.Anotacao.Trim(),
            Cartas = r.Detalhes.Select(d => new TiragemCarta
            {
                PosicaoId = d.Posicao.Id, CartaId = d.Carta.Id, SignificadoId = d.Significado?.Id,
                CategoriaAplicadaId = d.Categoria.Id, TextoAplicado = d.Texto
            }).ToList(),
            Resultado = new ResultadoTiragem
            {
                CategoriaResultadoId = r.Categoria.Id, RegraId = r.RegraAplicada.Id, TextoFinal = r.TextoFinal,
                Combinacoes = r.Combinacoes.Count == 0 ? null : string.Join(",", r.Combinacoes.Select(c => c.CodigoRegra)),
            }
        };
        await tiragens.AdicionarAsync(tiragem);

        return new TiragemResponse(
            tiragem.Id, tiragem.DataHora, ambito.Nome, tipo.Nome,
            r.Detalhes.Select(d => new DetalheDto(d.Posicao.Nome, d.Carta.Nome, d.Carta.NumeroArcano, d.Carta.Numeral, d.CodigoRegra,
                d.Fallback, d.Texto, d.Categoria.Nome, d.FatoUsado)).ToList(),
            r.Combinacoes.Select(c => new CombinacaoDto(c.CodigoRegra, c.Texto, c.Categoria, c.DefiniuResultado)).ToList(),
            new ResultadoFinalDto(r.Categoria.Nome, r.Categoria.Codigo, r.RegraAplicada.CodigoRegra, r.Complemento, r.TextoFinal),
            r.Rastro.Select(p => new PassoRastroDto(p.CodigoRegra, p.Prioridade, p.Disparou, p.Observacao)).ToList(),
            fatos.Count == 0 ? null : fatos,
            tiragem.Anotacao);
    }

    public async Task<TiragemResponse> ObterAsync(int id)
    {
        var t = await tiragens.ObterAsync(id) ?? throw new NaoEncontradoException($"Tiragem {id} não encontrada.");
        var ordem = t.TipoTiragem.PosicoesOrdenadas().Select(p => p.Id).ToList();
        var res = t.Resultado!;
        var codigos = (res.Combinacoes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var regras = (await baseRepo.ListarRegrasAsync()).Where(g => codigos.Contains(g.CodigoRegra));
        var fatos = t.FatosJson is null ? null : JsonSerializer.Deserialize<Dictionary<string, bool>>(t.FatosJson);
        return new TiragemResponse(t.Id, t.DataHora, t.Ambito.Nome, t.TipoTiragem.Nome,
            t.Cartas.OrderBy(c => ordem.IndexOf(c.PosicaoId)).Select(c => new DetalheDto(
                c.Posicao.Nome, c.Carta.Nome, c.Carta.NumeroArcano, c.Carta.Numeral,
                c.Significado is null ? BaseConhecimento.CodigoFallback : c.Carta.CodigoRegra ?? "R??", c.Significado is null,
                c.TextoAplicado, c.CategoriaAplicada?.Nome, null)).ToList(),
            regras.Select(g => new CombinacaoDto(g.CodigoRegra, g.Texto, g.CategoriaResultado?.Nome, g.Id == res.RegraId)).ToList(),
            new ResultadoFinalDto(res.CategoriaResultado.Nome, res.CategoriaResultado.Codigo, res.Regra.CodigoRegra, res.Regra.TextoComplemento, res.TextoFinal),
            null, fatos, t.Anotacao);
    }

    public async Task<List<TiragemResumoDto>> HistoricoAsync(int limite = 50) =>
        (await tiragens.ListarRecentesAsync(limite)).Select(t =>
        {
            var ordem = t.TipoTiragem.PosicoesOrdenadas().Select(p => p.Id).ToList();
            var cartas = string.Join(" / ", t.Cartas.OrderBy(c => ordem.IndexOf(c.PosicaoId)).Select(c => c.Carta.Nome));
            return new TiragemResumoDto(t.Id, t.DataHora, t.Ambito.Nome, t.TipoTiragem.Nome, cartas,
                t.Resultado!.CategoriaResultado.Nome, t.Resultado.Regra.CodigoRegra);
        }).ToList();
}

/// <summary>Monta a listagem completa da base de regras para validação com o professor (RF09).</summary>
public class RegrasService(ICatalogoRepositorio catalogo, IBaseConhecimentoRepositorio baseRepo)
{
    public async Task<List<RegraDto>> ListarAsync()
    {
        var lista = new List<RegraDto>();

        foreach (var a in await catalogo.ListarAmbitosAsync())
            lista.Add(new(a.CodigoRegra, "Contexto", "CONTEXTO",
                $"SE âmbito = \"{a.Nome}\" ENTÃO usar o dicionário de significados do âmbito ({a.Descricao.TrimEnd('.')}).", null, null, null, "Seção 8.2", null));

        foreach (var p in await catalogo.ListarPosicoesAsync())
            lista.Add(new(p.CodigoRegra, "Posição", "POSICAO",
                $"SE carta está na posição \"{p.Nome}\" ENTÃO ela {Minuscula(p.Descricao)}", null, null, null, "Seção 8.2", null));

        foreach (var t in await catalogo.ListarTiposTiragemAsync())
            lista.Add(new(t.CodigoRegra, "Posição", "TIPO_TIRAGEM",
                $"SE tipo de tiragem = {t.Nome} ENTÃO posições = [{string.Join(", ", t.PosicoesOrdenadas().Select(p => p.Nome))}].",
                null, null, null, "RF03", null));

        var cartas = await catalogo.ListarCartasAsync();
        var significados = await baseRepo.ListarSignificadosAsync();
        foreach (var c in cartas.Where(c => c.CodigoRegra is not null))
        {
            var categoria = c.FatoCodigo is null
                ? c.CategoriaSugerida.Nome
                : $"{c.CategoriaSugerida.Nome} se {c.Fato?.Codigo} = sim; senão {c.CategoriaSeFalso?.Nome}";
            if (c.NuanceCategoria is not null) categoria += $" ({c.NuanceCategoria})";
            var detalhes = significados.Where(s => s.CartaId == c.Id)
                .OrderBy(s => s.PosicaoId).ThenBy(s => s.AmbitoId)
                .Select(s => new SignificadoDto(s.Ambito?.Nome ?? "qualquer", s.Posicao?.Nome ?? "qualquer", s.Texto)).ToList();
            lista.Add(new(c.CodigoRegra!, "Carta", "CARTA",
                $"SE carta = \"{c.Nome}\" ENTÃO significado conforme âmbito × posição ({detalhes.Count} combinações; ficha na Seção 9.3).",
                categoria, null, null, "Seção 9.3", detalhes));
        }

        lista.Add(new(BaseConhecimento.CodigoFallback, "Carta", "FALLBACK",
            "SE não existir significado detalhado para a carta ENTÃO usar a ficha resumida (significado geral + categoria sugerida).",
            null, null, null, "Seção 9.4",
            cartas.Where(c => c.CodigoRegra is null)
                .Select(c => new SignificadoDto(c.Nome, c.CategoriaSugerida.Nome, c.DescricaoGeral)).ToList()));

        foreach (var r in await baseRepo.ListarRegrasAsync())
        {
            var camada = r.Tipo switch { TipoRegra.CONSOLIDACAO => "Consolidação", _ => "Combinação" };
            var categoria = r.Tipo == TipoRegra.OBSERVACAO ? null
                : r.CategoriaResultado?.Nome ?? $"(herdada de {r.HerdaCategoriaDe})";
            lista.Add(new(r.CodigoRegra, camada, r.Tipo.ToString(), r.Descricao, categoria, r.Prioridade, r.CondicaoJson, r.Fonte, null));
        }

        return lista.OrderBy(r => int.Parse(r.Codigo[1..])).ToList();
    }

    private static string Minuscula(string s) => s.Length == 0 ? s : char.ToLower(s[0]) + s[1..];
}
