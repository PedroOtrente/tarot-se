using Microsoft.AspNetCore.Mvc;
using TarotSE.Application;

namespace TarotSE.Api.Controllers;

// Controllers só recebem a requisição e devolvem a resposta — sem regra de negócio (RNF04).

[ApiController, Route("api")]
public class CatalogoController(CatalogoService servico) : ControllerBase
{
    /// <summary>Lista os âmbitos (R01–R03).</summary>
    [HttpGet("ambitos")] public Task<List<AmbitoDto>> Ambitos() => servico.AmbitosAsync();

    /// <summary>Lista os tipos de tiragem e as posições que cada um usa (R07–R09).</summary>
    [HttpGet("tipos-tiragem")] public Task<List<TipoTiragemDto>> TiposTiragem() => servico.TiposTiragemAsync();

    /// <summary>Lista os 22 Arcanos Maiores.</summary>
    [HttpGet("cartas")] public Task<List<CartaDto>> Cartas() => servico.CartasAsync();
}

[ApiController, Route("api/tiragens")]
public class TiragensController(TiragemService servico) : ControllerBase
{
    /// <summary>Executa o motor de inferência sobre a tiragem, persiste e devolve a interpretação.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TiragemResponse), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<ActionResult<TiragemResponse>> Criar(CriarTiragemRequest req)
    {
        var r = await servico.RealizarAsync(req);
        return CreatedAtAction(nameof(Obter), new { id = r.TiragemId }, r);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TiragemResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public Task<TiragemResponse> Obter(int id) => servico.ObterAsync(id);

    /// <summary>Histórico das tiragens mais recentes.</summary>
    [HttpGet] public Task<List<TiragemResumoDto>> Historico([FromQuery] int limite = 50) => servico.HistoricoAsync(Math.Clamp(limite, 1, 200));
}

[ApiController, Route("api/regras")]
public class RegrasController(RegrasService servico) : ControllerBase
{
    /// <summary>Base de regras completa (R01–R28), para validação com o professor.</summary>
    [HttpGet] public Task<List<RegraDto>> Listar() => servico.ListarAsync();
}
