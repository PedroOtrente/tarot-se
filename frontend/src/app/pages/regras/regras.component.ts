import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ApiService } from '../../core/api.service';
import { Regra } from '../../core/models';

/** Base de regras lida do banco — para a validação em sala (RF09). */
@Component({
  selector: 'app-regras',
  standalone: true,
  template: `
    <section class="painel">
      <h2>Base de regras</h2>
      <p class="sutil">
        {{ regras().length }} regras lidas do banco de dados. Cada carta recebe o significado mais específico para o
        âmbito e a posição. Depois, todas as regras de combinação e consolidação são avaliadas: entre as verdadeiras
        (conjunto de conflito), vence a de maior prioridade (P1 é a mais alta).
      </p>
      <div class="filtros">
        @for (c of camadas; track c) {
          <button class="chip" [class.ativo]="filtro() === c" (click)="filtro.set(c)">{{ c }} <small>{{ contagem(c) }}</small></button>
        }
      </div>
      @if (erro()) { <p class="erro">{{ erro() }}</p> }
      <div class="tabela-rolagem">
        <table>
          <thead><tr><th>Cód.</th><th>Camada</th><th>Regra (SE…ENTÃO)</th><th>Categoria</th></tr></thead>
          <tbody>
            @for (r of visiveis(); track r.codigo) {
              <tr [class.consolidacao]="r.camada === 'Consolidação'" [class.combinacao]="r.camada === 'Combinação'">
                <td><strong>{{ r.codigo }}</strong>
                  @if (r.prioridade) { <br><small>P{{ r.prioridade }}</small> }
                  @if (r.tipo === 'OBSERVACAO') { <br><small>observação</small> }
                </td>
                <td>{{ r.camada }}</td>
                <td>{{ r.regra }}
                  @if (r.detalhes?.length) {
                    <details class="detalhes-regra">
                      <summary>{{ r.tipo === 'FALLBACK' ? 'Fichas resumidas (' + r.detalhes!.length + ')' : 'Ver as ' + r.detalhes!.length + ' combinações' }}</summary>
                      <table class="mini">
                        @for (d of r.detalhes; track $index) {
                          <tr><td><small>{{ d.ambito }}<br>{{ d.posicao }}</small></td><td>{{ d.texto }}</td></tr>
                        }
                      </table>
                    </details>
                  }
                  @if (r.condicao) { <code>{{ r.condicao }}</code> }
                  @if (r.fonte) { <small class="fonte">Fonte: {{ r.fonte }}</small> }
                </td>
                <td>{{ r.categoria ?? '—' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>
  `,
})
export class RegrasComponent implements OnInit {
  private api = inject(ApiService);
  regras = signal<Regra[]>([]);
  erro = signal('');
  camadas = ['Todas', 'Contexto', 'Posição', 'Carta', 'Combinação', 'Consolidação'];
  filtro = signal('Todas');
  visiveis = computed(() => this.filtro() === 'Todas' ? this.regras() : this.regras().filter(r => r.camada === this.filtro()));
  contagem = (c: string) => c === 'Todas' ? this.regras().length : this.regras().filter(r => r.camada === c).length;
  ngOnInit() { this.api.regras().subscribe({ next: r => this.regras.set(r), error: (e: Error) => this.erro.set(e.message) }); }
}
