import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { CLASSE_CATEGORIA, TiragemResponse } from '../../core/models';
import { TiragemStateService } from '../../core/tiragem-state.service';
import { CartaComponent } from '../../shared/carta.component';
import { WizardStepsComponent } from '../../shared/wizard-steps.component';

@Component({
  selector: 'app-result',
  standalone: true,
  imports: [WizardStepsComponent, CartaComponent, RouterLink, DatePipe],
  templateUrl: './result.component.html',
})
export class ResultComponent implements OnInit {
  /** Vem da rota /resultado/:id (withComponentInputBinding). */
  id = input.required<string>();

  private api = inject(ApiService);
  private router = inject(Router);
  private estado = inject(TiragemStateService);

  tiragem = signal<TiragemResponse | null>(null);
  erro = signal('');
  classe = computed(() => CLASSE_CATEGORIA[this.tiragem()?.resultadoFinal.categoriaCodigo ?? ''] ?? '');

  ngOnInit() {
    // Se veio direto do wizard, usa a resposta do POST (que inclui o rastro); senão, busca na API.
    const ultimo = this.estado.ultimoResultado();
    if (ultimo && ultimo.tiragemId === +this.id()) this.tiragem.set(ultimo);
    else this.api.tiragem(+this.id()).subscribe({ next: t => this.tiragem.set(t), error: (e: Error) => this.erro.set(e.message) });
  }

  respostaFato(t: TiragemResponse) {
    return t.fatos ? Object.entries(t.fatos).map(([k, v]) => `${k}: ${v ? 'sim' : 'não'}`).join(' · ') : '';
  }

  novaLeitura() {
    this.estado.reiniciar();
    this.router.navigate(['/ambito']);
  }
}
