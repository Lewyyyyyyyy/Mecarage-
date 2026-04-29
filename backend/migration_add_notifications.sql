-- Migration: Add Notification table for chef notification system
-- This migration adds support for notifying chefs about new symptom reports

CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [RecipientId] uniqueidentifier NOT NULL,
    [SymptomReportId] uniqueidentifier NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [NotificationType] nvarchar(100) NOT NULL,
    [IsRead] bit NOT NULL DEFAULT 0,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [ReadAt] datetime2 NULL,
    [CreatedUtc] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Users_RecipientId] FOREIGN KEY ([RecipientId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Notifications_SymptomReports_SymptomReportId] FOREIGN KEY ([SymptomReportId]) REFERENCES [SymptomReports] ([Id]) ON DELETE SET NULL
);

-- Create indexes for better query performance
CREATE INDEX [IX_Notifications_RecipientId] ON [Notifications] ([RecipientId]);
CREATE INDEX [IX_Notifications_SymptomReportId] ON [Notifications] ([SymptomReportId]);
CREATE INDEX [IX_Notifications_RecipientId_IsRead] ON [Notifications] ([RecipientId], [IsRead]);
CREATE INDEX [IX_Notifications_CreatedAt] ON [Notifications] ([CreatedAt] DESC);

