-- Usuário da aplicação (use se estiver rodando o MySQL instalado localmente, sem Docker).
CREATE USER IF NOT EXISTS 'tarot'@'%' IDENTIFIED BY 'tarot123';
CREATE USER IF NOT EXISTS 'tarot'@'localhost' IDENTIFIED BY 'tarot123';
GRANT ALL PRIVILEGES ON tarot_se.* TO 'tarot'@'%';
GRANT ALL PRIVILEGES ON tarot_se.* TO 'tarot'@'localhost';
FLUSH PRIVILEGES;
