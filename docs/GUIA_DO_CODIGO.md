# Guia do código — para estudar antes da apresentação

Este guia explica o código na ordem em que uma tiragem passa por ele. Leia com o projeto aberto no VS Code. Os arquivos principais são cinco, e o mais importante é o `Motor.cs`.

## 1. O mapa do projeto

```
tarot-se/
├── database/                 o banco: 01_schema.sql (tabelas), 02_seed.sql (regras), 03_usuario.sql
├── tools/conhecimento/       a base de conhecimento em planilhas (de onde o 02_seed.sql é gerado)
├── backend/src/
│   ├── TarotSE.Domain/       entidades + MOTOR DE INFERÊNCIA (não conhece banco nem web)
│   ├── TarotSE.Application/  serviços: carregam a base, chamam o motor, montam a resposta
│   ├── TarotSE.Infrastructure/  acesso ao MySQL com Entity Framework (repositórios)
│   └── TarotSE.Api/          controllers (as rotas /api/...) e configuração
├── backend/tests/            testes automáticos do motor
└── frontend/src/app/         o site em Angular
```

A separação segue o RNF04: **Controller → Service → Repository → Domínio**. Cada projeto só conhece o de baixo:
a Api conhece a Application, a Application conhece o Domain, e o Domain não conhece ninguém.

## 2. O caminho de uma tiragem

| # | Arquivo | Método | O que faz |
|---|---|---|---|
| 1 | `frontend/.../draw-cards.component.ts` | `interpretar()` | Monta o JSON (âmbito, tipo, cartas por posição, resposta da Justiça) e faz o POST. |
| 2 | `frontend/.../core/api.service.ts` | `criarTiragem()` | Faz a chamada HTTP para `http://localhost:5080/api/tiragens`. |
| 3 | `backend/.../Api/Controllers/Controllers.cs` | `TiragensController.Criar` | Recebe o POST e chama o serviço. Não tem regra de negócio. |
| 4 | `backend/.../Application/Servicos.cs` | `TiragemService.RealizarAsync` | Converte ids em objetos, **lê a base de regras do banco**, chama o motor e grava a tiragem. |
| 5 | `backend/.../Domain/Motor/Motor.cs` | `MotorInferencia.Inferir` | Raciocina: valida, escolhe significados, avalia regras, resolve conflitos. |
| 6 | `backend/.../Infrastructure/Persistencia.cs` | `TiragemRepositorio.AdicionarAsync` | Salva no MySQL. |
| 7 | `frontend/.../pages/result/` | — | Mostra leitura, combinações e o rastro. |

## 3. O motor (`Motor.cs`) — o coração do trabalho

### `BaseConhecimento`
Guarda a base carregada do banco e separa as regras em dois grupos:
- `RegrasDeDecisao`: as que decidem a categoria (COMPOSTA e CONSOLIDACAO), **já ordenadas por prioridade**.
- `Observacoes`: as que só acrescentam texto.

No construtor ela também confere a base: dois significados para a mesma carta/âmbito/posição, ou duas regras com a mesma prioridade, geram erro. Isso impede uma base ambígua.

### `Inferir(...)` em 4 passos
1. **`Validar`** — as posições têm de ser exatamente as do tipo de tiragem (R27–R29), nenhuma carta pode repetir, e se tiver a Justiça precisa da resposta do histórico. Se algo falhar, lança `EntradaInvalidaException`, que vira HTTP 400.
2. **`InterpretarCarta`** — para cada carta:
   ```csharp
   var escolhido = bc.Significados
       .Where(s => s.AplicaSe(carta, ambito, posicao))   // os que valem para esta carta/âmbito/posição
       .OrderByDescending(s => s.Especificidade)          // do mais específico ao menos
       .FirstOrDefault();                                 // null = ficha resumida (R26)
   ```
   `Especificidade` está em `Entidades.cs`: âmbito vale 2 pontos e posição vale 1. Hoje todos os 126 significados têm âmbito e posição (3 pontos), mas o mecanismo aceita regras genéricas, como "Diabo em qualquer posição".
   A categoria de cada carta vem da ficha. Na Justiça, `CategoriaDaCarta` olha a resposta do usuário.
3. **Observações** — cada uma que for verdadeira entra na lista de combinações.
4. **Conflito** — percorre `RegrasDeDecisao`:
   ```csharp
   foreach (var regra in bc.RegrasDeDecisao)          // P1, P2, P3...
   {
       if (!Avaliar(...)) { rastro: "Condição falsa"; continue; }
       if (vencedora is null) vencedora = regra;     // a primeira verdadeira vence
       else rastro: "Também verdadeira, mas descartada";
   }
   ```
   As verdadeiras são o **conjunto de conflito**; a estratégia de resolução é **prioridade** (saliência). O motor não para na primeira de propósito: ele avalia todas para poder mostrar o rastro completo.

### `Avaliar(...)` — o interpretador das condições
Lê o JSON da coluna `condicao_json`:
- `{"todas": [...]}` → todas verdadeiras (E)
- `{"alguma": [...]}` → pelo menos uma (OU)
- `{"posicao": [...], "carta_em": [...]}` → existe uma dessas cartas nessas posições
- `{"categoria_em": [...]}` → existe carta com essa categoria
- `{"sempre": true}` → a regra padrão (R56)

É uma função recursiva: `todas` e `alguma` chamam `Avaliar` de novo para cada item.

## 4. Onde as regras ficam

| Regras | Tabela | Arquivo de origem |
|---|---|---|
| R01–R03 | `ambito` | `02_seed.sql` |
| R04–R06 | `posicao` | `02_seed.sql` |
| R27–R29 | `tipo_tiragem` + `tipo_tiragem_posicao` | `02_seed.sql` |
| R07–R20, R26 | `carta` + `significado` | `tools/conhecimento/cartas.csv` e `significados.csv` |
| R21–R25, R30–R56 | `regra` | `tools/conhecimento/regras.json` |

O `02_seed.sql` é **gerado** por `python tools/gerar_seed.py`. Os testes do motor leem as mesmas planilhas, então banco e testes nunca divergem.

## 5. Perguntas que o professor pode fazer

**Onde está o motor de inferência?**
Em `backend/src/TarotSE.Domain/Motor/Motor.cs`, método `Inferir`.

**É encadeamento para frente ou para trás?**
Para frente (*forward chaining*): parte dos fatos (âmbito, cartas, posições, resposta da Justiça) e dispara as regras até chegar à conclusão. Não parte de uma hipótese para provar.

**Como vocês resolvem quando duas regras disparam?**
Duas estratégias clássicas. Na escolha do significado, **especificidade** (a regra mais específica vence). Na categoria final, **prioridade**: todas as regras verdadeiras formam o conjunto de conflito e vence a de menor número de prioridade. A ordem segue a lógica do domínio: combinações antes de regras gerais, ruptura antes de alerta, alerta antes de favorável.

**E se nenhuma regra disparar?**
Impossível: a R56 tem condição `{"sempre": true}`. E o teste `Base_e_completa_e_deterministica...` roda as 55.440 tiragens de 3 cartas e confere que cada uma tem exatamente uma regra vencedora.

**Por que as regras estão no banco e não em if/else?**
Para separar o conhecimento do mecanismo de inferência, como num sistema especialista de verdade (RNF03). Mudar o significado de uma carta ou a prioridade de uma regra é mudar uma linha da tabela, sem recompilar.

**Mostra uma regra no banco.**
No MySQL: `SELECT codigo_regra, prioridade, condicao_json FROM regra ORDER BY prioridade;`
Ou no site, na tela **Base de regras**. Ou no Swagger: `GET /api/regras`.

**Como o sistema explica o raciocínio?**
O motor devolve o `rastro`: cada regra de decisão, se disparou, se foi descartada pela prioridade ou se era falsa. A tela de resultado mostra isso em "Como o sistema chegou a esta conclusão".

**Por que a Justiça faz uma pergunta?**
Porque a ficha diz que ela é Favorável se o histórico for sólido e Mudança Necessária se não for, e isso não está nas cartas. É um fato que só o usuário tem. O sistema só pergunta quando a carta aparece (`fato` na tabela `carta`).

**O que acontece com uma carta sem ficha completa?**
`InterpretarCarta` não encontra significado e usa a `descricao_geral` da carta, com a categoria da ficha resumida. É a R26 (fallback). Caso de teste CT05.

**Como vocês testaram?**
Dois níveis. `dotnet test` roda 23 testes do motor sem banco, incluindo todos os casos de teste e a verificação exaustiva. `python tools/testar_api.py` roda os casos contra a API com o MySQL de verdade.

**Por que Angular, ASP.NET e MySQL?**
Foi a arquitetura do documento: front-end, API e banco separados. O front-end pode ser trocado sem mexer no motor, e o motor pode ser testado sem front-end nem banco.

## 6. Para treinar

1. Abra o Swagger (http://localhost:5080/swagger), rode `POST /api/tiragens` com o exemplo do README e leia o `rastro` da resposta.
2. No MySQL (porta 3310), mude a prioridade da R21 para 0 e faça a tiragem do CT13 (A Torre / O Louco / O Sol, Relacionamentos). O resultado muda sem recompilar nada. Depois desfaça: `UPDATE regra SET prioridade = 9 WHERE codigo_regra = 'R21';`
3. Coloque um breakpoint em `Inferir` (Motor.cs) e siga uma tiragem passo a passo.
