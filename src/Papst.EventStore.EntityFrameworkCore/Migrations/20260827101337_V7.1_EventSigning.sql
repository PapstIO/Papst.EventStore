BEGIN TRANSACTION;
ALTER TABLE [Streams] ADD [LatestSignature] nvarchar(max) NULL;

ALTER TABLE [Streams] ADD [SigningAlgorithm] nvarchar(max) NULL;

ALTER TABLE [Streams] ADD [SigningCertificateThumbprint] nvarchar(max) NULL;

ALTER TABLE [Documents] ADD [Signature] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260827101337_V7.1_EventSigning', N'10.0.11');

COMMIT;
GO

