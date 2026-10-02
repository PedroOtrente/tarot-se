export interface Ambito { id: number; nome: string; descricao: string; regra: string; }
export interface Posicao { id: number; nome: string; descricao: string; }
export interface TipoTiragem { id: number; nome: string; qtdCartas: number; descricao: string; regra: string; posicoes: Posicao[]; }
export interface Fato { codigo: string; pergunta: string; }
export interface Carta {
  id: number; nome: string; numeroArcano: number; numeral: string; regra: string | null;
  aprofundada: boolean; descricaoGeral: string; categoriaSugerida: string; fato: Fato | null;
}

export interface CriarTiragemRequest {
  ambitoId: number;
  tipoTiragemId: number;
  cartas: { posicaoId: number; cartaId: number }[];
  fatos?: Record<string, boolean> | null;
  anotacao?: string | null;
}

export interface Detalhe {
  posicao: string; carta: string; numeroArcano: number; numeral: string; regra: string;
  fallback: boolean; significado: string; categoriaSugerida: string | null; fatoUsado: string | null;
}
export interface Combinacao { regra: string; texto: string; categoria: string | null; definiuResultado: boolean; }
export interface ResultadoFinal { categoria: string; categoriaCodigo: string; regraAplicada: string; complemento: string | null; texto: string; }
export interface PassoRastro { regra: string; prioridade: number; disparou: boolean; observacao: string; }
export interface TiragemResponse {
  tiragemId: number; dataHora: string; ambito: string; tipoTiragem: string;
  detalhes: Detalhe[]; combinacoes: Combinacao[]; resultadoFinal: ResultadoFinal; rastro: PassoRastro[] | null;
  fatos: Record<string, boolean> | null; anotacao: string | null;
}
export interface TiragemResumo { tiragemId: number; dataHora: string; ambito: string; tipoTiragem: string; cartas: string; categoria: string; regraAplicada: string; }
export interface SignificadoResumo { ambito: string; posicao: string; texto: string; }
export interface Regra {
  codigo: string; camada: string; tipo: string; regra: string; categoria: string | null; prioridade: number | null;
  condicao: string | null; fonte: string | null; detalhes: SignificadoResumo[] | null;
}

/** Símbolo decorativo de cada arcano (índice = número do arcano). */
export const SIMBOLOS = ['✧', '∞', '☽', '♀', '♔', '✠', '♡', '➹', '♌', '⚲', '☸', '⚖', '⟟', '☠', '♒', '♑', '⚡', '★', '☾', '☀', '♪', '⊕'];

export const CLASSE_CATEGORIA: Record<string, string> = {
  FAVORAVEL: 'cat-favoravel', CAUTELA: 'cat-cautela', MUDANCA: 'cat-mudanca', NEUTRO: 'cat-neutro',
};
