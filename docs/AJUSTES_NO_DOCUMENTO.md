# Ajustes para o documento ficar igual ao programa

O programa segue o documento (versão com as fichas da entrevista). Os pontos abaixo são o que o
**programa precisou decidir** porque o documento não define, ou define de forma contraditória.
Vale incorporar no documento antes da validação com o professor.

## 1. Regras novas (R27–R56)

O documento numera R01–R26. O programa mantém essa numeração e acrescenta:

| Códigos | O que são | De onde vêm |
|---|---|---|
| R27–R29 | Posições de cada tipo de tiragem: 1 carta = Significador; 2 cartas = Significador + Movimento; 3 cartas = as três | RF03 e Seção 7.2 |
| R30–R35 | Combinações que mudam a categoria (Torre/Morte + Sol/Estrela, Roda + Sol/carta favorável, Roda + Torre, Roda + Justiça, Lua + Louco/Sacerdotisa, Sol + Lua) | "Regras compostas" das fichas 9.3 |
| R36–R51 | Combinações que só acrescentam texto à leitura (observações), uma para cada combinação citada nas fichas | "Regras compostas" das fichas 9.3 |
| R52–R56 | Consolidação geral: transforma a tabela 8.3 em regras com prioridade | Seção 8.3 |

A lista completa, com o texto SE...ENTÃO de cada uma, aparece na tela **Base de regras** do sistema e em `GET /api/regras`.

## 2. Precedência entre categorias (o documento não define)

Uma tiragem pode disparar regras de categorias diferentes. No CT01, por exemplo, A Torre puxa para
Mudança Necessária e O Sol para Favorável. O programa avalia todas as regras de decisão. As que são
verdadeiras formam o **conjunto de conflito**, e vence a de maior prioridade:

| Prioridade | Regra | Categoria |
|---|---|---|
| P1 | R23 — Torre em Movimento + Sol em Resultado | Mudança (tendência positiva) |
| P2 | R30 — Torre/Morte + Sol/Estrela na tiragem | Mudança (tendência positiva) |
| P3 | R24 — Diabo + Estrela | Mudança (de origem interna, com tendência de cura) |
| P4 | R32 — Roda + Torre | Mudança |
| P5 | R22 — Louco + Torre | Cautela |
| P6 | R25 — Enamorados + Diabo | Cautela |
| P7 | R34 — Lua + Louco/Sacerdotisa | Cautela |
| P8 | R35 — Sol + Lua | Cautela |
| P9 | R21 — Louco em Movimento + Sol em Resultado | Favorável |
| P10 | R33 — Roda + Justiça | herda a da Justiça |
| P11 | R31 — Roda + carta favorável | Favorável |
| P12 | R52 — carta de Mudança em Significador/Movimento | Mudança |
| P13 | R53 — carta de Resultado (se não for Neutro) | herda a do Resultado |
| P14 | R54 — carta favorável na tiragem | Favorável |
| P15 | R55 — carta de cautela na tiragem | Cautela |
| P16 | R56 — padrão | Neutro |

Os princípios por trás dessa ordem: combinações vêm antes das regras gerais; dentro delas, ruptura vem
antes de alerta, e alerta vem antes de favorável; regras com posição definida vêm antes das sem posição.
O especialista do grupo deve confirmar essa ordem.

## 3. Decisões tomadas onde o documento é ambíguo

- **A Justiça (R13)** é "Favorável se histórico sólido, senão Mudança Necessária". Como o histórico não
  é uma entrada, o sistema **pergunta ao usuário** só quando A Justiça sai na tiragem: "Você considera que
  seu histórico/esforço nesta situação é sólido?". Isso precisa entrar na Seção 7.2 como uma pergunta
  condicional.
- **O Imperador** ("Favorável / Neutro, conforme o uso do poder") ficou como **Neutro**.
- **O Julgamento** ("Mudança Necessária / Favorável, conforme o contexto") ficou como **Mudança Necessária**.
- **R24 (Diabo + Estrela)** não tem categoria no documento. Ficou **Mudança Necessária, de origem interna,
  com tendência de cura**.
- **Células "Idem"** das fichas foram escritas por extenso: Louco/Resultado (Profissional e Acadêmico),
  Roda/Resultado (Profissional e Acadêmico), Diabo/Resultado (Profissional e Acadêmico) e
  Estrela/Movimento (Profissional e Acadêmico). Os textos estão em `tools/conhecimento/significados.csv`.
- **Categoria por carta:** cada carta usa a mesma categoria sugerida nas 9 combinações, a da ficha.

## 4. Contradições que continuam no documento

- **Seção 11 (testes):** o CT02 (só Movimento) e o CT03 (só Resultado) não são tiragens possíveis, porque
  as posições são fixas por tipo. O programa usa tiragens válidas: veja a lista abaixo.
- **Seção 9.5:** o texto de Os Enamorados ("decisão importante envolvendo pessoas ou parcerias de trabalho")
  não é o da ficha 9.3.4. O programa usa a ficha.
- **Seção 13 (checklist):** diz "17 regras formalizadas". O documento tem 26, e o programa tem 56.
- **Seção 8.1:** fala em 3 camadas, mas as regras de combinação e de consolidação funcionam como uma
  quarta camada.

## 5. Casos de teste que o programa roda

| Caso | Âmbito | Cartas (Sig. / Mov. / Res.) | Resultado (regra) |
|---|---|---|---|
| CT01 | Profissional | Os Enamorados / A Torre / O Sol | Mudança, tendência positiva (R23) |
| CT02 | Relacionamentos | A Sacerdotisa / A Roda da Fortuna | Cautela (R55) |
| CT03 | Acadêmico | O Hierofante / A Sacerdotisa / O Sol | Favorável (R53) |
| CT04 | Relacionamentos | O Louco | Neutro (R56) |
| CT05 | Profissional | O Eremita (ficha resumida) | Neutro (R56), fallback R26 sem erro |
| CT06 | Acadêmico | A Torre / A Morte | Mudança (R52) |
| CT07 | Profissional | O Mago / O Louco / O Sol | Favorável (R21) |
| CT08 | Relacionamentos | O Louco / A Torre / A Lua | Cautela (R22) |
| CT09 | Relacionamentos | O Diabo / A Temperança / A Estrela | Mudança (R24) |
| CT10 | Profissional | Os Enamorados / O Diabo | Cautela (R25) |
| CT11 | Acadêmico | A Justiça — histórico sólido: sim / não | Favorável (R54) / Mudança (R52) |
| CT12 | Profissional | O Sol / A Roda da Fortuna / O Hierofante | Favorável (R31) |
| CT13 | Relacionamentos | A Torre / O Louco / O Sol | Mudança (R30 vence R22 e R21) |
| CT14 | — | posição errada, carta repetida, Justiça sem resposta | recusado (HTTP 400) |
