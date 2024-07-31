IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Documents] (
    [Id] uniqueidentifier NOT NULL,
    [StreamId] uniqueidentifier NOT NULL,
    [Type] int NOT NULL,
    [Version] decimal(20,0) NOT NULL,
    [Time] datetimeoffset NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Data] nvarchar(max) NOT NULL,
    [DataType] nvarchar(max) NOT NULL,
    [TargetType] nvarchar(max) NOT NULL,
    [MetaDataUserId] nvarchar(max) NULL,
    [MetaDataUserName] nvarchar(max) NULL,
    [MetaDataTenantId] nvarchar(max) NULL,
    [MetaDataComment] nvarchar(max) NULL,
    [MetaDataAdditional] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Documents] PRIMARY KEY ([Id])
    );
GO

CREATE TABLE [Streams] (
    [StreamId] uniqueidentifier NOT NULL,
    [Created] datetimeoffset NOT NULL,
    [Version] decimal(20,0) NOT NULL,
    [NextVersion] decimal(20,0) NOT NULL,
    [Updated] datetimeoffset NOT NULL,
    [TargetType] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Streams] PRIMARY KEY ([StreamId])
    );
GO

CREATE INDEX [IX_Documents_StreamId] ON [Documents] ([StreamId]);
GO

CREATE UNIQUE INDEX [IX_Documents_StreamId_Version] ON [Documents] ([StreamId], [Version]);
GO

CREATE INDEX [IX_Documents_Version] ON [Documents] ([Version]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20230924140323_Initial', N'6.0.22');
GO

COMMIT;
GO