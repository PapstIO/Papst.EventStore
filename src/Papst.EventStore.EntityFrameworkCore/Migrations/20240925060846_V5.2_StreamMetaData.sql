BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Documents]') AND [c].[name] = N'MetaDataComment');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Documents] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Documents] DROP COLUMN [MetaDataComment];
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Documents]') AND [c].[name] = N'MetaDataTenantId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Documents] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Documents] DROP COLUMN [MetaDataTenantId];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Documents]') AND [c].[name] = N'MetaDataUserId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Documents] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Documents] DROP COLUMN [MetaDataUserId];
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Documents]') AND [c].[name] = N'MetaDataUserName');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Documents] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Documents] DROP COLUMN [MetaDataUserName];
GO

EXEC sp_rename N'[Documents].[MetaDataAdditional]', N'MetaData', N'COLUMN';
GO

ALTER TABLE [Streams] ADD [LatestSnapshotVersion] decimal(20,0) NULL;
GO

ALTER TABLE [Streams] ADD [MetaDataAdditionJson] nvarchar(max) NULL;
GO

ALTER TABLE [Streams] ADD [MetaDataComment] nvarchar(max) NULL;
GO

ALTER TABLE [Streams] ADD [MetaDataTenantId] nvarchar(max) NULL;
GO

ALTER TABLE [Streams] ADD [MetaDataUserId] nvarchar(max) NULL;
GO

ALTER TABLE [Streams] ADD [MetaDataUserName] nvarchar(max) NULL;
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Documents]') AND [c].[name] = N'TargetType');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Documents] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Documents] ALTER COLUMN [TargetType] nvarchar(100) NOT NULL;
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Documents]') AND [c].[name] = N'Name');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Documents] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Documents] ALTER COLUMN [Name] nvarchar(100) NOT NULL;
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Documents]') AND [c].[name] = N'DataType');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Documents] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Documents] ALTER COLUMN [DataType] nvarchar(100) NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240925060846_V5.2_StreamMetaData', N'8.0.7');
GO

COMMIT;
GO

