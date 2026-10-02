#!/usr/bin/env python3
"""Teste de regressão: executa os casos de teste contra a API em execução (banco real).

Uso:  python tools/testar_api.py [URL_DA_API]      (padrão: http://localhost:5080)
No Windows, se "python" não for reconhecido, use "py tools/testar_api.py".
Sem dependências externas (só biblioteca padrão).
"""
import json, sys, urllib.request, urllib.error

API = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5080").rstrip("/")
AMB = {"Relacionamentos": 1, "Profissional": 2, "Acadêmico": 3}

def chamar(metodo, rota, corpo=None):
    req = urllib.request.Request(API + rota, method=metodo,
                                 data=json.dumps(corpo).encode() if corpo is not None else None,
                                 headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, json.loads(r.read())
    except urllib.error.HTTPError as e:
        return e.code, json.loads(e.read() or b"{}")
    except urllib.error.URLError:
        sys.exit(f"Não consegui falar com a API em {API}. Ela está rodando?")

st, cartas = chamar("GET", "/api/cartas")
if st != 200:
    sys.exit(f"A API respondeu HTTP {st} em /api/cartas — nada foi testado.\n"
             f"Motivo: {cartas.get('detail') or cartas.get('title') or cartas}")
ID = {c["nome"]: c["id"] for c in cartas}

# (caso, âmbito, cartas, fatos, regra final esperada, categoria esperada)
CASOS = [
    ("CT01", "Profissional", ["Os Enamorados", "A Torre", "O Sol"], None, "R23", "MUDANCA"),
    ("CT02", "Relacionamentos", ["A Sacerdotisa", "A Roda da Fortuna"], None, "R55", "CAUTELA"),
    ("CT03", "Acadêmico", ["O Hierofante", "A Sacerdotisa", "O Sol"], None, "R53", "FAVORAVEL"),
    ("CT04", "Relacionamentos", ["O Louco"], None, "R56", "NEUTRO"),
    ("CT05", "Profissional", ["O Eremita"], None, "R56", "NEUTRO"),
    ("CT06", "Acadêmico", ["A Torre", "A Morte"], None, "R52", "MUDANCA"),
    ("CT07", "Profissional", ["O Mago", "O Louco", "O Sol"], None, "R21", "FAVORAVEL"),
    ("CT08", "Relacionamentos", ["O Louco", "A Torre", "A Lua"], None, "R22", "CAUTELA"),
    ("CT09", "Relacionamentos", ["O Diabo", "A Temperança", "A Estrela"], None, "R24", "MUDANCA"),
    ("CT10", "Profissional", ["Os Enamorados", "O Diabo"], None, "R25", "CAUTELA"),
    ("CT11a", "Acadêmico", ["A Justiça"], {"HISTORICO_SOLIDO": True}, "R54", "FAVORAVEL"),
    ("CT11b", "Acadêmico", ["A Justiça"], {"HISTORICO_SOLIDO": False}, "R52", "MUDANCA"),
    ("CT12", "Profissional", ["O Sol", "A Roda da Fortuna", "O Hierofante"], None, "R31", "FAVORAVEL"),
    ("CT13", "Relacionamentos", ["A Torre", "O Louco", "O Sol"], None, "R30", "MUDANCA"),
]

falhas = 0
def reportar(caso, ok, msg):
    global falhas
    falhas += not ok
    print(f"{'OK   ' if ok else 'FALHA'} {caso:6} {msg}")

for caso, amb, cs, fatos, regra, cat in CASOS:
    corpo = {"ambitoId": AMB[amb], "tipoTiragemId": len(cs),
             "cartas": [{"posicaoId": i + 1, "cartaId": ID[c]} for i, c in enumerate(cs)], "fatos": fatos}
    st, r = chamar("POST", "/api/tiragens", corpo)
    if st != 201:
        reportar(caso, False, f"HTTP {st}: {r}"); continue
    obtido = (r["resultadoFinal"]["regraAplicada"], r["resultadoFinal"]["categoriaCodigo"])
    combos = ",".join(c["regra"] for c in r["combinacoes"]) or "—"
    reportar(caso, obtido == (regra, cat),
             f"{' / '.join(cs):45} → {r['resultadoFinal']['categoria']} ({obtido[0]}); combinações: {combos}"
             + ("" if obtido == (regra, cat) else f"  ESPERADO {regra} {cat}"))

# CT14 — entradas inválidas devem ser rejeitadas com 400
invalidos = [
    ("tipo 2 com carta em Resultado", {"ambitoId": 2, "tipoTiragemId": 2,
        "cartas": [{"posicaoId": 1, "cartaId": ID["O Louco"]}, {"posicaoId": 3, "cartaId": ID["O Sol"]}]}),
    ("carta repetida", {"ambitoId": 2, "tipoTiragemId": 2,
        "cartas": [{"posicaoId": 1, "cartaId": ID["O Sol"]}, {"posicaoId": 2, "cartaId": ID["O Sol"]}]}),
    ("A Justiça sem resposta", {"ambitoId": 3, "tipoTiragemId": 1, "cartas": [{"posicaoId": 1, "cartaId": ID["A Justiça"]}]}),
]
for nome, corpo in invalidos:
    st, _ = chamar("POST", "/api/tiragens", corpo)
    reportar("CT14", st == 400, f"{nome} → HTTP {st}")

st, regras = chamar("GET", "/api/regras")
codigos = [r["codigo"] for r in regras]
reportar("REGRAS", st == 200 and codigos == [f"R{i:02d}" for i in range(1, 57)], f"GET /api/regras → R01–R56 ({len(regras)} regras)")

total = len(CASOS) + len(invalidos) + 1
print(f"\n{total - falhas} de {total} verificações passaram.")
sys.exit(1 if falhas else 0)
