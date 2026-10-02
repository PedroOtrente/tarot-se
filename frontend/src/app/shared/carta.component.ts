import { Component, computed, input } from '@angular/core';
import { SIMBOLOS } from '../core/models';

/** Desenho de uma carta (verso quando não há carta definida). */
@Component({
  selector: 'app-carta',
  standalone: true,
  template: `
    <div class="carta" [class.verso]="!nome()" [class.pequena]="pequena()">
      @if (nome()) {
        <span class="numeral">{{ numeral() }}</span>
        <span class="simbolo">{{ simbolo() }}</span>
        <span class="nome">{{ nome() }}</span>
      } @else {
        <span class="simbolo">✶</span>
      }
    </div>
  `,
})
export class CartaComponent {
  nome = input<string | null>(null);
  numeral = input<string>('');
  numero = input<number | null>(null);
  pequena = input(false);
  simbolo = computed(() => {
    const n = this.numero();
    return n === null ? '✦' : SIMBOLOS[n] ?? '✦';
  });
}
