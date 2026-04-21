USE Apostas;
GO

CREATE TRIGGER TRG_Aposta_Estado_Changed
ON Aposta
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Só nos interessa quando o Estado mudou
    IF NOT UPDATE(Estado) RETURN;

    DECLARE @Aposta_Id      INT;
    DECLARE @Utilizador_Id  INT;
    DECLARE @Novo_Estado    INT;
    DECLARE @Montante       DECIMAL(10,2);
    DECLARE @Odd            DECIMAL(10,2);
    DECLARE @Premio         DECIMAL(10,2);

    -- Iterar pelas linhas atualizadas (pode ser mais do que uma na resolução em massa)
    DECLARE cur CURSOR FOR
        SELECT i.Id, i.Utilizador_Id, i.Estado, i.Montante, i.Odd
        FROM inserted i
        JOIN deleted  d ON i.Id = d.Id
        WHERE i.Estado <> d.Estado;  -- apenas onde o estado realmente mudou

    OPEN cur;
    FETCH NEXT FROM cur INTO @Aposta_Id, @Utilizador_Id, @Novo_Estado, @Montante, @Odd;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Aposta GANHA → transação PG + crédito no saldo
        IF @Novo_Estado = 2
        BEGIN
            SET @Premio = @Montante * @Odd;

            INSERT INTO Pagamentos.dbo.Transacao
                (Aposta_Id, Utilizador_Id, Tipo, Valor, Data_Hora, Estado)
            VALUES
                (@Aposta_Id, @Utilizador_Id, 'PG', @Premio, GETDATE(), 'Processada');

            UPDATE Pagamentos.dbo.Saldo_Utilizador
            SET Saldo = Saldo + @Premio, Ultima_Atualizacao = GETDATE()
            WHERE Utilizador_Id = @Utilizador_Id;
        END

        -- Aposta ANULADA → transação RE + devolver montante original
        ELSE IF @Novo_Estado = 4
        BEGIN
            INSERT INTO Pagamentos.dbo.Transacao
                (Aposta_Id, Utilizador_Id, Tipo, Valor, Data_Hora, Estado)
            VALUES
                (@Aposta_Id, @Utilizador_Id, 'RE', @Montante, GETDATE(), 'Processada');

            UPDATE Pagamentos.dbo.Saldo_Utilizador
            SET Saldo = Saldo + @Montante, Ultima_Atualizacao = GETDATE()
            WHERE Utilizador_Id = @Utilizador_Id;
        END

        -- Aposta PERDIDA (3) → não faz nada, o débito já foi feito na inserção

        FETCH NEXT FROM cur INTO @Aposta_Id, @Utilizador_Id, @Novo_Estado, @Montante, @Odd;
    END

    CLOSE cur;
    DEALLOCATE cur;
END;
GO