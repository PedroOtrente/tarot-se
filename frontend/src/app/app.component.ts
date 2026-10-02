import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topo">
      <a routerLink="/" class="marca">☾ Tarô <span>Sistema Especialista</span></a>
      <nav>
        <a routerLink="/ambito" routerLinkActive="ativo">Nova leitura</a>
        <a routerLink="/historico" routerLinkActive="ativo">Histórico</a>
        <a routerLink="/regras" routerLinkActive="ativo">Base de regras</a>
      </nav>
    </header>
    <main><router-outlet /></main>
    <footer class="rodape">Apoio à reflexão, não previsão · Trabalho Prático — Sistema Especialista</footer>
  `,
})
export class AppComponent {}
