import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { TipoTiragem } from '../../core/models';
import { TiragemStateService } from '../../core/tiragem-state.service';
import { WizardStepsComponent } from '../../shared/wizard-steps.component';

@Component({
  selector: 'app-select-tiragem',
  standalone: true,
  imports: [WizardStepsComponent, RouterLink],
  template: `
    <app-wizard-steps [atual]="2" />
    <section class="painel">
      <p class="contexto">Âmbito: <strong>{{ estado.ambito()?.nome }}</strong></p>
      <h2>Quantas cartas você quer tirar?</h2>
      @if (erro()) { <p class="erro">{{ erro() }}</p> }
      <div class="opcoes">
        @for (t of tipos(); track t.id) {
          <button class="opcao" [class.escolhida]="estado.tipo()?.id === t.id" (click)="escolher(t)">
            <span class="leque">@for (p of t.posicoes; track p.id) { <i></i> }</span>
            <strong>{{ t.nome }}</strong>
            <small>{{ nomesPosicoes(t) }}</small>
            <span class="tag">{{ t.regra }}</span>
          </button>
        }
      </div>
      <div class="acoes"><a class="btn" routerLink="/ambito">← Voltar</a></div>
    </section>
  `,
})
export class SelectTiragemComponent implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  estado = inject(TiragemStateService);
  tipos = signal<TipoTiragem[]>([]);
  erro = signal('');

  ngOnInit() {
    if (!this.estado.ambito()) { this.router.navigate(['/ambito']); return; }
    this.api.tiposTiragem().subscribe({ next: t => this.tipos.set(t), error: (e: Error) => this.erro.set(e.message) });
  }

  nomesPosicoes(t: TipoTiragem) { return t.posicoes.map(p => p.nome).join(' → '); }

  escolher(t: TipoTiragem) {
    this.estado.escolherTipo(t);
    this.router.navigate(['/cartas']);
  }
}
