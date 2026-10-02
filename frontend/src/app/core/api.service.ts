import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { Ambito, Carta, CriarTiragemRequest, Regra, TipoTiragem, TiragemResponse, TiragemResumo } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  ambitos() { return this.get<Ambito[]>('/ambitos'); }
  tiposTiragem() { return this.get<TipoTiragem[]>('/tipos-tiragem'); }
  cartas() { return this.get<Carta[]>('/cartas'); }
  regras() { return this.get<Regra[]>('/regras'); }
  historico() { return this.get<TiragemResumo[]>('/tiragens'); }
  tiragem(id: number) { return this.get<TiragemResponse>(`/tiragens/${id}`); }

  criarTiragem(req: CriarTiragemRequest): Observable<TiragemResponse> {
    return this.http.post<TiragemResponse>(`${this.base}/tiragens`, req).pipe(catchError(e => this.erro(e)));
  }

  private get<T>(rota: string): Observable<T> {
    return this.http.get<T>(this.base + rota).pipe(catchError(e => this.erro(e)));
  }

  /** Converte o erro HTTP numa mensagem legível (usa o "detail" do ProblemDetails da API). */
  private erro(e: HttpErrorResponse) {
    const msg = e.status === 0
      ? 'Não foi possível falar com a API. Ela está rodando em ' + this.base + '?'
      : e.error?.detail ?? e.error?.title ?? `Erro HTTP ${e.status}`;
    return throwError(() => new Error(msg));
  }
}
