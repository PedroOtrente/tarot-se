# Base de regras — Sistema Especialista de Tarô

Este documento lista as **56 regras** do sistema, de R01 a R56, com o que cada uma faz (explicação) e por que ela existe e está escrita assim (justificativa). Ele é gerado automaticamente a partir da mesma base que alimenta o banco de dados (`tools/conhecimento/`), então o que está aqui é exatamente o que o sistema executa.

- **R01–R26** vêm do documento de especificação (Seção 8.2).
- **R27–R56** foram criadas na implementação para cobrir o que o documento deixa em aberto: as posições de cada tipo de tiragem, as combinações citadas nas fichas da entrevista e a consolidação do resultado final.

## Como o motor usa as regras

1. **Contexto e posição (R01–R06, R27–R29).** O âmbito escolhido define o dicionário de significados, e o tipo de tiragem define quais posições existem.
2. **Significado de cada carta (R07–R20, R26).** Cada carta recebe o significado da sua ficha para aquele âmbito e posição. Se a carta não tiver ficha completa, usa a ficha resumida (R26). Cada carta também traz uma **categoria sugerida**.
3. **Combinações e consolidação (R21–R25, R30–R56).** O motor avalia todas essas regras. As **observações** só acrescentam texto. Das **regras de decisão**, as que forem verdadeiras formam o *conjunto de conflito*, e vence a de maior prioridade (P1 é a mais alta). Isso garante que toda tiragem tenha exatamente um resultado.

### Ordem de prioridade das regras de decisão

| Prioridade | Regra | Condição | Categoria |
|---|---|---|---|
| P1 | R23 | A Torre em Movimento E O Sol em Resultado | Mudança Necessária |
| P2 | R30 | A Torre ou A Morte na tiragem E O Sol ou A Estrela na tiragem | Mudança Necessária |
| P3 | R24 | O Diabo na tiragem E A Estrela na tiragem | Mudança Necessária |
| P4 | R32 | A Roda da Fortuna na tiragem E A Torre na tiragem | Mudança Necessária |
| P5 | R22 | O Louco na tiragem E A Torre na tiragem | Cautela / Adiar |
| P6 | R25 | Os Enamorados na tiragem E O Diabo na tiragem | Cautela / Adiar |
| P7 | R34 | A Lua na tiragem E O Louco ou A Sacerdotisa na tiragem | Cautela / Adiar |
| P8 | R35 | O Sol na tiragem E A Lua na tiragem | Cautela / Adiar |
| P9 | R21 | O Louco em Movimento E O Sol em Resultado | Favorável |
| P10 | R33 | A Roda da Fortuna na tiragem E A Justiça na tiragem | herdada de A Justiça |
| P11 | R31 | A Roda da Fortuna na tiragem E carta com categoria Favorável na tiragem | Favorável |
| P12 | R52 | carta com categoria Mudança Necessária em Significador ou Movimento | Mudança Necessária |
| P13 | R53 | carta com categoria Favorável ou Cautela / Adiar ou Mudança Necessária em Resultado | herdada de Resultado |
| P14 | R54 | carta com categoria Favorável na tiragem | Favorável |
| P15 | R55 | carta com categoria Cautela / Adiar na tiragem | Cautela / Adiar |
| P16 | R56 | nenhuma regra de maior prioridade se aplicar | Neutro / Mais Informação |

Por que essa ordem:

- **Combinações antes de regras gerais.** Uma combinação de cartas é um conhecimento mais específico que a categoria de uma carta isolada, e em sistemas especialistas a regra mais específica tem preferência.
- **Ruptura antes de alerta, alerta antes de favorável.** Se a tiragem mostra uma ruptura em curso, ignorá-la seria o erro mais grave; um alerta de cautela vem antes de uma promessa de sucesso pelo mesmo motivo.
- **Regras com posição definida antes das sem posição.** "A Torre no Movimento e o Sol no Resultado" (R23) é mais preciso que "Torre e Sol na tiragem" (R30).

## Camada 1 — Contexto

### R01 — Âmbito Relacionamentos

**Regra:** SE âmbito = "Relacionamentos" ENTÃO usar o dicionário de significados de relacionamentos.

**Justificativa:** O mesmo arcano tem leituras diferentes em cada área da vida (ex.: a Torre é demissão no trabalho e crise no relacionamento). Escolher o âmbito primeiro define qual conjunto de significados vale para a tiragem inteira.

### R02 — Âmbito Profissional

**Regra:** SE âmbito = "Profissional" ENTÃO usar o dicionário de significados de profissional.

**Justificativa:** Idem à R01, para carreira, trabalho e projetos.

### R03 — Âmbito Acadêmico

**Regra:** SE âmbito = "Acadêmico" ENTÃO usar o dicionário de significados de acadêmico.

**Justificativa:** Idem à R01, para estudos, provas e formação.

## Camada 2 — Posição e tipo de tiragem

### R04 — Posição Significador

**Regra:** SE a carta está na posição "Significador" ENTÃO ela representa a situação/pessoa no momento presente.

**Justificativa:** Na tiragem de três posições usada pelo grupo, o Significador é a situação atual: o ponto de partida da leitura.

### R05 — Posição Movimento

**Regra:** SE a carta está na posição "Movimento" ENTÃO ela representa a ação ou tendência em curso.

**Justificativa:** O Movimento é a tendência em curso: o que está acontecendo ou precisa acontecer. Várias regras compostas dependem dessa posição (R21, R23).

### R06 — Posição Resultado

**Regra:** SE a carta está na posição "Resultado" ENTÃO ela representa o desfecho provável se a tendência atual continuar.

**Justificativa:** O Resultado é o desfecho provável se nada mudar. Por isso a R53 usa a carta dessa posição para decidir a categoria quando nada mais decide.

### R27 — Tiragem de 1 carta (criada na implementação)

**Regra:** SE tipo de tiragem = 1 carta ENTÃO posições = [Significador].

**Justificativa:** Uma carta só dá uma visão geral, então ocupa a posição Significador (situação atual). Não há Resultado: por isso uma tiragem de 1 carta só fica Favorável, Cautela ou Mudança pela categoria da própria carta (R52, R54, R55).

### R28 — Tiragem de 2 cartas (criada na implementação)

**Regra:** SE tipo de tiragem = 2 cartas ENTÃO posições = [Significador, Movimento].

**Justificativa:** Duas cartas descrevem situação → tendência (Seção 7.2), ou seja, Significador e Movimento.

### R29 — Tiragem de 3 cartas (criada na implementação)

**Regra:** SE tipo de tiragem = 3 cartas ENTÃO posições = [Significador, Movimento, Resultado].

**Justificativa:** Três cartas cobrem situação → movimento → resultado, a tiragem completa do documento.

Uma tiragem com posições diferentes dessas, com carta repetida ou com a Justiça sem a resposta sobre o histórico é recusada pela API (caso de teste CT14).

## Camada 3 — Significado de cada carta

Cada regra de R07 a R20 corresponde a um dos 14 arcanos aprofundados e reúne as 9 combinações de âmbito × posição da sua ficha (Seção 9.3), num total de 126 significados. A categoria sugerida é a mesma nas 9 combinações.

### R07 — O Louco

**Regra:** SE carta = "O Louco" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Neutro / Mais Informação.

**Explicação (significado geral):** Arquétipo do início absoluto: potencial puro ainda não testado pela realidade. Representa fé no desconhecido, coragem e espontaneidade — mas também a sombra da imprudência se agir sem nenhum discernimento.

**Justificativa da categoria:** Neutro / Mais Informação: segundo a ficha, o Louco é uma carta de movimento, que não empurra sozinha para um resultado bom ou ruim. O sentido vem das cartas ao lado (R21, R22, R34, R36, R50).

**Fonte:** ficha 9.3.1.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Pessoa sem muita experiência afetiva; contexto pede iniciar a jornada para ganhar vivência. | Pessoa sem muita experiência no cargo/mercado; está construindo a bagagem, não usando uma já existente. | Estudante iniciante numa área/matéria nova, ainda sem método consolidado. |
| **Movimento** | É preciso se expor emocionalmente sem garantias para a relação avançar. | Pede decisão de arriscar (aceitar proposta, trocar de emprego) mesmo sem certeza do resultado. | Pede mudar de abordagem de estudo ou tentar algo novo mesmo sem garantia de sucesso. |
| **Resultado** | Não é carta natural de Resultado; se aparecer, indica começo inesperado e rota sem garantias. | Novo começo que só se revela depois da decisão tomada. | Necessidade de dar o salto rumo ao desconhecido. |

### R08 — O Mago

**Regra:** SE carta = "O Mago" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Favorável (quando combinado com ação real).

**Explicação (significado geral):** Manifestação e poder pessoal: ter as ferramentas necessárias (os 4 naipes na mesa) e usar a vontade e o foco para transformar potencial em realidade concreta.

**Justificativa da categoria:** Favorável quando combinado com ação real: a pessoa já tem as ferramentas. A ficha alerta para a sombra da carta (habilidade usada para manipular), registrada na combinação com o Diabo (R38).

**Fonte:** ficha 9.3.2.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Pessoa com clareza sobre o que quer e capacidade de agir para conquistar/construir isso. | Pessoa com as habilidades e recursos necessários em mãos, na posição de tomar iniciativa. | Estudante com domínio das ferramentas necessárias (método, recursos); só falta aplicar. |
| **Movimento** | Uma ação decisiva e focada está sendo tomada na relação. | Momento de transformar intenção em ação concreta no trabalho. | Momento de aplicar o método/recurso que já se domina. |
| **Resultado** | Manifestação bem-sucedida daquilo que foi intencionado na relação. | A vontade se concretiza em resultado profissional visível. | Domínio do conteúdo se confirma no resultado. |

### R09 — A Sacerdotisa

**Regra:** SE carta = "A Sacerdotisa" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Neutro / Mais Informação.

**Explicação (significado geral):** Intuição, mistério e conhecimento interior ainda não revelado. Representa o inconsciente, a espera consciente e a confiança na percepção interna, mesmo sem prova lógica.

**Justificativa da categoria:** Neutro / Mais Informação: a Sacerdotisa pede espera e escuta interna e raramente entrega um desfecho fechado.

**Fonte:** ficha 9.3.3.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Pessoa guiada pela intuição, possivelmente guardando sentimentos ainda não verbalizados. | Pessoa com percepção estratégica que ainda não expôs abertamente; boa fase para observar antes de agir. | Estudante com compreensão intuitiva de um assunto, mesmo sem conseguir formalizar esse conhecimento ainda. |
| **Movimento** | Momento pede escuta interna e paciência, não ação impulsiva. | Pede observação estratégica antes de qualquer movimento público. | Pede deixar a compreensão amadurecer antes de tentar explicá-la/prová-la. |
| **Resultado** | A verdade afetiva se revela com o tempo, não por força. | A resposta profissional certa depende de confiar no processo interno. | O entendimento se consolida com paciência, não com pressa. |

### R10 — Os Enamorados

**Regra:** SE carta = "Os Enamorados" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Neutro / Mais Informação.

**Explicação (significado geral):** Duas camadas que coexistem: união/conexão genuína (alinhamento real de valores) e escolha significativa — decisão consciente entre dois caminhos, que diz algo sobre quem a pessoa é.

**Justificativa da categoria:** Neutro / Mais Informação: a carta entrega uma decisão a ser tomada, não um desfecho pronto. As combinações dizem se a decisão vem com ruptura (R40), com desfecho feliz (R41) ou por apego (R25).

**Fonte:** ficha 9.3.4.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Pessoa diante de uma decisão afetiva importante, ou vivendo conexão de valores genuína com alguém. | Pessoa diante de uma escolha de carreira relevante, que reflete valores, não só conveniência. | Estudante decidindo entre dois caminhos de formação que precisam refletir o que realmente importa. |
| **Movimento** | Uma decisão afetiva está amadurecendo e não dá para adiar para sempre. | Uma escolha de carreira está se aproximando, exigindo posicionamento. | Uma decisão sobre rumo de estudos está se formando. |
| **Resultado** | Escolha consciente que consolida (ou encerra) o vínculo com clareza. | Decisão de carreira bem alinhada aos valores, mesmo que difícil. | Definição clara de um caminho de formação que faz sentido. |

### R11 — A Força

**Regra:** SE carta = "A Força" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Favorável (conquistado com esforço interno consciente).

**Explicação (significado geral):** Coragem genuína exercida com compaixão e autocontrole, não com força bruta — domar o instinto com paciência interior, poder que vem da serenidade.

**Justificativa da categoria:** Favorável: a vitória vem do esforço interno consciente e do autocontrole, não da sorte.

**Fonte:** ficha 9.3.5.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Pessoa lidando com desafio afetivo com paciência e compaixão, não confronto impulsivo. | Pessoa enfrentando desafio de trabalho com resiliência e controle emocional. | Estudante superando dificuldade com disciplina e paciência, não com virada de última hora. |
| **Movimento** | A situação pede domar um impulso (raiva, medo) com calma, não reagir no automático. | Pede resiliência frente a um obstáculo de trabalho. | Pede constância e disciplina em vez de esforço explosivo e pontual. |
| **Resultado** | Superação de um desafio afetivo pela via da paciência. | Vitória profissional que vem de dentro, do autocontrole. | Domínio da matéria conquistado por disciplina consistente. |

### R12 — A Roda da Fortuna

**Regra:** SE carta = "A Roda da Fortuna" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Cautela / Adiar.

**Explicação (significado geral):** O ciclo inevitável da vida — sorte, destino e mudança fora do controle direto da pessoa. O que sobe, desce; o que desce, sobe. Convida a soltar a ilusão de controle total.

**Justificativa da categoria:** Cautela / Adiar como padrão: o resultado não está nas mãos da pessoa. Muda com outra carta: com o Sol ou outra favorável tende ao bom (R31), com a Torre vira ruptura (R32), com a Justiça depende do mérito (R33).

**Fonte:** ficha 9.3.6.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Pessoa numa fase afetiva marcada por reviravoltas externas. | Pessoa numa fase de carreira sujeita a fatores fora do seu controle (mercado, timing, decisões de terceiros). | Estudante numa fase de altos e baixos que dependem também de fatores externos (sorte, timing). |
| **Movimento** | Mudança de ciclo em curso, movida por fatores externos. | Virada de carreira em curso, fora do controle direto. | Virada de fase de estudos em curso, por fatores externos. |
| **Resultado** | Desfecho depende de fatores fora do controle total — pode ser bom, mas é incerto até se concretizar. | Desfecho profissional depende de fatores fora do controle total — recomenda-se estar preparado para os dois lados. | Desfecho acadêmico depende de fatores fora do controle total — pode ser bom, mas é incerto até se concretizar. |

### R13 — A Justiça

**Regra:** SE carta = "A Justiça" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Favorável se o histórico for sólido; senão, Mudança Necessária.

**Explicação (significado geral):** Equilíbrio, verdade e causa-efeito: decisões justas baseadas em fatos, não em emoção. O que foi plantado será colhido, com clareza.

**Justificativa da categoria:** Favorável se o histórico for sólido; senão, Mudança Necessária (a conta chega). Como o histórico não é uma carta, o sistema pergunta ao usuário quando a Justiça sai. É o único caso em que a resposta da pessoa entra no raciocínio.

**Fonte:** ficha 9.3.7.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Situação que exige honestidade e equilíbrio nas decisões afetivas. | Pessoa diante de uma avaliação justa (mérito, contrato, decisão formal) baseada em fatos. | Estudante diante de uma avaliação que vai refletir exatamente o esforço/preparo real. |
| **Movimento** | Uma decisão afetiva está sendo processada com base em fatos reais. | Uma avaliação/processo está em curso e tende a ser justo, desde que a pessoa também seja honesta. | Um processo avaliativo em curso, proporcional ao esforço já investido. |
| **Resultado** | Resultado proporcional ao mérito/esforço real na relação. | Resultado profissional proporcional ao mérito — sem distorções. | Nota/resultado que reflete exatamente o preparo real. |

### R14 — A Morte

**Regra:** SE carta = "A Morte" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Mudança Necessária (transição já madura).

**Explicação (significado geral):** Não é morte física — é transformação profunda e encerramento simbólico de um ciclo que já cumpriu sua função, abrindo espaço obrigatório para o novo.

**Justificativa da categoria:** Mudança Necessária, mas de uma transição que já estava madura, diferente do colapso abrupto da Torre.

**Fonte:** ficha 9.3.8.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Fim de uma fase afetiva (não necessariamente da relação em si, mas de uma forma antiga de se relacionar). | Fase de carreira que já se esgotou — o cargo ou modo de trabalhar não cabe mais em quem a pessoa se tornou. | Fase de estudos/abordagem que não serve mais; é preciso deixar um método ou área para evoluir. |
| **Movimento** | Um encerramento está em curso — uma forma antiga do vínculo está morrendo para uma nova nascer. | Uma transição de carreira ou função está se processando. | Uma virada de chave em curso: o jeito antigo de estudar está sendo substituído. |
| **Resultado** | Transformação definitiva — fim real da relação ou de uma versão antiga dela, sem volta ao que era. | Fechamento de um ciclo profissional como ponto de virada necessário. | Encerramento de uma fase de formação — o antes e o depois ficam claramente separados. |

### R15 — A Temperança

**Regra:** SE carta = "A Temperança" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Favorável (de forma gradual).

**Explicação (significado geral):** Equilíbrio, moderação e integração de opostos (como as duas taças misturando líquidos). Cura gradual, ajuste fino, a arte do meio-termo certo, sem excessos.

**Justificativa da categoria:** Favorável de forma gradual: sucesso construído por ajuste, diferente do sucesso imediato do Sol.

**Fonte:** ficha 9.3.9.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Pessoa buscando (ou precisando buscar) equilíbrio e paciência no relacionamento. | Pessoa em fase de ajuste no trabalho — moderação entre exigência e descanso. | Estudante buscando equilíbrio entre estudo e descanso, ajustando método aos poucos. |
| **Movimento** | Processo gradual de ajuste e integração em curso na relação. | Ajuste gradual de rotina/expectativas no trabalho em curso. | Ajuste gradual de método de estudo em curso. |
| **Resultado** | Equilíbrio alcançado com paciência — harmonia sustentável no tempo. | Sucesso gradual, conquistado por ajuste, não por golpe único. | Domínio construído aos poucos, de forma sustentável. |

### R16 — O Diabo

**Regra:** SE carta = "O Diabo" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Mudança Necessária (de origem interna).

**Explicação (significado geral):** Aprisionamento autoimposto — vício, apego, dependência ou padrão tóxico que a pessoa poderia largar, mas ainda não se permite (as correntes, na carta clássica, são frouxas).

**Justificativa da categoria:** Mudança Necessária de origem interna: a mudança depende da escolha consciente da pessoa de romper o padrão (as correntes da carta são frouxas), não de um evento externo.

**Fonte:** ficha 9.3.10.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Pessoa presa a um padrão pouco saudável — dependência emocional, ciúme, relação desequilibrada de poder. | Pessoa presa a uma situação de trabalho tóxica ou a um padrão próprio (procrastinação, medo de sair da zona de conforto). | Estudante preso a um padrão prejudicial (procrastinação, comparação excessiva, medo de errar). |
| **Movimento** | Um padrão de apego/dependência está se intensificando na relação. | Um padrão prejudicial está se intensificando no trabalho. | Um padrão prejudicial está travando o progresso nos estudos. |
| **Resultado** | Se nada mudar, o desfecho é permanecer presa ao padrão — a corrente não se rompe sozinha. | Se nada mudar, o desfecho é permanecer presa ao padrão no trabalho — a corrente não se rompe sozinha. | Se nada mudar, o desfecho é permanecer presa ao padrão nos estudos — a corrente não se rompe sozinha. |

### R17 — A Torre

**Regra:** SE carta = "A Torre" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Mudança Necessária.

**Explicação (significado geral):** A ruptura inevitável — colapso súbito de uma estrutura já comprometida por dentro. Não cria a crise: revela uma que já existia, de forma abrupta e fora do controle da pessoa.

**Justificativa da categoria:** Mudança Necessária: é a carta-símbolo da categoria, a ruptura inevitável de uma estrutura já comprometida.

**Fonte:** ficha 9.3.11.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Relação sobre base frágil (segredo, desalinhamento, dependência) prestes a ser exposta. | Situação de trabalho instável mesmo que pareça estável por fora. | Método de estudo ou situação acadêmica insustentável que vai cobrar a conta. |
| **Movimento** | Ruptura ou confronto em curso e não dá para evitar. | Mudança abrupta em andamento: corte, demissão, reestruturação. | Crise em curso: reprovação se formando, choque de realidade sobre o método. |
| **Resultado** | Fim de um ciclo afetivo como está; o que resta depois tende a ser mais verdadeiro. | Desfecho de perda/corte que remove algo sem sustentação, abrindo espaço para reconstrução. | Resultado de reprovação ou necessidade de recomeçar o método. |

### R18 — A Estrela

**Regra:** SE carta = "A Estrela" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Favorável (de forma suave, sem euforia).

**Explicação (significado geral):** Esperança, cura e renovação depois de uma crise — a serenidade que sucede o colapso da Torre. Fé no futuro e reconexão consigo mesmo, sem a euforia do Sol.

**Justificativa da categoria:** Favorável de forma suave, sem euforia: cura e renovação depois de uma crise, diferente do sucesso explosivo do Sol.

**Fonte:** ficha 9.3.12.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Pessoa em processo de cura emocional após fase difícil, reconectando com esperança. | Pessoa se recuperando de fase profissional difícil, reencontrando inspiração e propósito. | Estudante recuperando confiança após dificuldade, reconectando com o motivo original de estudar. |
| **Movimento** | Processo de cura e renovação em curso, de melhora gradual. | Processo de recuperação e renovação profissional em curso, de melhora gradual. | Processo de recuperação e renovação nos estudos em curso, de melhora gradual. |
| **Resultado** | Esperança realizada — recomeço genuíno e mais saudável, mesmo que não perfeito. | Recomeço profissional mais saudável que o ponto de partida. | Recomeço de formação mais consciente e saudável. |

### R19 — O Sol

**Regra:** SE carta = "O Sol" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Favorável.

**Explicação (significado geral):** Vitalidade, sucesso, clareza e alegria genuína — a carta mais próxima de um "sim" direto do baralho. Verdade exposta à luz, reconhecimento e vitória sem ambiguidade.

**Justificativa da categoria:** Favorável em qualquer posição: é a carta-símbolo da categoria, a mais próxima de um "sim" direto do baralho.

**Fonte:** ficha 9.3.13.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Pessoa em fase de leveza afetiva, autoestima alta, relação transparente. | Pessoa em momento de reconhecimento, confiança e energia produtiva. | Estudante em fase de confiança e clareza — entendimento "acendeu". |
| **Movimento** | Relação caminha para mais abertura e verdade. | As coisas se encaminham para reconhecimento profissional. | Entendimento se consolidando, destravando uma dificuldade. |
| **Resultado** | Desfecho feliz e verdadeiro — reconciliação sincera, confiança mútua. | Sucesso visível: aprovação, conquista, resultado reconhecido publicamente. | Aprovação, boa nota, domínio real da matéria. |

### R20 — O Mundo

**Regra:** SE carta = "O Mundo" ENTÃO significado = o da tabela abaixo, conforme âmbito e posição; categoria sugerida = Favorável.

**Explicação (significado geral):** Conclusão e realização plena — um ciclo inteiro completado com sucesso. É a última carta da jornada do Louco: integração total, sensação de "chegar lá".

**Justificativa da categoria:** Favorável, com sentido de fim de jornada: junto com o Sol, uma das cartas mais fortes da categoria.

**Fonte:** ficha 9.3.14.

| Posição | Relacionamentos | Profissional | Acadêmico |
|---|---|---|---|
| **Significador** | Fase de relacionamento completo e realizado, ou fechamento de ciclo afetivo importante. | Colheita dos frutos de um ciclo de trabalho bem conduzido. | Conclusão de uma etapa de formação com sucesso, sensação de missão cumprida. |
| **Movimento** | Um ciclo afetivo está se completando, aproximando-se de um fechamento bem-sucedido. | Um ciclo profissional está se completando com sucesso. | Uma etapa de formação está se completando com sucesso. |
| **Resultado** | Conclusão plena e satisfatória do ciclo afetivo. | Objetivo profissional perseguido se realiza de forma completa e reconhecida. | Formação/etapa concluída de forma completa e reconhecida. |

### R26 — Ficha resumida (fallback)

**Regra:** SE não existir significado detalhado para a carta ENTÃO usar a ficha resumida (significado geral + categoria sugerida) como significado.

**Justificativa:** A Seção 2.3 limitou o aprofundamento a 14 arcanos. Para os outros 8, a regra de reserva usa a ficha resumida da Seção 9.4 (significado geral + categoria sugerida). Assim nenhuma carta fica sem leitura e o sistema nunca dá erro por falta de regra.

**Fonte:** Seção 9.4.

| Arcano | Significado geral | Categoria sugerida |
|---|---|---|
| A Imperatriz | Abundância, fertilidade, criatividade e cuidado — crescimento natural de algo que está sendo bem nutrido. | Favorável |
| O Imperador | Estrutura, autoridade e ordem — liderança prática e construção de bases sólidas. | Neutro / Mais Informação |
| O Hierofante | Tradição, aprendizado formal e orientação por regras ou instituições estabelecidas. | Neutro / Mais Informação |
| O Carro | Vontade direcionada, vitória por foco e disciplina — avançar apesar de forças opostas. | Favorável |
| O Eremita | Introspecção e busca interior — necessidade de se retirar para refletir antes de agir. | Neutro / Mais Informação |
| O Enforcado | Pausa forçada, ver a situação de outro ângulo, rendição necessária antes de agir. | Cautela / Adiar |
| A Lua | Ilusão, confusão e medos inconscientes — intuição turva, cuidado para não se guiar por aparências. | Cautela / Adiar |
| O Julgamento | Despertar, chamado para uma nova fase, avaliação final de um ciclo que se encerra. | Mudança Necessária |

O Imperador ("Favorável / Neutro, conforme o uso do poder") ficou como Neutro, e o Julgamento ("Mudança Necessária / Favorável, conforme o contexto") ficou como Mudança Necessária, porque o sistema não tem como saber o uso do poder ou o contexto. O especialista do grupo deve confirmar.

## Camada 4 — Combinações que decidem o resultado

### R23 — prioridade P1 (do documento)

**Regra:** SE A Torre em Movimento E O Sol em Resultado ENTÃO categoria final = Mudança Necessária, com tendência positiva após a mudança.

**Explicação:** Ruptura necessária seguida de desfecho favorável e claro.

**Justificativa:** É o exemplo central da Seção 9.5. A Torre no Movimento diz que a ruptura já está acontecendo; O Sol no Resultado diz que o desfecho é claro e positivo. As duas leituras são verdadeiras ao mesmo tempo, então a categoria não pode ser só Favorável (ignoraria a ruptura) nem só Mudança (ignoraria o desfecho). A regra registra as duas coisas: a mudança é necessária e o que vem depois dela é bom. Tem a prioridade mais alta porque é a combinação mais específica da base (duas cartas em posições fixas).

**Fonte:** R23 (Seção 8.2) e Seção 9.5. **Caso de teste:** CT01.

### R30 — prioridade P2 (criada a partir das fichas)

**Regra:** SE A Torre ou A Morte na tiragem E O Sol ou A Estrela na tiragem ENTÃO categoria final = Mudança Necessária, com tendência positiva após a mudança.

**Explicação:** Ruptura ou transformação que desemboca em clareza, esperança e cura.

**Justificativa:** Generaliza a R23 usando o que as fichas dizem sobre as mesmas cartas: a Morte + o Sol é uma transformação que desemboca em clareza; a Morte + a Estrela é esperança depois da transformação; a Torre + a Estrela é cura depois do colapso. Em todos os casos há uma ruptura e, junto, uma carta de desfecho positivo. Como não exige posição, cobre também tiragens em que as cartas aparecem em outra ordem. Fica logo abaixo da R23 porque é menos específica.

**Fonte:** Fichas 9.3.8 (Morte + Sol, Morte + Estrela), 9.3.11 (Torre + Estrela) e 9.3.12. **Caso de teste:** CT13.

### R24 — prioridade P3 (do documento)

**Regra:** SE O Diabo na tiragem E A Estrela na tiragem ENTÃO categoria final = Mudança Necessária, com de origem interna, com tendência de cura.

**Explicação:** Reconhecer o apego é o primeiro passo para a cura e a libertação real.

**Justificativa:** Vem da R24 do documento e das fichas do Diabo e da Estrela: reconhecer o apego é o primeiro passo para a cura. O documento não define a categoria. Escolhemos Mudança Necessária, porque o Diabo pede uma decisão de romper o padrão, com o complemento "tendência de cura", que é o que a Estrela acrescenta. A origem é interna (escolha da pessoa), como diz a ficha do Diabo.

**Fonte:** R24 (Seção 8.2), fichas 9.3.10 e 9.3.12. **Caso de teste:** CT09.

### R32 — prioridade P4 (criada a partir das fichas)

**Regra:** SE A Roda da Fortuna na tiragem E A Torre na tiragem ENTÃO categoria final = Mudança Necessária, com virada de ciclo como ruptura brusca.

**Explicação:** A virada de ciclo vem como ruptura brusca.

**Justificativa:** Da ficha da Roda da Fortuna: com a Torre, a virada de ciclo vem como ruptura brusca. A Roda sozinha é Cautela (a pessoa não controla o resultado), mas com a Torre o rumo deixa de ser incerto: a mudança vai acontecer, e de forma abrupta.

**Fonte:** Ficha 9.3.6 (Roda da Fortuna + Torre).

### R22 — prioridade P5 (do documento)

**Regra:** SE O Louco na tiragem E A Torre na tiragem ENTÃO categoria final = Cautela / Adiar, com cautela redobrada antes de se lançar.

**Explicação:** Cautela redobrada: melhor avaliar onde "vai cair" antes de se lançar.

**Justificativa:** Vem da R22 do documento e das fichas do Louco e da Torre: antes de se lançar, é preciso avaliar onde vai cair. O Louco pede um salto; a Torre mostra que a estrutura pode ruir. Juntos, o conselho é prudência. É um alerta, e por isso fica abaixo das regras de ruptura (R23, R30, R24, R32) e acima das regras favoráveis.

**Fonte:** R22 (Seção 8.2), fichas 9.3.1 e 9.3.11. **Caso de teste:** CT08.

### R25 — prioridade P6 (do documento)

**Regra:** SE Os Enamorados na tiragem E O Diabo na tiragem ENTÃO categoria final = Cautela / Adiar, com reavaliar a decisão antes de agir.

**Explicação:** Alerta: a decisão pode estar sendo tomada por apego ou medo, não por valor genuíno — reavaliar antes de decidir.

**Justificativa:** Vem da R25 do documento e das fichas dos Enamorados e do Diabo: a decisão (Enamorados) pode estar sendo tomada por apego ou medo (Diabo), e não por um valor genuíno. O conselho é reavaliar antes de decidir, por isso a categoria é Cautela.

**Fonte:** R25 (Seção 8.2), fichas 9.3.4 e 9.3.10. **Caso de teste:** CT10.

### R34 — prioridade P7 (criada a partir das fichas)

**Regra:** SE A Lua na tiragem E O Louco ou A Sacerdotisa na tiragem ENTÃO categoria final = Cautela / Adiar, com cuidado com ilusões.

**Explicação:** Risco de se guiar por ilusão em vez de intuição real — cuidado com as aparências.

**Justificativa:** Das fichas do Louco (+ Lua: risco de se guiar por ilusão em vez de intuição real, reforça Cautela) e da Sacerdotisa (+ Lua: reforça a cautela com ilusões). A Lua é a carta da confusão; junto de cartas que dependem da intuição ou do salto no escuro, o risco de engano aumenta.

**Fonte:** Fichas 9.3.1 (Louco + Lua) e 9.3.3 (Sacerdotisa + Lua).

### R35 — prioridade P8 (criada a partir das fichas)

**Regra:** SE O Sol na tiragem E A Lua na tiragem ENTÃO categoria final = Cautela / Adiar, com o sucesso só vem depois de dissipar a dúvida.

**Explicação:** Tensão entre clareza e confusão: o sucesso só vem depois de dissipar a dúvida.

**Justificativa:** Da ficha do Sol: com a Lua, há tensão entre clareza e confusão, e o sucesso só vem depois de dissipar a dúvida. Mesmo com O Sol na tiragem, o conselho é esperar a dúvida se resolver, por isso fica acima das regras favoráveis.

**Fonte:** Ficha 9.3.13 (Sol + Lua).

### R21 — prioridade P9 (do documento)

**Regra:** SE O Louco em Movimento E O Sol em Resultado ENTÃO categoria final = Favorável, com novo começo com desfecho positivo.

**Explicação:** Novo começo com desfecho positivo, reforçando o resultado Favorável.

**Justificativa:** Vem da R21 do documento e das fichas do Louco e do Sol: o salto do Louco, quando o desfecho é o Sol, é um novo começo leve e bem-sucedido. Fica abaixo das rupturas e dos alertas porque, se houver, por exemplo, uma Torre junto (CT13), a ruptura e o alerta pesam mais que o otimismo.

**Fonte:** R21 (Seção 8.2), fichas 9.3.1 e 9.3.13. **Caso de teste:** CT07.

### R33 — prioridade P10 (criada a partir das fichas)

**Regra:** SE A Roda da Fortuna na tiragem E A Justiça na tiragem ENTÃO categoria final = a categoria sugerida de A Justiça, com resultado proporcional ao mérito.

**Explicação:** O resultado do ciclo será proporcional ao mérito, não pura sorte.

**Justificativa:** Da ficha da Roda da Fortuna: com a Justiça, o resultado do ciclo será proporcional ao mérito, não pura sorte. Por isso a categoria final é a mesma da Justiça, que depende da resposta do usuário sobre o próprio histórico.

**Fonte:** Ficha 9.3.6 (Roda da Fortuna + Justiça).

### R31 — prioridade P11 (criada a partir das fichas)

**Regra:** SE A Roda da Fortuna na tiragem E carta com categoria Favorável na tiragem ENTÃO categoria final = Favorável, com a virada de ciclo tende para o lado bom.

**Explicação:** Com uma carta de apoio favorável, a virada de ciclo tende para o lado bom.

**Justificativa:** Da Seção 8.3 (a Roda é Cautela quando não há carta de apoio favorável) e da ficha da Roda (+ Sol: a virada tende para o lado bom). Se houver uma carta favorável na tiragem, ela indica para que lado a roda está girando.

**Fonte:** Ficha 9.3.6 (Roda + Sol) e Seção 8.3 (Roda sem carta de apoio favorável = Cautela). **Caso de teste:** CT12.

## Camada 4 — Combinações que só comentam a leitura (observações)

As fichas citam outras combinações que mudam o *sentido* da leitura, mas não dizem para que lado elas empurram o resultado. Elas viraram observações: quando as duas cartas aparecem na tiragem, o texto entra na leitura, mas a categoria continua sendo decidida pelas regras de decisão.

| Regra | SE (na mesma tiragem) | ENTÃO acrescenta à leitura | Fonte |
|---|---|---|---|
| R36 | O Louco E O Mago | Bagagem e ferramentas prontas — falta agir. | Fichas 9.3.1 e 9.3.2 (Louco + Mago) |
| R37 | O Mago E A Sacerdotisa | Equilíbrio entre ação e intuição/espera. | Fichas 9.3.2 e 9.3.3 (Mago + Sacerdotisa) |
| R38 | O Mago E O Diabo | Poder usado de forma manipuladora, não genuína. | Ficha 9.3.2 (Mago + Diabo) |
| R39 | A Sacerdotisa E O Eremita | Forte convergência de introspecção e retiro para reflexão. | Ficha 9.3.3 (Sacerdotisa + Eremita) |
| R40 | Os Enamorados E A Torre | A decisão vem com ruptura, mas é necessária. | Ficha 9.3.4 (Enamorados + Torre) |
| R41 | Os Enamorados E O Sol | A decisão tende a um desfecho feliz e claro. | Ficha 9.3.4 (Enamorados + Sol) |
| R42 | A Força E O Diabo | Tensão entre domar o instinto (Força) e ser dominado por ele (Diabo) — ponto de decisão. | Ficha 9.3.5 (Força + Diabo) |
| R43 | A Força E O Carro | Força controlada + direção decidida = avanço forte. | Ficha 9.3.5 (Força + Carro) |
| R44 | A Justiça E O Enforcado | Necessidade de ver a situação de um novo ângulo antes de julgar. | Ficha 9.3.7 (Justiça + Enforcado) |
| R45 | A Morte E A Torre | Reforça o encerramento de ciclo, quase sem meio-termo. | Fichas 9.3.8 e 9.3.11 (Morte + Torre) |
| R46 | A Temperança E A Torre | Depois da ruptura abrupta, vem a necessidade de reconstrução equilibrada e paciente. | Ficha 9.3.9 (Temperança + Torre) |
| R47 | A Temperança E O Diabo | Tensão entre moderação e excesso/apego. | Ficha 9.3.9 (Temperança + Diabo) |
| R48 | O Diabo E A Torre | A estrutura de apego vai ruir de forma forçada, já que não foi rompida por escolha própria. | Ficha 9.3.10 (Diabo + Torre) |
| R49 | A Estrela E O Sol | Combinação forte de Favorável: cura e sucesso reconhecido juntos. | Ficha 9.3.12 (Estrela + Sol) |
| R50 | O Mundo E O Louco | Fecha o ciclo simbólico completo da jornada do Tarô (início e fim). | Ficha 9.3.14 (Mundo + Louco) |
| R51 | O Mundo E O Sol | Dobra a força do resultado Favorável. | Ficha 9.3.14 (Mundo + Sol) |

## Camada 5 — Consolidação

Quando nenhuma combinação decide, estas regras transformam a tabela da Seção 8.3 em decisões com prioridade.

### R52 — prioridade P12

**Regra:** SE carta com categoria Mudança Necessária em Significador ou Movimento ENTÃO categoria final = Mudança Necessária.

**Explicação:** Uma carta de mudança na situação atual ou na tendência em curso pede ruptura ou mudança de rota.

**Justificativa:** Transforma em regra a linha "Mudança Necessária" da Seção 8.3: presença de Torre, Morte ou Diabo em Significador ou Movimento. Usamos a categoria das cartas em vez da lista de nomes, para que uma carta nova cadastrada como Mudança funcione sem mudar a regra. Uma ruptura no presente ou na tendência pesa mais que qualquer desfecho, por isso fica acima da R53.

**Fonte:** Seção 8.3 — Mudança Necessária: Torre, Morte ou Diabo em Significador ou Movimento. **Caso de teste:** CT06 e CT11 (histórico: não).

### R53 — prioridade P13

**Regra:** SE carta com categoria Favorável ou Cautela / Adiar ou Mudança Necessária em Resultado ENTÃO categoria final = a categoria sugerida de Resultado.

**Explicação:** A carta de Resultado (desfecho provável) define a categoria.

**Justificativa:** A posição Resultado é, por definição (R06), o desfecho provável. Quando nenhuma combinação e nenhuma carta de mudança se aplica, quem decide é a carta de Resultado. Se ela for Neutra, a regra não dispara e as próximas são avaliadas.

**Fonte:** Seção 8.1 — Resultado = desfecho provável (R06). **Caso de teste:** CT03.

### R54 — prioridade P14

**Regra:** SE carta com categoria Favorável na tiragem ENTÃO categoria final = Favorável.

**Explicação:** Há carta favorável na tiragem e nenhuma regra de maior prioridade se aplicou.

**Justificativa:** Da Seção 8.3: Favorável quando há cartas como Sol, Mundo, Estrela, Força ou Temperança na tiragem. Vale principalmente para tiragens de 1 e 2 cartas, que não têm posição Resultado.

**Fonte:** Seção 8.3 — Favorável: Sol, Mundo, Estrela, Força, Temperança na tiragem. **Caso de teste:** CT11 (histórico: sim).

### R55 — prioridade P15

**Regra:** SE carta com categoria Cautela / Adiar na tiragem ENTÃO categoria final = Cautela / Adiar.

**Explicação:** Há carta de cautela na tiragem sem carta de apoio favorável.

**Justificativa:** Da Seção 8.3: Cautela quando há carta de cautela (Roda, Lua, Enforcado) sem carta de apoio favorável. Como a R54 vem antes, quando existe carta favorável ela prevalece.

**Fonte:** Seção 8.3 — Cautela / Adiar. **Caso de teste:** CT02.

### R56 — prioridade P16

**Regra:** SE nenhuma regra de maior prioridade se aplicar ENTÃO categoria final = Neutro / Mais Informação.

**Explicação:** As cartas não indicam claramente um desfecho.

**Justificativa:** Regra padrão. Garante que toda tiragem válida tenha exatamente um resultado: se nada apontar um lado, o resultado é Neutro / Mais Informação, como diz a Seção 8.3.

**Fonte:** Seção 8.3 — Neutro / Mais Informação. **Caso de teste:** CT04 e CT05.

