USE Apostas;
GO

CREATE OR ALTER PROCEDURE SP_Inserir_Jogo
    @Codigo_Jogo  VARCHAR(20),
    @Data_Hora    DATETIME,
    @Equipa_Casa  NVARCHAR(100),
    @Equipa_Fora  NVARCHAR(100),
    @Competicao   NVARCHAR(100),
    @Estado       INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Validar formato FUT-AAAA-JJNN
    IF @Codigo_Jogo NOT LIKE 'FUT-[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]'
    BEGIN
        RAISERROR('Formato do Codigo_Jogo inválido. Use FUT-AAAA-JJNN.', 16, 1);
        RETURN;
    END

    -- 2. Validar estado
    IF @Estado NOT IN (1, 2, 3, 4, 5)
    BEGIN
        RAISERROR('Estado inválido. Valores aceites: 1 a 5.', 16, 1);
        RETURN;
    END

    -- 3. Validar campos obrigatórios
    IF LTRIM(RTRIM(@Equipa_Casa)) = '' OR LTRIM(RTRIM(@Equipa_Fora)) = '' OR LTRIM(RTRIM(@Competicao)) = ''
    BEGIN
        RAISERROR('Equipa_Casa, Equipa_Fora e Competicao não podem estar vazios.', 16, 1);
        RETURN;
    END

    -- 4. Verificar duplicado
    IF EXISTS (SELECT 1 FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo)
    BEGIN
        RAISERROR('Já existe um jogo com o código %s.', 16, 1, @Codigo_Jogo);
        RETURN;
    END

    -- 5. Inserir
    BEGIN TRY
        INSERT INTO Jogo (Codigo_Jogo, Data_Hora, Equipa_Casa, Equipa_Fora, Competicao, Estado)
        VALUES (@Codigo_Jogo, @Data_Hora, @Equipa_Casa, @Equipa_Fora, @Competicao, @Estado);

        SELECT SCOPE_IDENTITY() AS Id_Inserido;
    END TRY
    BEGIN CATCH
        DECLARE @msg NVARCHAR(2048) = ERROR_MESSAGE();
        DECLARE @sev INT            = ERROR_SEVERITY();
        DECLARE @sta INT            = ERROR_STATE();
        RAISERROR(@msg, @sev, @sta);
    END CATCH
END;
GO


CREATE PROCEDURE SP_Resolver_Apostas
    @Jogo_Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Golos_Casa INT, @Golos_Fora INT;

    SELECT @Golos_Casa = Golos_Casa, @Golos_Fora = Golos_Fora
    FROM Resultado WHERE Jogo_Id = @Jogo_Id;

    -- Atualiza cada tipo de aposta pendente
    UPDATE Aposta
    SET Estado = CASE
        WHEN Tipo_Aposta = '1' AND @Golos_Casa >  @Golos_Fora THEN 2  -- Ganha
        WHEN Tipo_Aposta = '1' AND @Golos_Casa <= @Golos_Fora THEN 3  -- Perdida
        WHEN Tipo_Aposta = 'X' AND @Golos_Casa =  @Golos_Fora THEN 2
        WHEN Tipo_Aposta = 'X' AND @Golos_Casa <> @Golos_Fora THEN 3
        WHEN Tipo_Aposta = '2' AND @Golos_Fora >  @Golos_Casa THEN 2
        WHEN Tipo_Aposta = '2' AND @Golos_Fora <= @Golos_Casa THEN 3
        ELSE Estado
    END
    WHERE Jogo_Id = @Jogo_Id AND Estado = 1;  -- apenas Pendentes
    -- O trigger disparará para cada linha atualizada
END;
GO


CREATE OR ALTER PROCEDURE SP_Atualizar_Jogo
    @Codigo_Jogo  VARCHAR(20),
    @Novo_Estado  INT,
    @Golos_Casa   INT = NULL,
    @Golos_Fora   INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;

    -- 1. Validar estado recebido
    IF @Novo_Estado NOT IN (1, 2, 3, 4, 5)
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('Estado inválido. Valores aceites: 1 a 5.', 16, 1);
        RETURN;
    END

    -- 2. Buscar jogo
    DECLARE @Jogo_Id INT, @Estado_Atual INT;
    SELECT @Jogo_Id = Id, @Estado_Atual = Estado
    FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo;

    IF @Jogo_Id IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('Jogo não encontrado.', 16, 1);
        RETURN;
    END

    -- 3. Validar transição de estado
    IF NOT (
        (@Estado_Atual = 1 AND @Novo_Estado IN (2, 4, 5)) OR
        (@Estado_Atual = 2 AND @Novo_Estado IN (3, 4, 5)) OR
        (@Estado_Atual = @Novo_Estado)
    )
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('Transição de estado inválida.', 16, 1);
        RETURN;
    END

    -- 4. Atualizar estado
    UPDATE Jogo SET Estado = @Novo_Estado WHERE Id = @Jogo_Id;

    -- 5. Lógica de Finalização
    IF @Novo_Estado = 3
    BEGIN
        -- Usamos IF/ELSE em vez de MERGE para máxima compatibilidade
        IF EXISTS (SELECT 1 FROM Resultado WHERE Jogo_Id = @Jogo_Id)
        BEGIN
            UPDATE Resultado SET 
                Golos_Casa = ISNULL(@Golos_Casa, Golos_Casa), 
                Golos_Fora = ISNULL(@Golos_Fora, Golos_Fora), 
                Ultima_Atualizacao = GETDATE()
            WHERE Jogo_Id = @Jogo_Id AND Desconhecido = 1;
        END
        ELSE
        BEGIN
            INSERT INTO Resultado (Jogo_Id, Golos_Casa, Golos_Fora, Desconhecido, Ultima_Atualizacao)
            VALUES (@Jogo_Id, ISNULL(@Golos_Casa, 0), ISNULL(@Golos_Fora, 0), 
                    CASE WHEN @Golos_Casa IS NULL THEN 1 ELSE 0 END, GETDATE());
        END

        EXEC SP_Resolver_Apostas @Jogo_Id;
    END

    -- 6. Lógica de Cancelamento / Adiamento
    IF @Novo_Estado IN (4, 5)
    BEGIN
        UPDATE Aposta SET Estado = 4 
        WHERE Jogo_Id = @Jogo_Id AND Estado = 1;
    END

    COMMIT TRANSACTION;
END;
GO


CREATE OR ALTER PROCEDURE SP_Inserir_Aposta
    @Jogo_Id        INT,
    @Utilizador_Id  INT,
    @Tipo_Aposta    CHAR(1),
    @Montante       DECIMAL(10,2),
    @Odd            DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    -- 1. Validações de Jogo
    DECLARE @Estado_Jogo INT;
    SELECT @Estado_Jogo = Estado FROM Jogo WHERE Id = @Jogo_Id;
    
    IF @Estado_Jogo IS NULL
    BEGIN ROLLBACK; RAISERROR('Jogo não encontrado.', 16, 1); RETURN; END
    
    IF @Estado_Jogo IN (3, 4, 5)
    BEGIN ROLLBACK; RAISERROR('Não é possível apostar num jogo terminado, cancelado ou adiado.', 16, 1); RETURN; END

    -- 2. Validações de Aposta
    IF @Tipo_Aposta NOT IN ('1','X','2')
    BEGIN ROLLBACK; RAISERROR('Tipo de aposta inválido.', 16, 1); RETURN; END
    
    IF @Montante <= 0
    BEGIN ROLLBACK; RAISERROR('Montante deve ser positivo.', 16, 1); RETURN; END
    
    -- 3. Validação de Saldo (Base de dados Pagamentos)
    -- Ajustado para usar os nomes de colunas que confirmaste na imagem
    IF (SELECT Saldo FROM Pagamentos.dbo.Saldo_Utilizador WHERE Utilizador_Id = @Utilizador_Id) < @Montante
    BEGIN ROLLBACK; RAISERROR('Saldo insuficiente.', 16, 1); RETURN; END

    -- 4. Debitar Saldo
    UPDATE Pagamentos.dbo.Saldo_Utilizador
    SET Saldo = Saldo - @Montante
    WHERE Utilizador_Id = @Utilizador_Id;

    -- 5. Registar transação (Usando os nomes exatos da tua imagem: Id, Utilizador_Id, Estado)
    DECLARE @Trans_Id TABLE (Id INT);
    INSERT INTO Pagamentos.dbo.Transacao (Utilizador_Id, Tipo, Valor, Estado)
    OUTPUT INSERTED.Id INTO @Trans_Id
    VALUES (@Utilizador_Id, 'AP', @Montante, 'Processada');

    -- 6. Inserir aposta
    INSERT INTO Aposta (Jogo_Id, Utilizador_Id, Tipo_Aposta, Montante, Odd, Estado)
    VALUES (@Jogo_Id, @Utilizador_Id, @Tipo_Aposta, @Montante, @Odd, 1);

    DECLARE @Aposta_Id INT = SCOPE_IDENTITY();

    -- 7. Atualizar Aposta_Id na transação
    UPDATE Pagamentos.dbo.Transacao
    SET Aposta_Id = @Aposta_Id
    WHERE Id = (SELECT Id FROM @Trans_Id);

    SELECT @Aposta_Id AS Id_Aposta;
    COMMIT TRANSACTION;
END;
GO


CREATE PROCEDURE SP_Cancelar_Aposta
    @Aposta_Id INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    DECLARE @Estado_Aposta INT, @Estado_Jogo INT, @Jogo_Id INT;

    SELECT @Estado_Aposta = a.Estado, @Estado_Jogo = j.Estado, @Jogo_Id = a.Jogo_Id
    FROM Aposta a JOIN Jogo j ON a.Jogo_Id = j.Id
    WHERE a.Id = @Aposta_Id;

    IF @Estado_Aposta IS NULL
        BEGIN ROLLBACK; RAISERROR('Aposta não encontrada.', 16, 1); RETURN; END
    IF @Estado_Aposta <> 1
        BEGIN ROLLBACK; RAISERROR('Só é possível cancelar apostas Pendentes.', 16, 1); RETURN; END
    IF @Estado_Jogo <> 1
        BEGIN ROLLBACK; RAISERROR('Só é possível cancelar apostas em jogos Agendados.', 16, 1); RETURN; END

    -- Marcar como Anulada — o trigger tratará do reembolso
    UPDATE Aposta SET Estado = 4 WHERE Id = @Aposta_Id;

    COMMIT TRANSACTION;
END;
GO

CREATE PROCEDURE SP_Inserir_Resultado
    @Codigo_Jogo VARCHAR(20),
    @Golos_Casa  INT,
    @Golos_Fora  INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Jogo_Id INT, @Estado INT;
    SELECT @Jogo_Id = Id, @Estado = Estado FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo;

    IF @Jogo_Id IS NULL
        BEGIN RAISERROR('Jogo não encontrado.', 16, 1); RETURN; END
    IF @Estado <> 3 --Garante que ninguem insere resultados de jogos que ainda não acabaram, evitando erros no sistema
        BEGIN RAISERROR('Só é possível inserir resultado em jogos Finalizados.', 16, 1); RETURN; END
    IF EXISTS (SELECT 1 FROM Resultado WHERE Jogo_Id = @Jogo_Id AND Desconhecido = 0)
        BEGIN RAISERROR('Este jogo já tem resultado inserido.', 16, 1); RETURN; END

    -- Se já existe registo desconhecido, atualiza; senão insere
    IF EXISTS (SELECT 1 FROM Resultado WHERE Jogo_Id = @Jogo_Id)
        UPDATE Resultado
        SET Golos_Casa = @Golos_Casa, Golos_Fora = @Golos_Fora,
            Ultima_Atualizacao = GETDATE(), Desconhecido = 0
        WHERE Jogo_Id = @Jogo_Id;
    ELSE
        INSERT INTO Resultado (Jogo_Id, Golos_Casa, Golos_Fora, Ultima_Atualizacao, Desconhecido)
        VALUES (@Jogo_Id, @Golos_Casa, @Golos_Fora, GETDATE(), 0);
END;
GO


CREATE OR ALTER PROCEDURE SP_Criar_Utilizador
    @Nome   NVARCHAR(100),
    @Email  NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validar dados
        IF LTRIM(RTRIM(@Nome)) = ''
        BEGIN
            RAISERROR('Nome é obrigatório.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF LTRIM(RTRIM(@Email)) = ''
        BEGIN
            RAISERROR('Email é obrigatório.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Evitar duplicados
        IF EXISTS (SELECT 1 FROM Apostas.dbo.Utilizador WHERE Email = @Email)
        BEGIN
            RAISERROR('Já existe um utilizador com esse email.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Inserir utilizador em Apostas
        INSERT INTO Apostas.dbo.Utilizador (Nome, Email, Data_Criacao)
        VALUES (@Nome, @Email, GETDATE());

        DECLARE @Utilizador_Id INT = SCOPE_IDENTITY();

        -- Inserir saldo inicial em Pagamentos
        INSERT INTO Pagamentos.dbo.Saldo_Utilizador
            (Utilizador_Id, Saldo, Ultima_Atualizacao)
        VALUES
            (@Utilizador_Id, 50.00, GETDATE());

        -- Registar transação promocional
        INSERT INTO Pagamentos.dbo.Transacao
            (Aposta_Id, Utilizador_Id, Tipo, Valor, Data_Hora, Estado)
        VALUES
            (NULL, @Utilizador_Id, 'DE', 50.00, GETDATE(), 'Processada');

        COMMIT TRANSACTION;

        SELECT
            @Utilizador_Id AS Utilizador_Id,
            @Nome AS Nome,
            @Email AS Email,
            50.00 AS Saldo_Inicial;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO