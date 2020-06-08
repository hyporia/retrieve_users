CREATE TABLE [dbo].[Users] (
    [Id]      INT            NOT NULL,
    [Name]    NVARCHAR (MAX) NULL,
    [City]    NVARCHAR (MAX) NULL,
    [Phone]   NVARCHAR (MAX) NULL,
    [UpdDate] DATETIME       NOT NULL,
    [Email]   NVARCHAR (255) NULL,
    CONSTRAINT [PK_dbo.Users] PRIMARY KEY CLUSTERED ([Id] ASC)
);

