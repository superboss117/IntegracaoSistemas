CREATE DATABASE Apostas;
GO
USE Apostas;
GO

CREATE TABLE Jogo (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Codigo_Jogo   VARCHAR(20)       NOT NULL UNIQUE,  -- FUT-AAAA-JJNN
    Data_Hora     DATETIME          NOT NULL,
    Equipa_Casa   NVARCHAR(100)     NOT NULL,
    Equipa_Fora   NVARCHAR(100)     NOT NULL,
    Competicao    NVARCHAR(100)     NOT NULL,
    Estado        INT               NOT NULL DEFAULT 1
        CONSTRAINT CHK_Jogo_Estado CHECK (Estado BETWEEN 1 AND 5)
        -- 1=Agendado, 2=Em Curso, 3=Finalizado, 4=Cancelado, 5=Adiado
);

CREATE TABLE Resultado (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Jogo_Id         INT           NOT NULL UNIQUE,  -- 1-para-1
    Golos_Casa      INT           NOT NULL DEFAULT 0,
    Golos_Fora      INT           NOT NULL DEFAULT 0,
    Ultima_Atualizacao DATETIME   NOT NULL DEFAULT GETDATE(),
    Desconhecido    BIT           NOT NULL DEFAULT 0,  -- flag resultado auto
    CONSTRAINT FK_Resultado_Jogo FOREIGN KEY (Jogo_Id)
        REFERENCES Jogo(Id) ON DELETE CASCADE
);

CREATE TABLE Aposta (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Jogo_Id         INT             NOT NULL,
    Utilizador_Id   INT             NOT NULL,
    Tipo_Aposta     CHAR(1)         NOT NULL
        CONSTRAINT CHK_Tipo_Aposta CHECK (Tipo_Aposta IN ('1','X','2')),
    Montante        DECIMAL(10,2)   NOT NULL
        CONSTRAINT CHK_Montante CHECK (Montante > 0),
    Odd             DECIMAL(10,2)   NOT NULL
        CONSTRAINT CHK_Odd CHECK (Odd > 1.0),
    Estado          INT             NOT NULL DEFAULT 1
        CONSTRAINT CHK_Aposta_Estado CHECK (Estado BETWEEN 1 AND 4),
        -- 1=Pendente, 2=Ganha, 3=Perdida, 4=Anulada
    Data_Hora       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Aposta_Jogo FOREIGN KEY (Jogo_Id)
        REFERENCES Jogo(Id)
);


CREATE TABLE Utilizador (
 Id              INT IDENTITY(1,1) PRIMARY KEY,
     Nome        NVARCHAR(100)  NOT NULL,
    Email       NVARCHAR(150)  NOT NULL UNIQUE,
    Data_Criacao DATETIME      NOT NULL DEFAULT GETDATE()
);
