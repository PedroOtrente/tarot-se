import { Injectable, computed, signal } from '@angular/core';
import { Ambito, Carta, Fato, TipoTiragem, TiragemResponse } from './models';

/** Estado do wizard (Seção 7.3): só é enviado à API quando todas as posições estiverem preenchidas. */
@Injectable({ providedIn: 'root' })
export class TiragemStateService {
  readonly ambito = signal<Ambito | null>(null);
  readonly tipo = signal<TipoTiragem | null>(null);
  /** posicaoId → carta */
  readonly cartas = signal<Record<number, Carta>>({});
  readonly anotacao = signal('');
  /** Respostas a perguntas que algumas cartas exigem (ex.: A Justiça → HISTORICO_SOLIDO). */
  readonly fatos = signal<Record<string, boolean>>({});

  /** Perguntas exigidas pelas cartas já escolhidas. */
  readonly perguntas = computed(() => {
    const vistos = new Map<string, Fato>();
    Object.values(this.cartas()).forEach(c => { if (c.fato) vistos.set(c.fato.codigo, c.fato); });
    return [...vistos.values()];
  });
  readonly ultimoResultado = signal<TiragemResponse | null>(null);

  readonly completa = computed(() => {
    const t = this.tipo();
    return !!t && !!this.ambito() && t.posicoes.every(p => !!this.cartas()[p.id])
      && this.perguntas().every(f => this.fatos()[f.codigo] !== undefined);
  });

  escolherAmbito(a: Ambito) { this.ambito.set(a); }

  responder(codigo: string, valor: boolean) { this.fatos.set({ ...this.fatos(), [codigo]: valor }); }

  escolherTipo(t: TipoTiragem) {
    this.tipo.set(t);
    this.cartas.set({});
  }

  definirCarta(posicaoId: number, carta: Carta | null) {
    const atual = { ...this.cartas() };
    if (carta) atual[posicaoId] = carta; else delete atual[posicaoId];
    this.cartas.set(atual);
  }

  reiniciar() {
    this.ambito.set(null);
    this.tipo.set(null);
    this.cartas.set({});
    this.fatos.set({});
    this.anotacao.set('');
  }
}
