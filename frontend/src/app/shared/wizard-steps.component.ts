import { Component, input } from '@angular/core';

@Component({
  selector: 'app-wizard-steps',
  standalone: true,
  template: `
    <ol class="passos" aria-label="Etapas da leitura">
      @for (p of passos; track p; let i = $index) {
        <li [class.feito]="i + 1 < atual()" [class.atual]="i + 1 === atual()">
          <span class="bolinha">{{ i + 1 }}</span><span class="rotulo">{{ p }}</span>
        </li>
      }
    </ol>
  `,
})
export class WizardStepsComponent {
  atual = input.required<number>();
  passos = ['Âmbito', 'Tiragem', 'Cartas', 'Resultado'];
}
