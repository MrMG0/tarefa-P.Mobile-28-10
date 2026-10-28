-- =============================================================================
-- ESQUEMA DO BANCO DE DADOS - VANDERPAN
-- Distribuidora de Produtos de Panificação (MySQL)
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `vanderpan_db` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `vanderpan_db`;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `contas_receber`;
DROP TABLE IF EXISTS `itens_venda`;
DROP TABLE IF EXISTS `vendas`;
DROP TABLE IF EXISTS `parcelas_condicao`;
DROP TABLE IF EXISTS `condicoes_pagamento`;
DROP TABLE IF EXISTS `itens_pedido`;
DROP TABLE IF EXISTS `pedidos`;
DROP TABLE IF EXISTS `produtos`;
DROP TABLE IF EXISTS `categorias`;
DROP TABLE IF EXISTS `clientes`;
DROP TABLE IF EXISTS `vendedores`;
DROP TABLE IF EXISTS `cidades`;
DROP TABLE IF EXISTS `estados`;

SET FOREIGN_KEY_CHECKS = 1;

-- -----------------------------------------------------------------------------
-- 1. LOCALIZAÇÃO
-- -----------------------------------------------------------------------------

CREATE TABLE `estados` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `nome` VARCHAR(50) NOT NULL,
  `uf` CHAR(2) NOT NULL UNIQUE,
  `ativo` TINYINT(1) DEFAULT 1
) ENGINE=InnoDB;

CREATE TABLE `cidades` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `nome` VARCHAR(100) NOT NULL,
  `id_estado` INT NOT NULL,
  `ativo` TINYINT(1) DEFAULT 1,
  CONSTRAINT `fk_cidades_estado` FOREIGN KEY (`id_estado`) REFERENCES `estados` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 2. PESSOAS
-- -----------------------------------------------------------------------------

CREATE TABLE `vendedores` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `nome` VARCHAR(200) NOT NULL,
  `email` VARCHAR(200) NOT NULL UNIQUE,
  `telefone` VARCHAR(20) NULL,
  `cpf` VARCHAR(14) NOT NULL UNIQUE,
  `senha_hash` VARCHAR(255) NOT NULL,
  `data_cadastro` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `ativo` TINYINT(1) DEFAULT 1
) ENGINE=InnoDB;

CREATE TABLE `clientes` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `razao_social` VARCHAR(200) NOT NULL,
  `nome_fantasia` VARCHAR(200) NULL,
  `cnpj` VARCHAR(18) NOT NULL UNIQUE,
  `email` VARCHAR(200) NULL,
  `telefone` VARCHAR(20) NULL,
  `cep` VARCHAR(10) NULL,
  `rua` VARCHAR(200) NULL,
  `numero` VARCHAR(20) NULL,
  `bairro` VARCHAR(100) NULL,
  `id_cidade` INT NULL,
  `id_vendedor` INT NOT NULL,
  `sequencia_visita` INT NOT NULL COMMENT 'Sequencia com espacamento (10, 20, 30...) para insercoes intermediarias',
  `data_cadastro` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `ativo` TINYINT(1) DEFAULT 1,
  CONSTRAINT `fk_clientes_cidade` FOREIGN KEY (`id_cidade`) REFERENCES `cidades` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_clientes_vendedor` FOREIGN KEY (`id_vendedor`) REFERENCES `vendedores` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 3. PRODUTOS E CATEGORIAS
-- -----------------------------------------------------------------------------

CREATE TABLE `categorias` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `nome` VARCHAR(100) NOT NULL,
  `descricao` TEXT NULL,
  `ativo` TINYINT(1) DEFAULT 1
) ENGINE=InnoDB;

CREATE TABLE `produtos` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `nome` VARCHAR(200) NOT NULL,
  `descricao` TEXT NULL,
  `preco` DECIMAL(10,2) NOT NULL,
  `id_categoria` INT NOT NULL,
  `disponivel` TINYINT(1) NOT NULL DEFAULT 1,
  `imagem_url` VARCHAR(500) NULL,
  `estoque` DECIMAL(10,3) DEFAULT 0,
  `unidade_medida` ENUM('UNIDADE','LITRO','KG','BALDE') DEFAULT 'UNIDADE',
  `perecivel` TINYINT(1) DEFAULT 1,
  `data_cadastro` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `ativo` TINYINT(1) DEFAULT 1,
  CONSTRAINT `fk_produtos_categoria` FOREIGN KEY (`id_categoria`) REFERENCES `categorias` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 4. PEDIDOS
-- -----------------------------------------------------------------------------

CREATE TABLE `pedidos` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `id_vendedor` INT NOT NULL,
  `id_cliente` INT NOT NULL,
  `data_pedido` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `status` ENUM('PENDENTE','CORTADO','CONFIRMADO','EM_SEPARACAO','ENVIADO','ENTREGUE','CANCELADO') DEFAULT 'PENDENTE',
  `janela_entrega` ENUM('12H','19H') DEFAULT '12H',
  `observacao` TEXT NULL,
  `valor_total` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  CONSTRAINT `fk_pedidos_vendedor` FOREIGN KEY (`id_vendedor`) REFERENCES `vendedores` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_pedidos_cliente` FOREIGN KEY (`id_cliente`) REFERENCES `clientes` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `itens_pedido` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `id_pedido` INT NOT NULL,
  `id_produto` INT NOT NULL,
  `quantidade_pedida` DECIMAL(10,3) NOT NULL,
  `quantidade_entregue` DECIMAL(10,3) NULL,
  `preco_unitario` DECIMAL(10,2) NOT NULL,
  `subtotal` DECIMAL(10,2) NOT NULL,
  CONSTRAINT `fk_itens_pedido_pedido` FOREIGN KEY (`id_pedido`) REFERENCES `pedidos` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_itens_pedido_produto` FOREIGN KEY (`id_produto`) REFERENCES `produtos` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 5. FINANCEIRO E CONDIÇÕES DE PAGAMENTO
-- -----------------------------------------------------------------------------

CREATE TABLE `condicoes_pagamento` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `codigo` VARCHAR(10) NOT NULL UNIQUE,
  `descricao` VARCHAR(200) NOT NULL,
  `multa_percentual` DECIMAL(5,2) NOT NULL DEFAULT 0.00,
  `juros_dia_percentual` DECIMAL(5,2) NOT NULL DEFAULT 0.00,
  `desconto_dia_percentual` DECIMAL(5,2) NOT NULL DEFAULT 0.00,
  `ativo` TINYINT(1) DEFAULT 1
) ENGINE=InnoDB;

CREATE TABLE `parcelas_condicao` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `id_condicao` INT NOT NULL,
  `numero_parcela` INT NOT NULL,
  `dias_prazo` INT NOT NULL,
  `percentual` DECIMAL(5,2) NOT NULL,
  `forma_pagamento` ENUM('BOLETO','PIX','DINHEIRO','CHEQUE','TRANSFERENCIA') NOT NULL,
  CONSTRAINT `fk_parcelas_condicao` FOREIGN KEY (`id_condicao`) REFERENCES `condicoes_pagamento` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE `vendas` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `id_pedido` INT NOT NULL,
  `id_cliente` INT NOT NULL,
  `id_vendedor` INT NOT NULL,
  `id_condicao` INT NOT NULL,
  `data_venda` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `numero_nota` VARCHAR(20) NOT NULL,
  `valor_total` DECIMAL(10,2) NOT NULL,
  UNIQUE KEY `uq_vendas_pedido` (`id_pedido`),
  CONSTRAINT `fk_vendas_pedido` FOREIGN KEY (`id_pedido`) REFERENCES `pedidos` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_vendas_cliente` FOREIGN KEY (`id_cliente`) REFERENCES `clientes` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_vendas_vendedor` FOREIGN KEY (`id_vendedor`) REFERENCES `vendedores` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_vendas_condicao` FOREIGN KEY (`id_condicao`) REFERENCES `condicoes_pagamento` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `itens_venda` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `id_venda` INT NOT NULL,
  `id_produto` INT NOT NULL,
  `quantidade` DECIMAL(10,3) NOT NULL,
  `preco_unitario` DECIMAL(10,2) NOT NULL,
  `subtotal` DECIMAL(10,2) NOT NULL,
  CONSTRAINT `fk_itens_venda_venda` FOREIGN KEY (`id_venda`) REFERENCES `vendas` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_itens_venda_produto` FOREIGN KEY (`id_produto`) REFERENCES `produtos` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `contas_receber` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `id_venda` INT NOT NULL,
  `id_cliente` INT NOT NULL,
  `numero_parcela` INT NOT NULL,
  `numero_documento` VARCHAR(30) NOT NULL,
  `data_vencimento` DATE NOT NULL,
  `valor_original` DECIMAL(10,2) NOT NULL,
  `valor_multa` DECIMAL(10,2) DEFAULT 0.00,
  `valor_juros` DECIMAL(10,2) DEFAULT 0.00,
  `valor_desconto` DECIMAL(10,2) DEFAULT 0.00,
  `valor_pago` DECIMAL(10,2) DEFAULT 0.00,
  `data_pagamento` DATE NULL,
  `forma_pagamento` ENUM('BOLETO','PIX','DINHEIRO','CHEQUE','TRANSFERENCIA') NOT NULL,
  `status` ENUM('ABERTO','PAGO','VENCIDO','CANCELADO') DEFAULT 'ABERTO',
  CONSTRAINT `fk_contas_venda` FOREIGN KEY (`id_venda`) REFERENCES `vendas` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_contas_cliente` FOREIGN KEY (`id_cliente`) REFERENCES `clientes` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- SEED DATA (DADOS INICIAIS DE EXEMPLO)
-- -----------------------------------------------------------------------------

-- Estados
INSERT INTO `estados` (`id`, `nome`, `uf`) VALUES
(1, 'São Paulo', 'SP'),
(2, 'Paraná', 'PR');

-- Cidades
INSERT INTO `cidades` (`id`, `nome`, `id_estado`) VALUES
(1, 'Curitiba', 2),
(2, 'São José dos Pinhais', 2),
(3, 'São Paulo', 1);

-- Vendedores (senha padrão: "123456" hasheada com bcrypt)
INSERT INTO `vendedores` (`id`, `nome`, `email`, `telefone`, `cpf`, `senha_hash`) VALUES
(1, 'Carlos Oliveira', 'carlos@vanderpan.com.br', '(41) 98888-1111', '111.222.333-44', '$2a$11$0a4zYV0Z5v96M6gW.lGjCe8tWn/kM73o.lO4F9Z3z1.V1T41e0cTu'),
(2, 'Mariana Santos', 'mariana@vanderpan.com.br', '(41) 97777-2222', '222.333.444-55', '$2a$11$0a4zYV0Z5v96M6gW.lGjCe8tWn/kM73o.lO4F9Z3z1.V1T41e0cTu');

-- Clientes com sequenciamento espaçado (10, 20, 30...)
INSERT INTO `clientes` (`id`, `razao_social`, `nome_fantasia`, `cnpj`, `email`, `telefone`, `cep`, `rua`, `numero`, `bairro`, `id_cidade`, `id_vendedor`, `sequencia_visita`) VALUES
(1, 'Panificadora Pão Dourado Ltda', 'Panificadora Pão Dourado', '12.345.678/0001-90', 'contato@paodourado.com.br', '(41) 3222-1111', '80000-000', 'Rua das Flores', '100', 'Centro', 1, 1, 10),
(2, 'Padaria e Confeitaria Doce Pão', 'Doce Pão', '23.456.789/0001-01', 'docepao@gmail.com', '(41) 3333-2222', '80000-010', 'Av. Batel', '450', 'Batel', 1, 1, 20),
(3, 'Padaria Esperança ME', 'Padaria Esperança', '34.567.890/0001-12', 'esperanca@padaria.com', '(41) 3444-3333', '80000-020', 'Rua Marechal Deodoro', '820', 'Alto da XV', 1, 1, 30);

-- Categorias de Panificação
INSERT INTO `categorias` (`id`, `nome`, `descricao`) VALUES
(1, 'Fermentos e Leveduras', 'Fermentos biológicos frescos, secos e químicos'),
(2, 'Misturas para Bolos e Confeitaria', 'Misturas prontas para bolos, pães de ló, brownies'),
(3, 'Gorduras e Margarinas', 'Margarinas para folhados, bolos e cremes (baldes 20L/15kg)'),
(4, 'Farinhas e Melhoradores', 'Farinhas especiais e melhoradores de farinha');

-- Produtos perecíveis de panificação
INSERT INTO `produtos` (`id`, `nome`, `descricao`, `preco`, `id_categoria`, `disponivel`, `estoque`, `unidade_medida`, `perecivel`) VALUES
(1, 'Fermento Biológico Fresco 500g', 'Fermento biológico prensado para panificação artesanal e industrial', 7.50, 1, 1, 250.000, 'UNIDADE', 1),
(2, 'Fermento Seco Instantâneo 500g', 'Fermento em pó de alta atividade fermentativa', 18.90, 1, 1, 180.000, 'UNIDADE', 1),
(3, 'Margarina para Confeitaria Balde 20L', 'Margarina com 80% de lipídios com e sem sal para massas nobres', 185.00, 3, 1, 15.000, 'BALDE', 1),
(4, 'Mistura para Pão Francês 25kg', 'Mistura balanceada para pão francês crocante', 98.00, 4, 1, 60.000, 'KG', 1),
(5, 'Mistura para Bolo de Chocolate 5kg', 'Mistura úmida e fofa para bolos de confeitaria', 42.50, 2, 1, 40.000, 'KG', 1),
(6, 'Mistura para Salgados Fritos e Assados 5kg', 'Base profissional para coxinhas, esfihas e risoles', 38.00, 2, 1, 50.000, 'KG', 1);

-- Condições de Pagamento de Referência
-- Condição 10: 7/14 dias, multa 5%, juros 2% ao dia, desconto 1% ao dia
INSERT INTO `condicoes_pagamento` (`id`, `codigo`, `descricao`, `multa_percentual`, `juros_dia_percentual`, `desconto_dia_percentual`) VALUES
(1, '10', '7/14 dias - Boleto Bancário', 5.00, 2.00, 1.00),
(2, '20', '7/14/21 dias - PIX e Boleto', 5.00, 2.00, 1.00);

-- Parcelas da Condição 10 (50% em 7 dias, 50% em 14 dias - Boleto)
INSERT INTO `parcelas_condicao` (`id`, `id_condicao`, `numero_parcela`, `dias_prazo`, `percentual`, `forma_pagamento`) VALUES
(1, 1, 1, 7, 50.00, 'BOLETO'),
(2, 1, 2, 14, 50.00, 'BOLETO');

-- Parcelas da Condição 20 (33.33% 7 dias PIX, 33.33% 14 dias Boleto, 33.34% 21 dias Boleto)
INSERT INTO `parcelas_condicao` (`id`, `id_condicao`, `numero_parcela`, `dias_prazo`, `percentual`, `forma_pagamento`) VALUES
(3, 2, 1, 7, 33.33, 'PIX'),
(4, 2, 2, 14, 33.33, 'BOLETO'),
(5, 2, 3, 21, 33.34, 'BOLETO');
