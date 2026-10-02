using TarotSE.Domain.Entidades;
using TarotSE.Domain.Motor;

namespace TarotSE.Application;

// ---------------- Repositórios (implementados na Infrastructure) ----------------
public interface ICatalogoRepositorio
{
    Task<List<Ambito>> ListarAmbitosAsync();
    Task<List<Posicao>> ListarPosicoesAsync();
    Task<List<TipoTiragem>> ListarTiposTiragemAsync();
    Task<List<Carta>> ListarCartasAsync();
    Task<List<CategoriaResultado>> ListarCategoriasAsync();
}

public interface IBaseConhecimentoRepositorio
{
    Task<List<Significado>> ListarSignificadosAsync();
    Task<List<Regra>> ListarRegrasAsync();
}

public interface ITiragemRepositorio
{
    Task AdicionarAsync(Tiragem tiragem);
    Task<Tiragem?> ObterAsync(int id);
    Task<List<Tiragem>> ListarRecentesAsync(int limite);
}

// ---------------- DTOs ----------------
public record AmbitoDto(int Id, string Nome, string Descricao, string Regra);
public record PosicaoDto(int Id, string Nome, string Descricao);
public record TipoTiragemDto(int Id, string Nome, int QtdCartas, string Descricao, string Regra, List<PosicaoDto> Posicoes);
public record FatoDto(string Codigo, string Pergunta);
public record CartaDto(int Id, string Nome, int NumeroArcano, string Numeral, string? Regra, bool Aprofundada,
    string DescricaoGeral, string CategoriaSugerida, FatoDto? Fato);

public record CartaPosicaoRequest(int PosicaoId, int CartaId);
public record CriarTiragemRequest(int AmbitoId, int TipoTiragemId, List<CartaPosicaoRequest> Cartas, Dictionary<string, bool>? Fatos, string? Anotacao);

public record DetalheDto(string Posicao, string Carta, int NumeroArcano, string Numeral, string Regra, bool Fallback, string Significado, string? CategoriaSugerida, string? FatoUsado);
public record ResultadoFinalDto(string Categoria, string CategoriaCodigo, string RegraAplicada, string? Complemento, string Texto);
public record PassoRastroDto(string Regra, int Prioridade, bool Disparou, string Observacao);
public record CombinacaoDto(string Regra, string Texto, string? Categoria, bool DefiniuResultado);
public record TiragemResponse(
    int TiragemId, DateTime DataHora, string Ambito, string TipoTiragem,
    List<DetalheDto> Detalhes, List<CombinacaoDto> Combinacoes, ResultadoFinalDto ResultadoFinal,
    List<PassoRastroDto>? Rastro, Dictionary<string, bool>? Fatos, string? Anotacao);
public record TiragemResumoDto(int TiragemId, DateTime DataHora, string Ambito, string TipoTiragem, string Cartas, string Categoria, string RegraAplicada);

public record SignificadoDto(string Ambito, string Posicao, string Texto);
public record RegraDto(string Codigo, string Camada, string Tipo, string Regra, string? Categoria, int? Prioridade,
    string? Condicao, string? Fonte, List<SignificadoDto>? Detalhes);

public class NaoEncontradoException(string mensagem) : Exception(mensagem);
