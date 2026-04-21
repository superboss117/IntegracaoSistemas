-- =============================================
-- Base de Dados: Resultados
-- BetStrike -- Plataforma de Resultados de Futebol
-- =============================================

CREATE DATABASE Resultados;
GO

USE Resultados;
GO

-- =============================================
-- TABELA: Jogos
-- =============================================
CREATE TABLE Jogos (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Codigo_Jogo     VARCHAR(20)     NOT NULL UNIQUE,   -- FUT-AAAA-JJNN
    Data_Jogo       DATE            NOT NULL,
    Hora_Inicio     TIME            NOT NULL,
    Equipa_Casa     NVARCHAR(100)   NOT NULL,
    Equipa_Fora     NVARCHAR(100)   NOT NULL,
    Golos_Casa      INT             NOT NULL DEFAULT 0,
    Golos_Fora      INT             NOT NULL DEFAULT 0,
    Estado          INT             NOT NULL DEFAULT 1, -- 1=Agendado,2=Em Curso,3=Finalizado,4=Cancelado,5=Adiado
    Data_Criacao    DATETIME        NOT NULL DEFAULT GETDATE(),
    Data_Atualizacao DATETIME       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT CK_Estado CHECK (Estado BETWEEN 1 AND 5),
    CONSTRAINT CK_Golos_Casa CHECK (Golos_Casa >= 0),
    CONSTRAINT CK_Golos_Fora CHECK (Golos_Fora >= 0),
    CONSTRAINT CK_Equipas CHECK (Equipa_Casa <> Equipa_Fora)
);
GO

-- =============================================
-- SP: Inserir Jogo
-- =============================================
CREATE PROCEDURE sp_InserirJogo
    @Codigo_Jogo    VARCHAR(20),
    @Data_Jogo      DATE,
    @Hora_Inicio    TIME,
    @Equipa_Casa    NVARCHAR(100),
    @Equipa_Fora    NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Validar formato do código (FUT-AAAA-JJNN)
    IF @Codigo_Jogo NOT LIKE 'FUT-[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]'
    BEGIN
        RAISERROR('Código de jogo inválido. Formato esperado: FUT-AAAA-JJNN', 16, 1);
        RETURN;
    END

    -- Verificar duplicado
    IF EXISTS (SELECT 1 FROM Jogos WHERE Codigo_Jogo = @Codigo_Jogo)
    BEGIN
        RAISERROR('Já existe um jogo com o código %s.', 16, 1, @Codigo_Jogo);
        RETURN;
    END

    INSERT INTO Jogos (Codigo_Jogo, Data_Jogo, Hora_Inicio, Equipa_Casa, Equipa_Fora, Estado)
    VALUES (@Codigo_Jogo, @Data_Jogo, @Hora_Inicio, @Equipa_Casa, @Equipa_Fora, 1);

    SELECT SCOPE_IDENTITY() AS Id_Inserido, @Codigo_Jogo AS Codigo_Jogo;
END
GO

-- =============================================
-- SP: Atualizar Jogo (estado e/ou marcador)
-- =============================================
CREATE PROCEDURE sp_AtualizarJogo
    @Codigo_Jogo    VARCHAR(20),
    @Novo_Estado    INT,
    @Golos_Casa     INT,
    @Golos_Fora     INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Estado_Atual INT;

    SELECT @Estado_Atual = Estado FROM Jogos WHERE Codigo_Jogo = @Codigo_Jogo;

    IF @Estado_Atual IS NULL
    BEGIN
        RAISERROR('Jogo com código %s não encontrado.', 16, 1, @Codigo_Jogo);
        RETURN;
    END

    -- Validar transições de estado
    -- Finalizado, Cancelado e Adiado são estados terminais
    IF @Estado_Atual IN (3, 4, 5)
    BEGIN
        RAISERROR('O jogo está num estado terminal e não pode ser alterado.', 16, 1);
        RETURN;
    END

    -- Agendado(1) só pode ir para Em Curso(2), Cancelado(4) ou Adiado(5)
    IF @Estado_Atual = 1 AND @Novo_Estado NOT IN (2, 4, 5)
    BEGIN
        RAISERROR('Transição de estado inválida: Agendado só pode ir para Em Curso, Cancelado ou Adiado.', 16, 1);
        RETURN;
    END

    -- Em Curso(2) só pode ir para Finalizado(3), Cancelado(4) ou Adiado(5)
    IF @Estado_Atual = 2 AND @Novo_Estado NOT IN (3, 4, 5)
    BEGIN
        RAISERROR('Transição de estado inválida: Em Curso só pode ir para Finalizado, Cancelado ou Adiado.', 16, 1);
        RETURN;
    END

    UPDATE Jogos
    SET Estado           = @Novo_Estado,
        Golos_Casa       = @Golos_Casa,
        Golos_Fora       = @Golos_Fora,
        Data_Atualizacao = GETDATE()
    WHERE Codigo_Jogo = @Codigo_Jogo;

    SELECT @@ROWCOUNT AS Linhas_Afetadas;
END
GO

-- =============================================
-- SP: Listar Jogos (com filtros opcionais)
-- =============================================
CREATE PROCEDURE sp_ListarJogos
    @Data   DATE = NULL,
    @Estado INT  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Codigo_Jogo, Data_Jogo, Hora_Inicio,
           Equipa_Casa, Equipa_Fora, Golos_Casa, Golos_Fora,
           Estado, Data_Criacao, Data_Atualizacao
    FROM Jogos
    WHERE (@Data   IS NULL OR Data_Jogo = @Data)
      AND (@Estado IS NULL OR Estado    = @Estado)
    ORDER BY Data_Jogo, Hora_Inicio;
END
GO

-- =============================================
-- SP: Obter Jogo por Código
-- =============================================
CREATE PROCEDURE sp_ObterJogo
    @Codigo_Jogo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Codigo_Jogo, Data_Jogo, Hora_Inicio,
           Equipa_Casa, Equipa_Fora, Golos_Casa, Golos_Fora,
           Estado, Data_Criacao, Data_Atualizacao
    FROM Jogos
    WHERE Codigo_Jogo = @Codigo_Jogo;

    IF @@ROWCOUNT = 0
        RAISERROR('Jogo não encontrado.', 16, 1);
END
GO

-- =============================================
-- SP: Remover Jogo (só Agendado)
-- =============================================
CREATE PROCEDURE sp_RemoverJogo
    @Codigo_Jogo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Estado INT;
    SELECT @Estado = Estado FROM Jogos WHERE Codigo_Jogo = @Codigo_Jogo;

    IF @Estado IS NULL
    BEGIN
        RAISERROR('Jogo não encontrado.', 16, 1);
        RETURN;
    END

    IF @Estado <> 1
    BEGIN
        RAISERROR('Só é permitido remover jogos no estado Agendado.', 16, 1);
        RETURN;
    END

    DELETE FROM Jogos WHERE Codigo_Jogo = @Codigo_Jogo;
    SELECT @@ROWCOUNT AS Linhas_Afetadas;
END
GO