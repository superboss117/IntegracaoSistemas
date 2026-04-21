CREATE DATABASE Pagamentos;
GO
USE Pagamentos;
GO

CREATE TABLE Saldo_Utilizador (
    Utilizador_Id       INT             PRIMARY KEY,
    Saldo               DECIMAL(10,2)   NOT NULL DEFAULT 0.00
        CONSTRAINT CHK_Saldo_Positivo CHECK (Saldo >= 0),
    Ultima_Atualizacao  DATETIME        NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Transacao (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Aposta_Id       INT             NULL,         -- pode ser NULL (depósitos, levantamentos)
    Utilizador_Id   INT             NOT NULL,
    Tipo            CHAR(2)         NOT NULL
        CONSTRAINT CHK_Tipo_Transacao CHECK (Tipo IN ('AP','PG','RE','DE','LV')),
    Valor           DECIMAL(10,2)   NOT NULL
        CONSTRAINT CHK_Valor_Transacao CHECK (Valor > 0),
    Data_Hora       DATETIME        NOT NULL DEFAULT GETDATE(),
    Estado          NVARCHAR(20)    NOT NULL DEFAULT 'Pendente'
        CONSTRAINT CHK_Estado_Transacao CHECK (Estado IN ('Pendente','Processada','Falhada','Reembolsada'))
);