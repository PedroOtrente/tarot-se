import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { Carta, Posicao } from '../../core/models';
import { TiragemStateService } from '../../core/tiragem-state.service';
import { CartaComponent } from '../../shared/carta.component';
import { WizardStepsComponent } from '../../shared/wizard-steps.component';

@Component({
  selector: 'app-draw-cards',
  standalone: true,
  imports: [WizardStepsComponent, CartaComponent, FormsModule, RouterLink],
  templateUrl: './draw-cards.component.html',
})
export class DrawCardsComponent implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  estado = inject(TiragemStateService);

  baralho = signal<Carta[]>([]);
  erro = signal('');
  enviando = signal(false);
  posicoes = computed(() => this.estado.tipo()?.posicoes ?? []);

  ngOnInit() {
    if (!this.estado.ambito() || !this.estado.tipo()) { this.router.navigate(['/ambito']); return; }
    this.api.cartas().subscribe({ next: c => this.baralho.set(c), error: (e: Error) => this.erro.set(e.message) });
  }

  cartaEm(p: Posicao): Carta | null { return this.estado.cartas()[p.id] ?? null; }

  /** Cartas ainda disponíveis para uma posição (sem repetição na mesma tiragem — RF04). */
  disponiveis(p: Posicao) {
    const usadas = new Set(Object.entries(this.estado.cartas()).filter(([pid]) => +pid !== p.id).map(([, c]) => c.id));
    return this.baralho().filter(c => !usadas.has(c.id));
  }

  selecionar(p: Posicao, id: string) {
    this.estado.definirCarta(p.id, this.baralho().find(c => c.id === +id) ?? null);
  }

  sortear(p: Posicao) {
    const opcoes = this.disponiveis(p);
    this.estado.definirCarta(p.id, opcoes[Math.floor(Math.random() * opcoes.length)]);
  }

  sortearTodas() {
    this.posicoes().forEach(p => this.estado.definirCarta(p.id, null));
    this.posicoes().forEach(p => this.sortear(p));
  }

  interpretar() {
    if (!this.estado.completa()) return;
    this.enviando.set(true);
    this.erro.set('');
    this.api.criarTiragem({
      ambitoId: this.estado.ambito()!.id,
      tipoTiragemId: this.estado.tipo()!.id,
      cartas: this.posicoes().map(p => ({ posicaoId: p.id, cartaId: this.estado.cartas()[p.id].id })),
      fatos: this.estado.perguntas().length ? this.estado.fatos() : null,
      anotacao: this.estado.anotacao() || null,
    }).subscribe({
      next: r => {
        this.estado.ultimoResultado.set(r);
        this.router.navigate(['/resultado', r.tiragemId]);
      },
      error: (e: Error) => { this.erro.set(e.message); this.enviando.set(false); },
    });
  }
}
