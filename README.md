# Sistema Especialista — Tarô

Um sistema especialista baseado em regras (SE…ENTÃO) que interpreta tiragens dos 22 Arcanos Maiores em três âmbitos: Relacionamentos, Profissional e Acadêmico.

Ele implementa o documento `docs/Trabalho_Pratico_Sistema_Especialista_Tarot.pdf`, a versão com as fichas da entrevista. O programa tem:

- 14 arcanos aprofundados, com 126 significados (3 âmbitos × 3 posições cada);
- 8 arcanos com ficha resumida, que entram pelo fallback R26;
- 56 regras numeradas: R01–R26 do documento e R27–R56 acrescentadas pelo programa;
- 4 categorias de resultado.

> O documento não define algumas coisas, como a precedência entre regras e a pergunta da Justiça, e o programa precisou decidir. Elas estão em **`docs/AJUSTES_NO_DOCUMENTO.md`**. Leia antes da apresentação.

| Camada (RNF04) | Projeto | Tecnologia |
|---|---|---|
| Apresentação | `frontend/` | Angular 18 |
| API / Controllers | `backend/src/TarotSE.Api` | ASP.NET Core 8 + Swagger |
| Services | `backend/src/TarotSE.Application` | C# |
| Domínio / Motor de inferência | `backend/src/TarotSE.Domain` | C# puro |
| Repository / Persistência | `backend/src/TarotSE.Infrastructure` + `database/` | EF Core (Pomelo) + MySQL 8 |

## Como rodar — opção 1: tudo no Docker (recomendado)

Precisa só do [Docker Desktop](https://www.docker.com/products/docker-desktop/) ligado. Na pasta do projeto:

```
docker compose up -d --build
```

A primeira vez demora alguns minutos (baixa as imagens e compila). Depois:

| O quê | Endereço |
|---|---|
| Site | http://localhost:4201 |
| API (Swagger) | http://localhost:5080/swagger |
| MySQL (Workbench) | `localhost`, porta **3310**, usuário `tarot`, senha `tarot123` (root: `root123`) |

O MySQL fica na porta **3310** para não brigar com outros MySQL da máquina (3306, 3307).

| Comando | Para quê |
|---|---|
| `docker compose down` | Desliga tudo (os dados ficam guardados) |
| `docker compose down -v` | Desliga e **apaga o banco**. Use sempre que os arquivos de `database/` mudarem: os scripts só rodam quando o banco é criado |
| `docker compose up -d --build` | Liga de novo (recompila se o código mudou) |
| `docker compose logs api` | Mostra o log da API |

## Como rodar — opção 2: sem Docker (Windows)

Pré-requisitos: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), [Node.js LTS](https://nodejs.org/) e um MySQL 8.

1. **Banco.** No Workbench, clique na conexão primeiro (senão o raio ⚡ fica apagado) e rode, nesta ordem, `database/01_schema.sql`, `02_seed.sql` e `03_usuario.sql`. Rode de novo sempre que baixar uma versão nova do projeto.
2. **Porta.** A API procura o MySQL na porta **3310** (a do Docker). Se o seu MySQL estiver em outra porta, troque `Port=3310` em `backend/src/TarotSE.Api/appsettings.json`.
3. **API** (terminal 1, na pasta `backend`): `dotnet run --project src/TarotSE.Api` → http://localhost:5080/swagger
4. **Site** (terminal 2, na pasta `frontend`): `npm install` (só na primeira vez) e `npm start` → http://localhost:4201

Ao subir, a API confere o banco e escreve no log `Banco OK` ou, se algo estiver errado, uma linha `ATENÇÃO` dizendo o que fazer. A mesma explicação aparece no site, no lugar de "Erro inesperado".

## Testes

```
cd backend
dotnet test                      # 23 testes do motor, sem banco (inclui as 55.440 tiragens de 3 cartas)

python tools/testar_api.py       # 18 verificações (no Windows: py tools/testar_api.py) contra a API rodando com o MySQL real
```

Os testes do motor leem a mesma base de `tools/conhecimento/` que gera o banco. Por isso, os testes e o banco nunca ficam diferentes.

## Base de conhecimento: onde editar

Toda a base está em **`tools/conhecimento/`**. Os arquivos `.csv` abrem no Excel (separador `;`).

| Arquivo | Conteúdo | Seção do documento |
|---|---|---|
| `cartas.csv` | Os 22 arcanos: código da regra, se é aprofundado, categoria sugerida, nuance e significado geral (usado pelo fallback) | 9.3 e 9.4 |
| `significados.csv` | Os 126 significados (carta × âmbito × posição) | 9.3 |
| `regras.json` | Combinações (R21–R25, R30–R51), consolidação (R52–R56), prioridades, a pergunta da Justiça e a justificativa de cada regra | 8.2, 8.3 e 9.3 |
| `justificativas.json` | Justificativas das regras R01–R20 e R26–R29 | — |

Depois de editar:
```
python tools/gerar_seed.py      # valida a base e regera database/02_seed.sql
python tools/cobertura.py       # mostra o que cada arcano cobre e o que cai no fallback
```
Depois, rode os 3 scripts SQL de novo no Workbench.

Para aprofundar um dos 8 arcanos de ficha resumida:
1. Em `cartas.csv`, dê a ele um código (por exemplo, `R57`) e marque `aprofundada` como `sim`.
2. Acrescente as 9 linhas dele em `significados.csv`.

## Como o motor de inferência funciona

O código está em `backend/src/TarotSE.Domain/Motor/Motor.cs`.

1. **Validação (R27–R29).** As posições informadas precisam ser exatamente as do tipo de tiragem. Nenhuma carta pode se repetir. Se a tiragem tiver A Justiça, a resposta sobre o histórico é obrigatória.
2. **Camada 3: especificidade.** Cada carta recebe o significado mais específico para o âmbito e a posição (R07–R20). Se não houver, recebe a ficha resumida (R26). A categoria vem da ficha da carta. No caso da Justiça, depende da resposta do usuário.
3. **Combinações e consolidação.** O motor avalia todas as regras:
   - as **observações** (R36–R51) só acrescentam texto à leitura;
   - as **regras de decisão** (R21–R25, R30–R35 e R52–R56) que forem verdadeiras formam o **conjunto de conflito**, e vence a de maior prioridade (P1 a P16).
4. **Explicação.** A tela de resultado mostra as combinações identificadas e, em "Como o sistema chegou a esta conclusão", cada regra avaliada: a que disparou, as descartadas pela prioridade e as falsas.

A base é lida do banco a cada tiragem. Alterar as tabelas muda o comportamento sem recompilar nada (RNF03).

### Gramática de `condicao_json`
```jsonc
{"todas":  [ ... ]}                                            // E lógico
{"alguma": [ ... ]}                                            // OU lógico
{"posicao": ["Movimento"], "carta_em": ["A Torre"]}           // carta nessa posição
{"carta_em": ["O Diabo"]}                                      // carta em qualquer posição
{"posicao": ["Resultado"], "categoria_em": ["FAVORAVEL"]}      // pela categoria sugerida
{"sempre": true}                                               // regra padrão
```

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/ambitos` | Âmbitos (R01–R03) |
| GET | `/api/tipos-tiragem` | Tipos de tiragem e suas posições (R27–R29) |
| GET | `/api/cartas` | Os 22 arcanos, com a categoria sugerida e a pergunta, quando houver |
| POST | `/api/tiragens` | Executa o motor e devolve a interpretação, as combinações e o rastro. Retorna 400 se a entrada for inválida. |
| GET | `/api/tiragens` | Histórico |
| GET | `/api/tiragens/{id}` | Uma tiragem registrada |
| GET | `/api/regras` | Base completa (R01–R56), com as 9 combinações de cada carta |

Exemplo de pedido com A Justiça:
```json
{ "ambitoId": 3, "tipoTiragemId": 1, "cartas": [ { "posicaoId": 1, "cartaId": 12 } ], "fatos": { "HISTORICO_SOLIDO": true } }
```
Os ids das cartas são o número do arcano + 1 (O Louco = 1, O Sol = 20). Os ids das posições são: 1 Significador, 2 Movimento, 3 Resultado.

## Documentos

| Arquivo | Conteúdo |
|---|---|
| `docs/REGRAS.md` / `docs/REGRAS.docx` | As 56 regras, com explicação e justificativa de cada uma (gerado por `python tools/gerar_doc_regras.py`) |
| `docs/GUIA_DO_CODIGO.md` | Guia para estudar o código, com as perguntas prováveis do professor |
| `docs/AJUSTES_NO_DOCUMENTO.md` | O que o programa decidiu onde o documento é omisso |
| `docs/Trabalho_Pratico_Sistema_Especialista_Tarot.pdf` | Documento de especificação |

## Problemas comuns

| Sintoma | Causa |
|---|---|
| "O banco está desatualizado..." / `Table 'tarot_se.fato' doesn't exist` | O banco é de uma versão antiga. Sem Docker: rode os 3 scripts de novo. Com Docker: `docker compose down -v` e `docker compose up -d --build`. |
| "Não consegui conectar no MySQL" | MySQL desligado ou porta errada no `appsettings.json` (o padrão é 3310). |
| "Não foi possível falar com a API" | A API não está rodando. |
| O raio do Workbench está apagado | O script foi aberto sem conexão. Conecte primeiro e depois abra o script. |
| `testar_api.py` diz "nada foi testado" | A API respondeu com erro; o motivo aparece na mesma mensagem. |
| "Port ... is already in use" | Outro programa usa a porta. No `docker-compose.yml`, troque o número da esquerda (ex.: `"4202:80"`). |
