import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { Ambito } from '../../core/models';
import { TiragemStateService } from '../../core/tiragem-state.service';
import { WizardStepsComponent } from '../../shared/wizard-steps.component';

@Component({
  selector: 'app-select-ambito',
  standalone: true,
  imports: [WizardStepsComponent],
  template: `
    <app-wizard-steps [atual]="1" />
    <section class="painel">
      <h2>Sobre qual área da sua vida você gostaria de refletir hoje?</h2>
      @if (erro()) { <p class="erro">{{ erro() }}</p> }
      <div class="opcoes">
        @for (a of ambitos(); track a.id) {
          <button class="opcao" [class.escolhida]="estado.ambito()?.id === a.id" (click)="escolher(a)">
            <span class="icone">{{ icones[a.id] }}</span>
            <strong>{{ a.nome }}</strong>
            <small>{{ a.descricao }}</small>
            <span class="tag">{{ a.regra }}</span>
          </button>
        }
      </div>
    </section>
  `,
})
export class SelectAmbitoComponent implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  estado = inject(TiragemStateService);
  ambitos = signal<Ambito[]>([]);
  erro = signal('');
  icones: Record<number, string> = { 1: '♡', 2: '⚒', 3: '✎' };

  ngOnInit() {
    this.api.ambitos().subscribe({ next: a => this.ambitos.set(a), error: (e: Error) => this.erro.set(e.message) });
  }

  escolher(a: Ambito) {
    this.estado.escolherAmbito(a);
    this.router.navigate(['/tiragem']);
  }
}
