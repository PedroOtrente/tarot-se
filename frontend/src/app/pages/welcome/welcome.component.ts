import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TiragemStateService } from '../../core/tiragem-state.service';

@Component({
  selector: 'app-welcome',
  standalone: true,
  template: `
    <section class="painel centro">
      <div class="lua">☾</div>
      <h1>Sistema Especialista de Tarô</h1>
      <p class="lead">
        Uma leitura dos 22 Arcanos Maiores guiada por regras explícitas, para refletir sobre
        <strong>relacionamentos</strong>, vida <strong>profissional</strong> ou <strong>acadêmica</strong>.
      </p>
      <p class="sutil">
        Cada interpretação vem de uma base de regras SE…ENTÃO, e o sistema mostra qual regra gerou cada conclusão.
        É um apoio à reflexão, não uma previsão.
      </p>
      <button class="btn primario grande" (click)="comecar()">Começar leitura</button>
    </section>
  `,
})
export class WelcomeComponent {
  private router = inject(Router);
  private estado = inject(TiragemStateService);
  comecar() { this.estado.reiniciar(); this.router.navigate(['/ambito']); }
}
