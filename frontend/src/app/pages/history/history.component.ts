import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { TiragemResumo } from '../../core/models';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [RouterLink, DatePipe],
  template: `
    <section class="painel">
      <h2>Histórico de leituras</h2>
      @if (erro()) { <p class="erro">{{ erro() }}</p> }
      @if (!erro() && itens().length === 0) { <p class="sutil">Nenhuma leitura registrada ainda.</p> }
      @if (itens().length) {
        <div class="tabela-rolagem">
          <table>
            <thead><tr><th>Nº</th><th>Data</th><th>Âmbito</th><th>Tiragem</th><th>Cartas</th><th>Resultado</th><th>Regra</th></tr></thead>
            <tbody>
              @for (t of itens(); track t.tiragemId) {
                <tr>
                  <td><a [routerLink]="['/resultado', t.tiragemId]">{{ t.tiragemId }}</a></td>
                  <td>{{ t.dataHora | date: 'dd/MM HH:mm' }}</td>
                  <td>{{ t.ambito }}</td><td>{{ t.tipoTiragem }}</td><td>{{ t.cartas }}</td>
                  <td>{{ t.categoria }}</td><td><span class="tag">{{ t.regraAplicada }}</span></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `,
})
export class HistoryComponent implements OnInit {
  private api = inject(ApiService);
  itens = signal<TiragemResumo[]>([]);
  erro = signal('');
  ngOnInit() { this.api.historico().subscribe({ next: i => this.itens.set(i), error: (e: Error) => this.erro.set(e.message) }); }
}
