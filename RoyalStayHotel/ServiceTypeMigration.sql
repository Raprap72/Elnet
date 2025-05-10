-- Add ServiceType column to HotelServices table
ALTER TABLE [HotelServices] ADD [ServiceType] int NOT NULL DEFAULT 0;

-- Update existing records to make Room Cleaning, Breakfast Buffet, Gym Access, and Swimming Pool as AdditionalService type (value 1)
UPDATE [HotelServices] SET [ServiceType] = 1 WHERE [Id] = 1; -- Room Cleaning
UPDATE [HotelServices] SET [ServiceType] = 1 WHERE [Id] = 2; -- Breakfast Buffet
UPDATE [HotelServices] SET [ServiceType] = 1 WHERE [Id] = 3; -- Gym Access
UPDATE [HotelServices] SET [ServiceType] = 1 WHERE [Id] = 4; -- Swimming Pool

-- Insert additional main services if they don't exist
IF NOT EXISTS (SELECT 1 FROM [HotelServices] WHERE [Id] = 5)
BEGIN
    SET IDENTITY_INSERT [HotelServices] ON;
    INSERT INTO [HotelServices] ([Id], [Description], [IsAvailable], [Name], [Price], [ServiceType])
    VALUES (5, N'Private luxury transportation to and from the airport', CAST(1 AS bit), N'Airport Transfers', 2500.0, 0);
    SET IDENTITY_INSERT [HotelServices] OFF;
END
ELSE
BEGIN
    UPDATE [HotelServices] SET [ServiceType] = 0 WHERE [Id] = 5;
END

IF NOT EXISTS (SELECT 1 FROM [HotelServices] WHERE [Id] = 6)
BEGIN
    SET IDENTITY_INSERT [HotelServices] ON;
    INSERT INTO [HotelServices] ([Id], [Description], [IsAvailable], [Name], [Price], [ServiceType])
    VALUES (6, N'Explore the city at your own pace with our premium bikes', CAST(1 AS bit), N'Bike Rentals', 800.0, 0);
    SET IDENTITY_INSERT [HotelServices] OFF;
END
ELSE
BEGIN
    UPDATE [HotelServices] SET [ServiceType] = 0 WHERE [Id] = 6;
END

IF NOT EXISTS (SELECT 1 FROM [HotelServices] WHERE [Id] = 7)
BEGIN
    SET IDENTITY_INSERT [HotelServices] ON;
    INSERT INTO [HotelServices] ([Id], [Description], [IsAvailable], [Name], [Price], [ServiceType])
    VALUES (7, N'Assistance with reservations, tickets, and local recommendations', CAST(1 AS bit), N'Concierge Services', 0.0, 0);
    SET IDENTITY_INSERT [HotelServices] OFF;
END
ELSE
BEGIN
    UPDATE [HotelServices] SET [ServiceType] = 0 WHERE [Id] = 7;
END

IF NOT EXISTS (SELECT 1 FROM [HotelServices] WHERE [Id] = 8)
BEGIN
    SET IDENTITY_INSERT [HotelServices] ON;
    INSERT INTO [HotelServices] ([Id], [Description], [IsAvailable], [Name], [Price], [ServiceType])
    VALUES (8, N'Professional translators available upon request', CAST(1 AS bit), N'Translation Services', 1500.0, 0);
    SET IDENTITY_INSERT [HotelServices] OFF;
END
ELSE
BEGIN
    UPDATE [HotelServices] SET [ServiceType] = 0 WHERE [Id] = 8;
END

-- Update migration history table to mark this migration as applied
IF EXISTS (SELECT 1 FROM [__EFMigrationsHistory])
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20250429150229_AddServiceTypeColumn')
    BEGIN
        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20250429150229_AddServiceTypeColumn', N'9.0.4');
    END
END 