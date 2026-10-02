import { Routes } from '@angular/router';
import { DrawCardsComponent } from './pages/draw-cards/draw-cards.component';
import { HistoryComponent } from './pages/history/history.component';
import { RegrasComponent } from './pages/regras/regras.component';
import { ResultComponent } from './pages/result/result.component';
import { SelectAmbitoComponent } from './pages/select-ambito/select-ambito.component';
import { SelectTiragemComponent } from './pages/select-tiragem/select-tiragem.component';
import { WelcomeComponent } from './pages/welcome/welcome.component';

export const routes: Routes = [
  { path: '', component: WelcomeComponent, title: 'Tarô — Sistema Especialista' },
  { path: 'ambito', component: SelectAmbitoComponent, title: 'Escolha o âmbito' },
  { path: 'tiragem', component: SelectTiragemComponent, title: 'Escolha a tiragem' },
  { path: 'cartas', component: DrawCardsComponent, title: 'Retire as cartas' },
  { path: 'resultado/:id', component: ResultComponent, title: 'Resultado da leitura' },
  { path: 'historico', component: HistoryComponent, title: 'Histórico' },
  { path: 'regras', component: RegrasComponent, title: 'Base de regras' },
  { path: '**', redirectTo: '' },
];
