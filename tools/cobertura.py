#!/usr/bin/env python3
"""Conferência da base (Seção 12): mostra, para cada arcano, quantas das 9 combinações âmbito × posição
têm significado próprio e quais caem no fallback R26 (ficha resumida).

    python tools/cobertura.py
"""
from kb import carregar, AMBITOS, POSICOES, QUALQUER

cartas, sigs, regras, _ = carregar()
cobre = lambda s, a, p: s["ambito"] in (QUALQUER, a) and s["posicao"] in (QUALQUER, p)
total = 0
print(f"{'Arcano':20} {'Regra':6} {'Cobertas':>8}   Situação")
for c in sorted(cartas, key=lambda c: int(c["numero"])):
    n = sum(any(s["carta"] == c["nome"] and cobre(s, a, p) for s in sigs) for a in AMBITOS for p in POSICOES)
    total += n
    situacao = "completa" if n == 9 else ("fallback R26 (ficha resumida)" if n == 0 else f"faltam {9 - n}")
    print(f"{c['nome']:20} {c['codigo_regra'] or '—':6} {n:>5}/9   {situacao}")
print(f"\nTotal: {total}/198 combinações com significado próprio; o restante usa a ficha resumida (R26).")
print(f"Regras de combinação/consolidação: {sum(r['tipo'] != 'OBSERVACAO' for r in regras)} com categoria, "
      f"{sum(r['tipo'] == 'OBSERVACAO' for r in regras)} observações.")
