#!/usr/bin/env python3
"""Gera database/02_seed.sql a partir de tools/conhecimento/ (ETL de conhecimento — Seção 12).

    python tools/gerar_seed.py

Edite as planilhas em tools/conhecimento/ (abra os .csv no Excel, separador ';') e rode de novo.
Depois execute 01_schema.sql, 02_seed.sql e 03_usuario.sql no MySQL.
"""
import json
from pathlib import Path
from kb import carregar, descrever, AMBITOS, POSICOES, CATEGORIAS, NOMES_CATEGORIA, NUMERAIS, QUALQUER

def q(s): return "NULL" if s in (None, "") else "'" + str(s).replace("\\", "\\\\").replace("'", "''") + "'"

cartas, sigs, regras, fatos = carregar()
id_carta = {c["nome"]: int(c["numero"]) + 1 for c in cartas}
L = ["-- =====================================================================",
     "-- Sistema Especialista — Tarô | Carga inicial (seed)",
     "-- GERADO por tools/gerar_seed.py a partir de tools/conhecimento/. Não edite à mão.",
     "-- =====================================================================",
     "USE tarot_se;", "SET NAMES utf8mb4;", ""]

L += ["INSERT INTO ambito (id, codigo_regra, nome, descricao) VALUES",
      "(1,'R01','Relacionamentos','Dicionário R: interpretações voltadas a vínculos afetivos e familiares.'),",
      "(2,'R02','Profissional','Dicionário P: interpretações voltadas a carreira, trabalho e projetos.'),",
      "(3,'R03','Acadêmico','Dicionário A: interpretações voltadas a estudos, provas e formação.');", "",
      "INSERT INTO posicao (id, codigo_regra, nome, descricao) VALUES",
      "(1,'R04','Significador','Representa a situação/pessoa no momento presente.'),",
      "(2,'R05','Movimento','Representa a ação ou tendência em curso.'),",
      "(3,'R06','Resultado','Representa o desfecho provável se a tendência atual continuar.');", "",
      "INSERT INTO tipo_tiragem (id, codigo_regra, nome, qtd_cartas, descricao) VALUES",
      "(1,'R27','1 carta',1,'Visão geral: Significador.'),",
      "(2,'R28','2 cartas',2,'Situação → tendência: Significador e Movimento.'),",
      "(3,'R29','3 cartas',3,'Situação → movimento → resultado.');", "",
      "INSERT INTO tipo_tiragem_posicao (tipo_tiragem_id, posicao_id, ordem) VALUES",
      "(1,1,1), (2,1,1),(2,2,2), (3,1,1),(3,2,2),(3,3,3);", "",
      "INSERT INTO categoria_resultado (id, codigo, nome, descricao) VALUES",
      "(1,'FAVORAVEL','Favorável','O cenário tende a um desfecho positivo, alinhado ao que o consulente busca.'),",
      "(2,'CAUTELA','Cautela / Adiar','Momento não é ideal para decisões definitivas; recomenda-se esperar ou reunir mais informações antes de agir.'),",
      "(3,'MUDANCA','Mudança Necessária','O cenário exige uma ruptura ou mudança de rota para evoluir.'),",
      "(4,'NEUTRO','Neutro / Mais Informação','As cartas não indicam claramente um desfecho; recomenda-se nova tiragem ou reflexão adicional.');", ""]

L.append("INSERT INTO fato (codigo, pergunta, fonte) VALUES")
L.append(",\n".join(f"({q(f['codigo'])}, {q(f['pergunta'])}, {q(f['fonte'])})" for f in fatos) + ";\n")

L.append("-- Fichas dos 22 arcanos (Seções 9.3 e 9.4). aprofundada = TRUE para os 14 da Seção 9.3.")
L.append("INSERT INTO carta (id, nome, numero_arcano, numeral, codigo_regra, aprofundada, descricao_geral, "
         "categoria_sugerida_id, fato_codigo, categoria_se_falso_id, nuance_categoria) VALUES")
vals = []
for c in sorted(cartas, key=lambda c: int(c["numero"])):
    n = int(c["numero"])
    vals.append(f"({n + 1}, {q(c['nome'])}, {n}, '{NUMERAIS[n]}', {q(c['codigo_regra'])}, "
                f"{'TRUE' if c['aprofundada'] == 'sim' else 'FALSE'}, {q(c['descricao_geral'])}, "
                f"{CATEGORIAS[c['categoria']]}, {q(c['fato'])}, "
                f"{CATEGORIAS[c['categoria_se_falso']] if c['categoria_se_falso'] else 'NULL'}, {q(c['nuance'])})")
L.append(",\n".join(vals) + ";\n")

L.append(f"-- Camada 3 — {len(sigs)} significados (carta × âmbito × posição). Categoria = a da carta.")
L.append("INSERT INTO significado (carta_id, ambito_id, posicao_id, texto) VALUES")
vals = []
for s in sigs:
    amb = "NULL" if s["ambito"] == QUALQUER else AMBITOS[s["ambito"]]
    pos = "NULL" if s["posicao"] == QUALQUER else POSICOES[s["posicao"]]
    vals.append(f"({id_carta[s['carta']]}, {amb}, {pos}, {q(s['texto'])})")
L.append(",\n".join(vals) + ";\n")

L.append("-- Camadas 4 e 5 — combinações (COMPOSTA / OBSERVACAO) e consolidação, por prioridade.")
L.append("INSERT INTO regra (codigo_regra, tipo, descricao, prioridade, condicao_json, categoria_resultado_id, "
         "herda_categoria_de, texto, texto_complemento, fonte) VALUES")
vals = []
for r in sorted(regras, key=lambda r: int(r["codigo"][1:])):
    if r["tipo"] == "OBSERVACAO":
        entao = f"acrescentar à leitura: \"{r['texto']}\""
    elif r.get("categoria"):
        entao = f"categoria final = {NOMES_CATEGORIA[r['categoria']]}" + (f", {r['complemento']}" if r.get("complemento") else "")
    else:
        entao = f"categoria final = categoria sugerida de {r['herda_de']}" + (f", {r['complemento']}" if r.get("complemento") else "")
    desc = f"SE {descrever(r['condicao'])} ENTÃO {entao}."
    vals.append(f"({q(r['codigo'])}, {q(r['tipo'])}, {q(desc)}, {r.get('prioridade') or 'NULL'}, "
                f"{q(json.dumps(r['condicao'], ensure_ascii=False))}, "
                f"{CATEGORIAS[r['categoria']] if r.get('categoria') else 'NULL'}, {q(r.get('herda_de'))}, "
                f"{q(r['texto'])}, {q(r.get('complemento'))}, {q(r['fonte'])})")
L.append(",\n".join(vals) + ";")

destino = Path(__file__).resolve().parent.parent / "database" / "02_seed.sql"
destino.write_text("\n".join(L) + "\n", encoding="utf-8")
print(f"OK: {destino} — 22 cartas, {len(sigs)} significados, {len(regras)} regras de combinação/consolidação.")
