"""Leitura e validação da base de conhecimento em tools/conhecimento/ (fonte única do seed e dos testes)."""
import json, sys
from pathlib import Path

PASTA = Path(__file__).with_name("conhecimento")
AMBITOS = {"Relacionamentos": 1, "Profissional": 2, "Acadêmico": 3}
POSICOES = {"Significador": 1, "Movimento": 2, "Resultado": 3}
CATEGORIAS = {"FAVORAVEL": 1, "CAUTELA": 2, "MUDANCA": 3, "NEUTRO": 4}
NOMES_CATEGORIA = {"FAVORAVEL": "Favorável", "CAUTELA": "Cautela / Adiar", "MUDANCA": "Mudança Necessária", "NEUTRO": "Neutro / Mais Informação"}
QUALQUER = "qualquer"
NUMERAIS = ["0", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV",
            "XVI", "XVII", "XVIII", "XIX", "XX", "XXI"]

def _csv(nome, colunas):
    """CSV com ';'. A última coluna (texto) pode conter ';' — por isso o split é limitado."""
    linhas = (PASTA / nome).read_text(encoding="utf-8-sig").splitlines()
    cab = linhas[0].split(";")
    assert cab == colunas, f"{nome}: cabeçalho esperado {colunas}, encontrado {cab}"
    saida = []
    for n, l in enumerate(linhas[1:], start=2):
        if not l.strip():
            continue
        partes = [p.strip() for p in l.split(";", len(colunas) - 1)]
        if len(partes) != len(colunas):
            sys.exit(f"{nome}, linha {n}: esperava {len(colunas)} colunas")
        saida.append(dict(zip(colunas, partes), _linha=n))
    return saida

def carregar():
    erros = []
    cartas = _csv("cartas.csv", ["numero", "nome", "codigo_regra", "aprofundada", "categoria", "fato",
                                 "categoria_se_falso", "nuance", "descricao_geral"])
    sigs = _csv("significados.csv", ["carta", "ambito", "posicao", "texto"])
    regras_doc = json.loads((PASTA / "regras.json").read_text(encoding="utf-8"))
    fatos = {f["codigo"]: f for f in regras_doc["fatos"]}
    nomes = {c["nome"]: c for c in cartas}

    if sorted(int(c["numero"]) for c in cartas) != list(range(22)):
        erros.append("cartas.csv precisa ter os 22 arcanos (0 a 21)")
    for c in cartas:
        if c["categoria"] not in CATEGORIAS: erros.append(f"cartas.csv linha {c['_linha']}: categoria inválida")
        if c["fato"] and c["fato"] not in fatos: erros.append(f"cartas.csv linha {c['_linha']}: fato desconhecido")
        if bool(c["fato"]) != bool(c["categoria_se_falso"]): erros.append(f"cartas.csv linha {c['_linha']}: fato e categoria_se_falso andam juntos")
        if c["aprofundada"] not in ("sim", "nao"): erros.append(f"cartas.csv linha {c['_linha']}: aprofundada deve ser sim/nao")

    vistos = {}
    for s in sigs:
        if s["carta"] not in nomes: erros.append(f"significados.csv linha {s['_linha']}: carta desconhecida '{s['carta']}'"); continue
        if not nomes[s["carta"]]["codigo_regra"]: erros.append(f"significados.csv linha {s['_linha']}: '{s['carta']}' não tem codigo_regra em cartas.csv")
        if s["ambito"] != QUALQUER and s["ambito"] not in AMBITOS: erros.append(f"significados.csv linha {s['_linha']}: âmbito inválido")
        if s["posicao"] != QUALQUER and s["posicao"] not in POSICOES: erros.append(f"significados.csv linha {s['_linha']}: posição inválida")
        chave = (s["carta"], s["ambito"], s["posicao"])
        if chave in vistos: erros.append(f"significados.csv linha {s['_linha']}: repete a linha {vistos[chave]} (mesma especificidade)")
        vistos[chave] = s["_linha"]

    regras = regras_doc["regras"]
    def checar(cond, cod):
        if "sempre" in cond: return
        if "todas" in cond or "alguma" in cond:
            for c in cond.get("todas", cond.get("alguma")): checar(c, cod)
            return
        for p in cond.get("posicao", []):
            if p not in POSICOES: erros.append(f"{cod}: posição desconhecida '{p}'")
        for n in cond.get("carta_em", []):
            if n not in nomes: erros.append(f"{cod}: carta desconhecida '{n}'")
        for k in cond.get("categoria_em", []):
            if k not in CATEGORIAS: erros.append(f"{cod}: categoria desconhecida '{k}'")
        if "carta_em" not in cond and "categoria_em" not in cond: erros.append(f"{cod}: condição sem carta_em/categoria_em")
    prioridades = [r["prioridade"] for r in regras if r["tipo"] != "OBSERVACAO"]
    if len(set(prioridades)) != len(prioridades): erros.append("regras.json: prioridades repetidas")
    if not any(r.get("condicao") == {"sempre": True} for r in regras): erros.append("regras.json: falta a regra padrão {sempre:true}")
    for r in regras:
        checar(r["condicao"], r["codigo"])
        if r["tipo"] != "OBSERVACAO" and not r.get("categoria") and not r.get("herda_de"):
            erros.append(f"{r['codigo']}: regra sem categoria precisa de herda_de")
    if erros:
        sys.exit("Base de conhecimento inválida:\n  " + "\n  ".join(erros))
    return cartas, sigs, regras, list(fatos.values())

def descrever(cond):
    """Escreve a condição como texto SE... (para a coluna regra.descricao)."""
    if cond.get("sempre"): return "nenhuma regra de maior prioridade se aplicar"
    if "todas" in cond: return " E ".join(descrever(c) for c in cond["todas"])
    if "alguma" in cond: return "(" + " OU ".join(descrever(c) for c in cond["alguma"]) + ")"
    onde = "em " + " ou ".join(cond["posicao"]) if cond.get("posicao") else "na tiragem"
    if "carta_em" in cond:
        cs = cond["carta_em"]
        return f"{cs[0]} {onde}" if len(cs) == 1 else f"{' ou '.join(cs)} {onde}"
    cats = " ou ".join(NOMES_CATEGORIA[k] for k in cond["categoria_em"])
    return f"carta com categoria {cats} {onde}"
