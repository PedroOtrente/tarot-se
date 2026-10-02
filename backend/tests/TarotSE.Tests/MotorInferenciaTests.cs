using TarotSE.Domain.Entidades;
using TarotSE.Domain.Motor;
using static TarotSE.Tests.BaseDeTeste;

namespace TarotSE.Tests;

public class MotorInferenciaTests
{
    private readonly MotorInferencia _motor = new();

    private ResultadoInferencia Rodar(Ambito a, TipoTiragem t, string[] cartas, Dictionary<string, bool>? fatos = null) =>
        _motor.Inferir(Base(), a, t, Entrada(t, cartas), fatos);

    // ---------------- Casos de teste (Seção 11, ajustados a tiragens válidas) ----------------
    public static TheoryData<string, int, int, string[], string, string> Casos => new()
    {
        { "CT01", 2, 3, ["Os Enamorados", "A Torre", "O Sol"],            "R23", "MUDANCA" },
        { "CT02", 1, 2, ["A Sacerdotisa", "A Roda da Fortuna"],           "R55", "CAUTELA" },
        { "CT03", 3, 3, ["O Hierofante", "A Sacerdotisa", "O Sol"],       "R53", "FAVORAVEL" },
        { "CT04", 1, 1, ["O Louco"],                                      "R56", "NEUTRO" },
        { "CT05", 2, 1, ["O Eremita"],                                    "R56", "NEUTRO" },
        { "CT06", 3, 2, ["A Torre", "A Morte"],                           "R52", "MUDANCA" },
        { "CT07", 2, 3, ["O Mago", "O Louco", "O Sol"],                   "R21", "FAVORAVEL" },
        { "CT08", 1, 3, ["O Louco", "A Torre", "A Lua"],                  "R22", "CAUTELA" },
        { "CT09", 1, 3, ["O Diabo", "A Temperança", "A Estrela"],         "R24", "MUDANCA" },
        { "CT10", 2, 2, ["Os Enamorados", "O Diabo"],                     "R25", "CAUTELA" },
        { "CT12", 2, 3, ["O Sol", "A Roda da Fortuna", "O Hierofante"],   "R31", "FAVORAVEL" },
        { "CT13", 1, 3, ["A Torre", "O Louco", "O Sol"],                  "R30", "MUDANCA" },
    };

    [Theory, MemberData(nameof(Casos))]
    public void Casos_de_teste(string caso, int ambitoId, int tipoId, string[] cartas, string regraFinal, string categoria)
    {
        var r = Rodar(Ambitos[ambitoId - 1], new[] { Uma, Duas, Tres }[tipoId - 1], cartas);
        Assert.True(regraFinal == r.RegraAplicada.CodigoRegra, $"{caso}: esperava {regraFinal}, veio {r.RegraAplicada.CodigoRegra}");
        Assert.Equal(categoria, r.Categoria.Codigo);
        Assert.Equal(1, r.Rastro.Count(p => p.Disparou));
    }

    [Fact]
    public void CT01_usa_as_fichas_e_lista_as_combinacoes()
    {
        var r = Rodar(Profissional, Tres, ["Os Enamorados", "A Torre", "O Sol"]);
        Assert.Equal(["R10", "R17", "R19"], r.Detalhes.Select(d => d.CodigoRegra));
        Assert.StartsWith("Pessoa diante de uma escolha de carreira", r.Detalhes[0].Texto);
        Assert.Equal(["R23", "R30", "R40", "R41"], r.Combinacoes.Select(c => c.CodigoRegra));
        Assert.True(r.Combinacoes.Single(c => c.CodigoRegra == "R23").DefiniuResultado);
        Assert.Equal("tendência positiva após a mudança", r.Complemento);
    }

    [Fact]
    public void CT05_carta_de_ficha_resumida_usa_fallback_R26_com_categoria()
    {
        var r = Rodar(Profissional, Uma, ["A Lua"]);
        Assert.True(r.Detalhes[0].Fallback);
        Assert.Equal("R26", r.Detalhes[0].CodigoRegra);
        Assert.Equal("CAUTELA", r.Detalhes[0].Categoria.Codigo);
        Assert.Equal("R55", r.RegraAplicada.CodigoRegra);
    }

    [Theory]
    [InlineData(true, "R54", "FAVORAVEL")]
    [InlineData(false, "R52", "MUDANCA")]
    public void CT11_Justica_depende_do_historico_informado(bool solido, string regra, string categoria)
    {
        var r = Rodar(Academico, Uma, ["A Justiça"], new() { ["HISTORICO_SOLIDO"] = solido });
        Assert.Equal(regra, r.RegraAplicada.CodigoRegra);
        Assert.Equal(categoria, r.Categoria.Codigo);
    }

    [Fact]
    public void CT14_Justica_sem_resposta_e_rejeitada_com_a_pergunta()
    {
        var ex = Assert.Throws<EntradaInvalidaException>(() => Rodar(Academico, Uma, ["A Justiça"]));
        Assert.Contains("histórico", ex.Message);
    }

    [Fact]
    public void CT14_posicoes_incompativeis_com_o_tipo_sao_rejeitadas()
    {
        var entrada = new List<CartaNaPosicao> { new(Sig, Cartas["O Louco"]), new(Res, Cartas["O Sol"]) };
        Assert.Throws<EntradaInvalidaException>(() => _motor.Inferir(Base(), Profissional, Duas, entrada));
    }

    [Fact]
    public void CT14_carta_repetida_e_rejeitada()
    {
        var entrada = new List<CartaNaPosicao> { new(Sig, Cartas["O Sol"]), new(Mov, Cartas["O Sol"]) };
        Assert.Throws<EntradaInvalidaException>(() => _motor.Inferir(Base(), Profissional, Duas, entrada));
    }

    // ---------------- Propriedades da base ----------------
    [Fact]
    public void Os_14_arcanos_aprofundados_tem_as_9_combinacoes()
    {
        var posicoes = new[] { Sig, Mov, Res };
        foreach (var c in Cartas.Values.Where(c => c.Aprofundada))
        foreach (var a in Ambitos)
        for (var i = 0; i < 3; i++)
        {
            // coloca a carta na posição i e completa com cartas de ficha resumida
            var enchimento = new Queue<Carta>(new[] { "O Carro", "O Eremita" }.Select(n => Cartas[n]));
            var entrada = posicoes.Select((p, j) => new CartaNaPosicao(p, j == i ? c : enchimento.Dequeue())).ToList();
            var r = _motor.Inferir(Base(), a, Tres, entrada, new Dictionary<string, bool> { ["HISTORICO_SOLIDO"] = true });
            Assert.False(r.Detalhes[i].Fallback, $"{c.Nome} em {a.Nome}/{posicoes[i].Nome} caiu no fallback");
        }
        Assert.Equal(14, Cartas.Values.Count(c => c.Aprofundada));
        Assert.Equal(126, Significados().Count);
    }

    [Fact]
    public void Base_e_completa_e_deterministica_para_todas_as_tiragens_de_3_cartas()
    {
        var bc = Base();
        var todas = Cartas.Values.ToList();
        var total = 0;
        foreach (var solido in new[] { true, false })
        foreach (var a in Ambitos)
        foreach (var s in todas) foreach (var m in todas) foreach (var res in todas)
        {
            if (s == m || s == res || m == res) continue;
            var fatos = new Dictionary<string, bool> { ["HISTORICO_SOLIDO"] = solido };
            var r1 = _motor.Inferir(bc, a, Tres, [new(Sig, s), new(Mov, m), new(Res, res)], fatos);
            Assert.Equal(1, r1.Rastro.Count(p => p.Disparou));
            total++;
        }
        Assert.Equal(2 * 3 * 22 * 21 * 20, total);
    }

    [Fact]
    public void Todas_as_4_categorias_sao_alcancaveis_em_tiragem_de_1_carta()
    {
        var cats = new[] { "O Sol", "A Lua", "A Torre", "O Louco" }
            .Select(c => Rodar(Relacionamentos, Uma, [c]).Categoria.Codigo).ToHashSet();
        Assert.Equal(4, cats.Count);
    }

    [Fact]
    public void Base_com_prioridade_duplicada_e_rejeitada()
    {
        var regras = Regras();
        regras.First(r => r.CodigoRegra == "R21").Prioridade = 1;
        Assert.Throws<BaseConhecimentoInvalidaException>(() => new BaseConhecimento(Significados(), regras));
    }
}
