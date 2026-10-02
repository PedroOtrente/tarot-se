-- =====================================================================
-- Sistema Especialista — Tarô | Esquema do banco (MySQL 8)
-- A base de conhecimento fica em dados (RNF03):
--   carta (ficha geral + categoria sugerida + fallback R26),
--   significado (Camada 3: 14 arcanos × 3 âmbitos × 3 posições),
--   regra (combinações R21–R25, R30–R51 e consolidação R52–R56).
-- Contexto (R01–R03), posição (R04–R06) e tipo de tiragem (R27–R29)
-- ficam nas tabelas de domínio, com o código da regra.
-- =====================================================================
DROP DATABASE IF EXISTS tarot_se;
CREATE DATABASE tarot_se CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE tarot_se;

-- Camada 1 — Contexto (R01–R03)
CREATE TABLE ambito (
  id            INT PRIMARY KEY,
  codigo_regra  VARCHAR(10)  NOT NULL UNIQUE,
  nome          VARCHAR(40)  NOT NULL UNIQUE,
  descricao     VARCHAR(255) NOT NULL
);

-- Camada 2 — Posição (R04–R06)
CREATE TABLE posicao (
  id            INT PRIMARY KEY,
  codigo_regra  VARCHAR(10)  NOT NULL UNIQUE,
  nome          VARCHAR(40)  NOT NULL UNIQUE,
  descricao     VARCHAR(255) NOT NULL
);

-- Camada 2 — Tipo de tiragem (R27–R29)
CREATE TABLE tipo_tiragem (
  id            INT PRIMARY KEY,
  codigo_regra  VARCHAR(10)  NOT NULL UNIQUE,
  nome          VARCHAR(40)  NOT NULL,
  qtd_cartas    INT          NOT NULL,
  descricao     VARCHAR(255) NOT NULL
);

CREATE TABLE tipo_tiragem_posicao (
  tipo_tiragem_id INT NOT NULL,
  posicao_id      INT NOT NULL,
  ordem           INT NOT NULL,
  PRIMARY KEY (tipo_tiragem_id, posicao_id),
  FOREIGN KEY (tipo_tiragem_id) REFERENCES tipo_tiragem(id),
  FOREIGN KEY (posicao_id)      REFERENCES posicao(id)
);

CREATE TABLE categoria_resultado (
  id        INT PRIMARY KEY,
  codigo    VARCHAR(20)  NOT NULL UNIQUE,         -- FAVORAVEL, CAUTELA, MUDANCA, NEUTRO
  nome      VARCHAR(60)  NOT NULL UNIQUE,
  descricao VARCHAR(255) NOT NULL
);

-- Fato que o sistema pergunta ao usuário só quando uma regra precisa dele (ex.: A Justiça)
CREATE TABLE fato (
  codigo    VARCHAR(40)  PRIMARY KEY,
  pergunta  VARCHAR(255) NOT NULL,
  fonte     VARCHAR(255) NOT NULL
);

-- Ficha de cada arcano (Seções 9.3 e 9.4)
CREATE TABLE carta (
  id                    INT PRIMARY KEY,           -- id = número do arcano + 1
  nome                  VARCHAR(60)  NOT NULL UNIQUE,
  numero_arcano         INT          NOT NULL UNIQUE,
  numeral               VARCHAR(6)   NOT NULL,
  codigo_regra          VARCHAR(10)  NULL UNIQUE,  -- R07–R20 para os 14 aprofundados
  aprofundada           BOOLEAN      NOT NULL,
  descricao_geral       TEXT         NOT NULL,     -- significado geral / ficha resumida (fallback R26)
  categoria_sugerida_id INT          NOT NULL,
  fato_codigo           VARCHAR(40)  NULL,         -- se preenchido: categoria_sugerida quando o fato é verdadeiro
  categoria_se_falso_id INT          NULL,         --                categoria_se_falso quando é falso
  nuance_categoria      VARCHAR(120) NULL,         -- ex.: "de forma gradual"
  FOREIGN KEY (categoria_sugerida_id) REFERENCES categoria_resultado(id),
  FOREIGN KEY (categoria_se_falso_id) REFERENCES categoria_resultado(id),
  FOREIGN KEY (fato_codigo)           REFERENCES fato(codigo)
);

-- Camada 3 — Significado de carta × âmbito × posição (R07–R20)
CREATE TABLE significado (
  id                    INT AUTO_INCREMENT PRIMARY KEY,
  carta_id              INT  NOT NULL,
  ambito_id             INT  NULL,                 -- NULL = qualquer âmbito
  posicao_id            INT  NULL,                 -- NULL = qualquer posição
  texto                 TEXT NOT NULL,
  categoria_sugerida_id INT  NULL,                 -- NULL = usa a categoria sugerida da carta
  chave_unica VARCHAR(40) AS (CONCAT(carta_id,'-',IFNULL(ambito_id,0),'-',IFNULL(posicao_id,0))) STORED,
  UNIQUE KEY uq_significado (chave_unica),
  FOREIGN KEY (carta_id)              REFERENCES carta(id),
  FOREIGN KEY (ambito_id)             REFERENCES ambito(id),
  FOREIGN KEY (posicao_id)            REFERENCES posicao(id),
  FOREIGN KEY (categoria_sugerida_id) REFERENCES categoria_resultado(id)
);

-- Camadas 4 e 5 — Combinações e consolidação
CREATE TABLE regra (
  id                     INT AUTO_INCREMENT PRIMARY KEY,
  codigo_regra           VARCHAR(10)  NOT NULL UNIQUE,
  tipo                   ENUM('COMPOSTA','OBSERVACAO','CONSOLIDACAO') NOT NULL,
  descricao              VARCHAR(600) NOT NULL,     -- a regra escrita como SE...ENTÃO
  prioridade             INT          NULL UNIQUE,  -- 1 = mais alta; NULL para OBSERVACAO
  condicao_json          JSON         NOT NULL,
  categoria_resultado_id INT          NULL,
  herda_categoria_de     VARCHAR(60)  NULL,         -- nome de carta ou de posição, quando a categoria é herdada
  texto                  VARCHAR(500) NOT NULL,     -- significado agregado da combinação
  texto_complemento      VARCHAR(255) NULL,
  fonte                  VARCHAR(255) NOT NULL,     -- onde está no documento
  FOREIGN KEY (categoria_resultado_id) REFERENCES categoria_resultado(id)
);

-- Registro das sessões
CREATE TABLE tiragem (
  id              INT AUTO_INCREMENT PRIMARY KEY,
  ambito_id       INT      NOT NULL,
  tipo_tiragem_id INT      NOT NULL,
  data_hora       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  fatos_json      JSON     NULL,
  anotacao        TEXT     NULL,
  FOREIGN KEY (ambito_id)       REFERENCES ambito(id),
  FOREIGN KEY (tipo_tiragem_id) REFERENCES tipo_tiragem(id)
);

CREATE TABLE tiragem_carta (
  id                    INT AUTO_INCREMENT PRIMARY KEY,
  tiragem_id            INT  NOT NULL,
  posicao_id            INT  NOT NULL,
  carta_id              INT  NOT NULL,
  significado_id        INT  NULL,                 -- NULL = fallback R26
  categoria_aplicada_id INT  NULL,
  texto_aplicado        TEXT NOT NULL,
  UNIQUE KEY uq_pos   (tiragem_id, posicao_id),
  UNIQUE KEY uq_carta (tiragem_id, carta_id),
  FOREIGN KEY (tiragem_id)            REFERENCES tiragem(id) ON DELETE CASCADE,
  FOREIGN KEY (posicao_id)            REFERENCES posicao(id),
  FOREIGN KEY (carta_id)              REFERENCES carta(id),
  FOREIGN KEY (significado_id)        REFERENCES significado(id),
  FOREIGN KEY (categoria_aplicada_id) REFERENCES categoria_resultado(id)
);

CREATE TABLE resultado_tiragem (
  id                     INT AUTO_INCREMENT PRIMARY KEY,
  tiragem_id             INT  NOT NULL UNIQUE,
  categoria_resultado_id INT  NOT NULL,
  regra_id               INT  NOT NULL,
  combinacoes            VARCHAR(255) NULL,         -- códigos das combinações identificadas
  texto_final            TEXT NOT NULL,
  FOREIGN KEY (tiragem_id)             REFERENCES tiragem(id) ON DELETE CASCADE,
  FOREIGN KEY (categoria_resultado_id) REFERENCES categoria_resultado(id),
  FOREIGN KEY (regra_id)               REFERENCES regra(id)
);
