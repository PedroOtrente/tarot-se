#!/usr/bin/env python3
"""Gera docs/REGRAS.md (e docs/REGRAS.docx, se o pandoc estiver instalado) com TODAS as regras,
a explicação e a justificativa de cada uma — a partir da mesma base que gera o banco.

    python tools/gerar_doc_regras.py
"""
import json, shutil, subprocess
from pathlib import Path
from kb import carregar, descrever, PASTA, NOMES_CATEGORIA, AMBITOS, POSICOES

cartas, sigs, regras, fatos = carregar()
just = json.loads((PASTA / "justificativas.json").read_text(encoding="utf-8"))
por_nome = {c["nome"]: c for c in cartas}
regra = {r["codigo"]: r for r in regras}
CASOS = {"R23": "CT01", "R55": "CT02", "R53": "CT03", "R56": "CT04 e CT05", "R52": "CT06 e CT11 (histórico: não)",
         "R21": "CT07", "R22": "CT08", "R24": "CT09", "R25": "CT10", "R54": "CT11 (histórico: sim)",
         "R31": "CT12", "R30": "CT13"}
L = []
add = L.append

def categoria_carta(c):
    if c["fato"]:
        return f"{NOMES_CATEGORIA[c['categoria']]} se o histórico for sólido; senão, {NOMES_CATEGORIA[c['categoria_se_falso']]}"
    return NOMES_CATEGORIA[c["categoria"]] + (f" ({c['nuance']})" if c["nuance"] else "")

def entao(r):
    if r["tipo"] == "OBSERVACAO":
        return f"acrescentar à leitura: \"{r['texto']}\""
    cat = NOMES_CATEGORIA[r["categoria"]] if r.get("categoria") else f"a categoria sugerida de {r['herda_de']}"
    return f"categoria final = {cat}" + (f", com {r['complemento']}" if r.get("complemento") else "")

add("# Base de regras — Sistema Especialista de Tarô\n")
add("Este documento lista as **56 regras** do sistema, de R01 a R56, com o que cada uma faz (explicação) e por que "
    "ela existe e está escrita assim (justificativa). Ele é gerado automaticamente a partir da mesma base que "
    "alimenta o banco de dados (`tools/conhecimento/`), então o que está aqui é exatamente o que o sistema executa.\n")
add("- **R01–R26** vêm do documento de especificação (Seção 8.2).")
add("- **R27–R56** foram criadas na implementação para cobrir o que o documento deixa em aberto: as posições de cada "
    "tipo de tiragem, as combinações citadas nas fichas da entrevista e a consolidação do resultado final.\n")

add("## Como o motor usa as regras\n")
add("1. **Contexto e posição (R01–R06, R27–R29).** O âmbito escolhido define o dicionário de significados, e o tipo "
    "de tiragem define quais posições existem.")
add("2. **Significado de cada carta (R07–R20, R26).** Cada carta recebe o significado da sua ficha para aquele âmbito "
    "e posição. Se a carta não tiver ficha completa, usa a ficha resumida (R26). Cada carta também traz uma "
    "**categoria sugerida**.")
add("3. **Combinações e consolidação (R21–R25, R30–R56).** O motor avalia todas essas regras. As **observações** só "
    "acrescentam texto. Das **regras de decisão**, as que forem verdadeiras formam o *conjunto de conflito*, e vence a "
    "de maior prioridade (P1 é a mais alta). Isso garante que toda tiragem tenha exatamente um resultado.\n")
add("### Ordem de prioridade das regras de decisão\n")
add("| Prioridade | Regra | Condição | Categoria |")
add("|---|---|---|---|")
for r in sorted((r for r in regras if r["tipo"] != "OBSERVACAO"), key=lambda r: r["prioridade"]):
    cat = NOMES_CATEGORIA[r["categoria"]] if r.get("categoria") else f"herdada de {r['herda_de']}"
    add(f"| P{r['prioridade']} | {r['codigo']} | {descrever(r['condicao'])} | {cat} |")
add("\nPor que essa ordem:\n")
add("- **Combinações antes de regras gerais.** Uma combinação de cartas é um conhecimento mais específico que a "
    "categoria de uma carta isolada, e em sistemas especialistas a regra mais específica tem preferência.")
add("- **Ruptura antes de alerta, alerta antes de favorável.** Se a tiragem mostra uma ruptura em curso, ignorá-la "
    "seria o erro mais grave; um alerta de cautela vem antes de uma promessa de sucesso pelo mesmo motivo.")
add("- **Regras com posição definida antes das sem posição.** \"A Torre no Movimento e o Sol no Resultado\" (R23) é "
    "mais preciso que \"Torre e Sol na tiragem\" (R30).\n")

add("## Camada 1 — Contexto\n")
for cod, nome in (("R01", "Relacionamentos"), ("R02", "Profissional"), ("R03", "Acadêmico")):
    add(f"### {cod} — Âmbito {nome}\n")
    add(f"**Regra:** SE âmbito = \"{nome}\" ENTÃO usar o dicionário de significados de {nome.lower()}.\n")
    add(f"**Justificativa:** {just[cod]}\n")

add("## Camada 2 — Posição e tipo de tiragem\n")
for cod, pos, txt in (("R04", "Significador", "ela representa a situação/pessoa no momento presente"),
                      ("R05", "Movimento", "ela representa a ação ou tendência em curso"),
                      ("R06", "Resultado", "ela representa o desfecho provável se a tendência atual continuar")):
    add(f"### {cod} — Posição {pos}\n")
    add(f"**Regra:** SE a carta está na posição \"{pos}\" ENTÃO {txt}.\n")
    add(f"**Justificativa:** {just[cod]}\n")
for cod, n, pos in (("R27", 1, "Significador"), ("R28", 2, "Significador, Movimento"), ("R29", 3, "Significador, Movimento, Resultado")):
    add(f"### {cod} — Tiragem de {n} carta{'s' if n > 1 else ''} (criada na implementação)\n")
    add(f"**Regra:** SE tipo de tiragem = {n} carta{'s' if n > 1 else ''} ENTÃO posições = [{pos}].\n")
    add(f"**Justificativa:** {just[cod]}\n")
add("Uma tiragem com posições diferentes dessas, com carta repetida ou com a Justiça sem a resposta sobre o "
    "histórico é recusada pela API (caso de teste CT14).\n")

add("## Camada 3 — Significado de cada carta\n")
add("Cada regra de R07 a R20 corresponde a um dos 14 arcanos aprofundados e reúne as 9 combinações de âmbito × posição "
    "da sua ficha (Seção 9.3), num total de 126 significados. A categoria sugerida é a mesma nas 9 combinações.\n")
for c in sorted((c for c in cartas if c["codigo_regra"]), key=lambda c: int(c["codigo_regra"][1:])):
    cod = c["codigo_regra"]
    add(f"### {cod} — {c['nome']}\n")
    add(f"**Regra:** SE carta = \"{c['nome']}\" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; "
        f"categoria sugerida = {categoria_carta(c)}.\n")
    add(f"**Explicação (significado geral):** {c['descricao_geral']}\n")
    add(f"**Justificativa da categoria:** {just[cod]}\n")
    add(f"**Fonte:** ficha 9.3.{int(cod[1:]) - 6}.\n")
    add("| Posição | " + " | ".join(AMBITOS) + " |")
    add("|---|---|---|---|")
    for p in POSICOES:
        linha = [next(s["texto"] for s in sigs if s["carta"] == c["nome"] and s["ambito"] == a and s["posicao"] == p) for a in AMBITOS]
        add(f"| **{p}** | " + " | ".join(linha) + " |")
    add("")
add("### R26 — Ficha resumida (fallback)\n")
add("**Regra:** SE não existir significado detalhado para a carta ENTÃO usar a ficha resumida (significado geral + "
    "categoria sugerida) como significado.\n")
add(f"**Justificativa:** {just['R26']}\n")
add("**Fonte:** Seção 9.4.\n")
add("| Arcano | Significado geral | Categoria sugerida |")
add("|---|---|---|")
for c in sorted((c for c in cartas if not c["codigo_regra"]), key=lambda c: int(c["numero"])):
    add(f"| {c['nome']} | {c['descricao_geral']} | {NOMES_CATEGORIA[c['categoria']]} |")
add("\nO Imperador (\"Favorável / Neutro, conforme o uso do poder\") ficou como Neutro, e o Julgamento (\"Mudança "
    "Necessária / Favorável, conforme o contexto\") ficou como Mudança Necessária, porque o sistema não tem como saber "
    "o uso do poder ou o contexto. O especialista do grupo deve confirmar.\n")

add("## Camada 4 — Combinações que decidem o resultado\n")
for r in sorted((r for r in regras if r["tipo"] == "COMPOSTA"), key=lambda r: r["prioridade"]):
    origem = "do documento" if int(r["codigo"][1:]) <= 26 else "criada a partir das fichas"
    add(f"### {r['codigo']} — prioridade P{r['prioridade']} ({origem})\n")
    add(f"**Regra:** SE {descrever(r['condicao'])} ENTÃO {entao(r)}.\n")
    add(f"**Explicação:** {r['texto']}\n")
    add(f"**Justificativa:** {r['justificativa']}\n")
    add(f"**Fonte:** {r['fonte']}." + (f" **Caso de teste:** {CASOS[r['codigo']]}." if r["codigo"] in CASOS else "") + "\n")

add("## Camada 4 — Combinações que só comentam a leitura (observações)\n")
add("As fichas citam outras combinações que mudam o *sentido* da leitura, mas não dizem para que lado elas empurram o "
    "resultado. Elas viraram observações: quando as duas cartas aparecem na tiragem, o texto entra na leitura, "
    "mas a categoria continua sendo decidida pelas regras de decisão.\n")
add("| Regra | SE (na mesma tiragem) | ENTÃO acrescenta à leitura | Fonte |")
add("|---|---|---|---|")
for r in sorted((r for r in regras if r["tipo"] == "OBSERVACAO"), key=lambda r: int(r["codigo"][1:])):
    add(f"| {r['codigo']} | {descrever(r['condicao']).replace(' na tiragem', '')} | {r['texto']} | {r['fonte']} |")
add("")

add("## Camada 5 — Consolidação\n")
add("Quando nenhuma combinação decide, estas regras transformam a tabela da Seção 8.3 em decisões com prioridade.\n")
for r in sorted((r for r in regras if r["tipo"] == "CONSOLIDACAO"), key=lambda r: r["prioridade"]):
    add(f"### {r['codigo']} — prioridade P{r['prioridade']}\n")
    add(f"**Regra:** SE {descrever(r['condicao'])} ENTÃO {entao(r)}.\n")
    add(f"**Explicação:** {r['texto']}\n")
    add(f"**Justificativa:** {r['justificativa']}\n")
    add(f"**Fonte:** {r['fonte']}." + (f" **Caso de teste:** {CASOS[r['codigo']]}." if r["codigo"] in CASOS else "") + "\n")

docs = Path(__file__).resolve().parent.parent / "docs"
md = docs / "REGRAS.md"
md.write_text("\n".join(L) + "\n", encoding="utf-8")
print(f"OK: {md}")
if shutil.which("pandoc"):
    subprocess.run(["pandoc", str(md), "-o", str(docs / "REGRAS.docx"), "--toc", "--toc-depth=2",
                    "-M", "toc-title=Sumário"], check=True)
    print(f"OK: {docs / 'REGRAS.docx'}")
