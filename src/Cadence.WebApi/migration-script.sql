BEGIN TRANSACTION;
ALTER TABLE [Injects] ADD [DeliveryMethodId] uniqueidentifier NULL;

ALTER TABLE [Injects] ADD [DeliveryMethodOther] nvarchar(100) NULL;

ALTER TABLE [Injects] ADD [LocationName] nvarchar(200) NULL;

ALTER TABLE [Injects] ADD [LocationType] nvarchar(100) NULL;

ALTER TABLE [Injects] ADD [Priority] int NULL;

ALTER TABLE [Injects] ADD [ResponsibleController] nvarchar(200) NULL;

ALTER TABLE [Injects] ADD [SourceReference] nvarchar(50) NULL;

ALTER TABLE [Injects] ADD [Track] nvarchar(100) NULL;

ALTER TABLE [Injects] ADD [TriggerType] nvarchar(20) NOT NULL DEFAULT N'';

CREATE TABLE [DeliveryMethods] (
    [Id] uniqueidentifier NOT NULL,
    [OrganizationId] uniqueidentifier NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [IsOther] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NOT NULL,
    [ModifiedBy] uniqueidentifier NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    [DeletedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_DeliveryMethods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeliveryMethods_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ExpectedOutcomes] (
    [Id] uniqueidentifier NOT NULL,
    [InjectId] uniqueidentifier NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [SortOrder] int NOT NULL,
    [WasAchieved] bit NULL,
    [EvaluatorNotes] nvarchar(2000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NOT NULL,
    [ModifiedBy] uniqueidentifier NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    [DeletedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_ExpectedOutcomes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExpectedOutcomes_Injects_InjectId] FOREIGN KEY ([InjectId]) REFERENCES [Injects] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'Description', N'IsActive', N'IsDeleted', N'IsOther', N'ModifiedBy', N'Name', N'OrganizationId', N'SortOrder', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[DeliveryMethods]'))
    SET IDENTITY_INSERT [DeliveryMethods] ON;
INSERT INTO [DeliveryMethods] ([Id], [CreatedAt], [CreatedBy], [DeletedAt], [DeletedBy], [Description], [IsActive], [IsDeleted], [IsOther], [ModifiedBy], [Name], [OrganizationId], [SortOrder], [UpdatedAt])
VALUES ('10000000-0000-0000-0000-000000000001', '2025-01-01T00:00:00.0000000Z', '00000000-0000-0000-0000-000000000001', NULL, NULL, N'Spoken directly to player', CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), '00000000-0000-0000-0000-000000000001', N'Verbal', NULL, 1, '2025-01-01T00:00:00.0000000Z'),
('10000000-0000-0000-0000-000000000002', '2025-01-01T00:00:00.0000000Z', '00000000-0000-0000-0000-000000000001', NULL, NULL, N'Simulated phone call', CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), '00000000-0000-0000-0000-000000000001', N'Phone', NULL, 2, '2025-01-01T00:00:00.0000000Z'),
('10000000-0000-0000-0000-000000000003', '2025-01-01T00:00:00.0000000Z', '00000000-0000-0000-0000-000000000001', NULL, NULL, N'Simulated email', CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), '00000000-0000-0000-0000-000000000001', N'Email', NULL, 3, '2025-01-01T00:00:00.0000000Z'),
('10000000-0000-0000-0000-000000000004', '2025-01-01T00:00:00.0000000Z', '00000000-0000-0000-0000-000000000001', NULL, NULL, N'Radio communication', CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), '00000000-0000-0000-0000-000000000001', N'Radio', NULL, 4, '2025-01-01T00:00:00.0000000Z'),
('10000000-0000-0000-0000-000000000005', '2025-01-01T00:00:00.0000000Z', '00000000-0000-0000-0000-000000000001', NULL, NULL, N'Paper document', CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), '00000000-0000-0000-0000-000000000001', N'Written', NULL, 5, '2025-01-01T00:00:00.0000000Z'),
('10000000-0000-0000-0000-000000000006', '2025-01-01T00:00:00.0000000Z', '00000000-0000-0000-0000-000000000001', NULL, NULL, N'CAX/simulation input', CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), '00000000-0000-0000-0000-000000000001', N'Simulation', NULL, 6, '2025-01-01T00:00:00.0000000Z'),
('10000000-0000-0000-0000-000000000007', '2025-01-01T00:00:00.0000000Z', '00000000-0000-0000-0000-000000000001', NULL, NULL, N'Custom delivery method (specify in notes)', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), '00000000-0000-0000-0000-000000000001', N'Other', NULL, 7, '2025-01-01T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'CreatedBy', N'DeletedAt', N'DeletedBy', N'Description', N'IsActive', N'IsDeleted', N'IsOther', N'ModifiedBy', N'Name', N'OrganizationId', N'SortOrder', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[DeliveryMethods]'))
    SET IDENTITY_INSERT [DeliveryMethods] OFF;

CREATE INDEX [IX_Injects_DeliveryMethodId] ON [Injects] ([DeliveryMethodId]);

CREATE INDEX [IX_Injects_Track] ON [Injects] ([Track]);

CREATE INDEX [IX_DeliveryMethods_OrganizationId_Name] ON [DeliveryMethods] ([OrganizationId], [Name]);

CREATE INDEX [IX_DeliveryMethods_OrganizationId_SortOrder] ON [DeliveryMethods] ([OrganizationId], [SortOrder]);

CREATE INDEX [IX_ExpectedOutcomes_InjectId_SortOrder] ON [ExpectedOutcomes] ([InjectId], [SortOrder]);

ALTER TABLE [Injects] ADD CONSTRAINT [FK_Injects_DeliveryMethods_DeliveryMethodId] FOREIGN KEY ([DeliveryMethodId]) REFERENCES [DeliveryMethods] ([Id]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260117034932_AddExcelImportFields', N'10.0.0');

COMMIT;
GO

