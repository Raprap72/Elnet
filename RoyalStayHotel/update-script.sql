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
CREATE TABLE [ContactFormSubmissions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [SubmissionDate] datetime2 NOT NULL,
    [IsRead] bit NOT NULL,
    [ResponseMessage] nvarchar(max) NULL,
    [RespondedAt] datetime2 NULL,
    CONSTRAINT [PK_ContactFormSubmissions] PRIMARY KEY ([Id])
);

CREATE TABLE [Discounts] (
    [DiscountId] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [DiscountCode] nvarchar(max) NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [IsPercentage] bit NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [MinimumStay] int NULL,
    [MinimumSpend] decimal(18,2) NULL,
    [MaxUsage] int NULL,
    [MaxUses] int NULL,
    [UsageCount] int NOT NULL,
    [Type] int NOT NULL,
    [RoomTypeId] int NULL,
    [ApplicableRoomType] int NULL,
    CONSTRAINT [PK_Discounts] PRIMARY KEY ([DiscountId])
);

CREATE TABLE [Guests] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [Address] nvarchar(200) NOT NULL,
    [City] nvarchar(100) NOT NULL,
    [Country] nvarchar(100) NOT NULL,
    [PostalCode] nvarchar(20) NOT NULL,
    [DateOfBirth] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Guests] PRIMARY KEY ([Id])
);

CREATE TABLE [HotelServices] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [IsAvailable] bit NOT NULL,
    CONSTRAINT [PK_HotelServices] PRIMARY KEY ([Id])
);

CREATE TABLE [RoomTypeInfos] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [BasePrice] decimal(18,2) NOT NULL,
    [ImageUrl] nvarchar(200) NOT NULL,
    [MaxOccupancy] int NOT NULL,
    [BedType] nvarchar(100) NOT NULL,
    [Size] nvarchar(50) NOT NULL,
    [Amenities] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_RoomTypeInfos] PRIMARY KEY ([Id])
);

CREATE TABLE [RoomTypeInventories] (
    [Id] int NOT NULL IDENTITY,
    [RoomType] int NOT NULL,
    [Price] int NOT NULL,
    [TotalRooms] int NOT NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_RoomTypeInventories] PRIMARY KEY ([Id])
);

CREATE TABLE [Services] (
    [ServiceId] int NOT NULL IDENTITY,
    [ServiceName] nvarchar(100) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_Services] PRIMARY KEY ([ServiceId])
);

CREATE TABLE [SiteSettings] (
    [Id] int NOT NULL IDENTITY,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(1000) NOT NULL,
    [DisplayName] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Category] nvarchar(50) NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [Id] int NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Email] nvarchar(450) NOT NULL,
    [Username] nvarchar(450) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [UserType] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
);

CREATE TABLE [Rooms] (
    [RoomId] int NOT NULL IDENTITY,
    [Id] int NOT NULL,
    [RoomNumber] nvarchar(450) NOT NULL,
    [RoomTypeInfoId] int NULL,
    [RoomType] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [PricePerNight] decimal(18,2) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [MaxGuests] int NOT NULL,
    [Capacity] int NOT NULL,
    [BedType] nvarchar(max) NOT NULL,
    [HasKingBed] bit NOT NULL,
    [HasDoubleBeds] bit NOT NULL,
    [RoomSize] nvarchar(max) NOT NULL,
    [AvailabilityStatus] int NOT NULL,
    [IsAvailable] bit NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Rooms] PRIMARY KEY ([RoomId]),
    CONSTRAINT [FK_Rooms_RoomTypeInfos_RoomTypeInfoId] FOREIGN KEY ([RoomTypeInfoId]) REFERENCES [RoomTypeInfos] ([Id])
);

CREATE TABLE [Notifications] (
    [NotificationId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [Type] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ReadAt] datetime2 NULL,
    [IsRead] bit NOT NULL,
    [Priority] int NOT NULL,
    [RelatedEntityType] nvarchar(max) NULL,
    [RelatedEntityId] int NULL,
    [ActionLink] nvarchar(max) NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);

CREATE TABLE [UserActivityLogs] (
    [LogId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [AdminId] int NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_UserActivityLogs] PRIMARY KEY ([LogId]),
    CONSTRAINT [FK_UserActivityLogs_Users_AdminId] FOREIGN KEY ([AdminId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserActivityLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Bookings] (
    [BookingId] int NOT NULL IDENTITY,
    [Id] int NOT NULL,
    [UserId] int NOT NULL,
    [RoomId] int NOT NULL,
    [CheckInDate] datetime2 NOT NULL,
    [CheckOutDate] datetime2 NOT NULL,
    [NumberOfGuests] int NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [BookingDate] datetime2 NOT NULL,
    [AppliedDiscountId] int NULL,
    [DiscountAmount] decimal(18,2) NULL,
    [OriginalPrice] decimal(18,2) NULL,
    [SpecialRequests] nvarchar(500) NULL,
    [GuestUserId] int NULL,
    [GuestId] int NULL,
    [RoomId1] int NULL,
    CONSTRAINT [PK_Bookings] PRIMARY KEY ([BookingId]),
    CONSTRAINT [FK_Bookings_Discounts_AppliedDiscountId] FOREIGN KEY ([AppliedDiscountId]) REFERENCES [Discounts] ([DiscountId]),
    CONSTRAINT [FK_Bookings_Guests_GuestId] FOREIGN KEY ([GuestId]) REFERENCES [Guests] ([Id]),
    CONSTRAINT [FK_Bookings_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bookings_Rooms_RoomId1] FOREIGN KEY ([RoomId1]) REFERENCES [Rooms] ([RoomId]),
    CONSTRAINT [FK_Bookings_Users_GuestUserId] FOREIGN KEY ([GuestUserId]) REFERENCES [Users] ([UserId]),
    CONSTRAINT [FK_Bookings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [HousekeepingTasks] (
    [TaskId] int NOT NULL IDENTITY,
    [RoomId] int NOT NULL,
    [StaffId] int NULL,
    [TaskDescription] nvarchar(max) NOT NULL,
    [TaskType] int NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [AssignedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    [Priority] int NOT NULL,
    CONSTRAINT [PK_HousekeepingTasks] PRIMARY KEY ([TaskId]),
    CONSTRAINT [FK_HousekeepingTasks_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HousekeepingTasks_Users_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL
);

CREATE TABLE [MaintenanceRequests] (
    [Id] int NOT NULL IDENTITY,
    [RequestId] int NOT NULL,
    [RoomId] int NOT NULL,
    [ReportedById] int NULL,
    [TechnicianId] int NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Status] int NOT NULL,
    [IssueType] int NOT NULL,
    [Priority] int NOT NULL,
    [RequestDate] datetime2 NOT NULL,
    [ReportedAt] datetime2 NOT NULL,
    [ScheduledFor] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [Resolution] nvarchar(max) NULL,
    [CostOfRepair] decimal(18,2) NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_MaintenanceRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MaintenanceRequests_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MaintenanceRequests_Users_ReportedById] FOREIGN KEY ([ReportedById]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MaintenanceRequests_Users_TechnicianId] FOREIGN KEY ([TechnicianId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Reviews] (
    [ReviewId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [RoomId] int NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(max) NULL,
    [ReviewDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([ReviewId]),
    CONSTRAINT [FK_Reviews_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Reviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [BookedServices] (
    [BookingServiceId] int NOT NULL IDENTITY,
    [BookingId] int NOT NULL,
    [ServiceId] int NOT NULL,
    [Quantity] int NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_BookedServices] PRIMARY KEY ([BookingServiceId]),
    CONSTRAINT [FK_BookedServices_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BookedServices_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([ServiceId]) ON DELETE CASCADE
);

CREATE TABLE [Payments] (
    [PaymentId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [BookingId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [PaymentStatus] int NOT NULL,
    [PaymentMethod] int NOT NULL,
    [TransactionId] nvarchar(max) NULL,
    [Notes] nvarchar(500) NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PaymentDetails] nvarchar(max) NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Payments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'IsAvailable', N'Name', N'Price') AND [object_id] = OBJECT_ID(N'[HotelServices]'))
    SET IDENTITY_INSERT [HotelServices] ON;
INSERT INTO [HotelServices] ([Id], [Description], [IsAvailable], [Name], [Price])
VALUES (1, N'Daily room cleaning service', CAST(1 AS bit), N'Room Cleaning', 0.0),
(2, N'Enjoy a lavish breakfast buffet with international and local cuisine', CAST(1 AS bit), N'Breakfast Buffet', 1200.0),
(3, N'24/7 access to our fully equipped fitness center', CAST(1 AS bit), N'Gym Access', 500.0),
(4, N'Access to our infinity pool with stunning views', CAST(1 AS bit), N'Swimming Pool', 0.0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'IsAvailable', N'Name', N'Price') AND [object_id] = OBJECT_ID(N'[HotelServices]'))
    SET IDENTITY_INSERT [HotelServices] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoomId', N'AvailabilityStatus', N'BedType', N'Capacity', N'Description', N'HasDoubleBeds', N'HasKingBed', N'Id', N'ImageUrl', N'IsAvailable', N'MaxGuests', N'Name', N'Price', N'PricePerNight', N'RoomNumber', N'RoomSize', N'RoomType', N'RoomTypeInfoId') AND [object_id] = OBJECT_ID(N'[Rooms]'))
    SET IDENTITY_INSERT [Rooms] ON;
INSERT INTO [Rooms] ([RoomId], [AvailabilityStatus], [BedType], [Capacity], [Description], [HasDoubleBeds], [HasKingBed], [Id], [ImageUrl], [IsAvailable], [MaxGuests], [Name], [Price], [PricePerNight], [RoomNumber], [RoomSize], [RoomType], [RoomTypeInfoId])
VALUES (1, 0, N'King', 3, N'Experience luxury in our spacious Deluxe Room with modern amenities and elegant design.', CAST(0 AS bit), CAST(1 AS bit), 1, N'/images/deluxe_room.png', CAST(1 AS bit), 3, N'Deluxe', 22628.0, 22628.0, N'101', N'40 sq m', 0, NULL),
(2, 0, N'Double', 4, N'Upgrade your stay with our Deluxe Suite featuring a separate living area and premium furnishings.', CAST(1 AS bit), CAST(0 AS bit), 2, N'/images/deluxe-suite_room.png', CAST(1 AS bit), 4, N'DeluxeSuite', 33800.0, 33800.0, N'201', N'60 sq m', 1, NULL),
(3, 0, N'King and Double', 4, N'Our Executive Deluxe Room offers the ultimate luxury experience with panoramic views and exclusive amenities.', CAST(1 AS bit), CAST(1 AS bit), 3, N'/images/executive-deluxe_room.png', CAST(1 AS bit), 4, N'ExecutiveDeluxe', 55500.0, 55500.0, N'301', N'80 sq m', 2, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoomId', N'AvailabilityStatus', N'BedType', N'Capacity', N'Description', N'HasDoubleBeds', N'HasKingBed', N'Id', N'ImageUrl', N'IsAvailable', N'MaxGuests', N'Name', N'Price', N'PricePerNight', N'RoomNumber', N'RoomSize', N'RoomType', N'RoomTypeInfoId') AND [object_id] = OBJECT_ID(N'[Rooms]'))
    SET IDENTITY_INSERT [Rooms] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ServiceId', N'Description', N'Name', N'Price', N'ServiceName') AND [object_id] = OBJECT_ID(N'[Services]'))
    SET IDENTITY_INSERT [Services] ON;
INSERT INTO [Services] ([ServiceId], [Description], [Name], [Price], [ServiceName])
VALUES (1, N'Luxury transportation from the airport to the hotel', N'Airport Transfer', 2500.0, N'Airport Transfer'),
(2, N'Relaxing full body massage and spa treatment', N'Spa Treatment', 3500.0, N'Spa Treatment'),
(3, N'24/7 in-room dining service', N'Room Service', 500.0, N'Room Service'),
(4, N'Same-day laundry and dry cleaning service', N'Laundry Service', 1000.0, N'Laundry Service');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ServiceId', N'Description', N'Name', N'Price', N'ServiceName') AND [object_id] = OBJECT_ID(N'[Services]'))
    SET IDENTITY_INSERT [Services] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'CreatedAt', N'Email', N'FullName', N'Id', N'Password', N'Phone', N'PhoneNumber', N'UserType', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([UserId], [CreatedAt], [Email], [FullName], [Id], [Password], [Phone], [PhoneNumber], [UserType], [Username])
VALUES (1, '2023-01-01T12:00:00.0000000', N'admin@royalstay.com', N'Admin User', 1, N'Admin123!', N'123-456-7890', N'123-456-7890', 1, N'admin');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'CreatedAt', N'Email', N'FullName', N'Id', N'Password', N'Phone', N'PhoneNumber', N'UserType', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] OFF;

CREATE INDEX [IX_BookedServices_BookingId] ON [BookedServices] ([BookingId]);

CREATE INDEX [IX_BookedServices_ServiceId] ON [BookedServices] ([ServiceId]);

CREATE INDEX [IX_Bookings_AppliedDiscountId] ON [Bookings] ([AppliedDiscountId]);

CREATE INDEX [IX_Bookings_GuestId] ON [Bookings] ([GuestId]);

CREATE INDEX [IX_Bookings_GuestUserId] ON [Bookings] ([GuestUserId]);

CREATE INDEX [IX_Bookings_RoomId] ON [Bookings] ([RoomId]);

CREATE INDEX [IX_Bookings_RoomId1] ON [Bookings] ([RoomId1]);

CREATE INDEX [IX_Bookings_UserId] ON [Bookings] ([UserId]);

CREATE INDEX [IX_HousekeepingTasks_RoomId] ON [HousekeepingTasks] ([RoomId]);

CREATE INDEX [IX_HousekeepingTasks_StaffId] ON [HousekeepingTasks] ([StaffId]);

CREATE INDEX [IX_MaintenanceRequests_ReportedById] ON [MaintenanceRequests] ([ReportedById]);

CREATE INDEX [IX_MaintenanceRequests_RoomId] ON [MaintenanceRequests] ([RoomId]);

CREATE INDEX [IX_MaintenanceRequests_TechnicianId] ON [MaintenanceRequests] ([TechnicianId]);

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);

CREATE INDEX [IX_Payments_BookingId] ON [Payments] ([BookingId]);

CREATE INDEX [IX_Payments_UserId] ON [Payments] ([UserId]);

CREATE INDEX [IX_Reviews_RoomId] ON [Reviews] ([RoomId]);

CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);

CREATE UNIQUE INDEX [IX_Rooms_RoomNumber] ON [Rooms] ([RoomNumber]);

CREATE INDEX [IX_Rooms_RoomTypeInfoId] ON [Rooms] ([RoomTypeInfoId]);

CREATE INDEX [IX_UserActivityLogs_AdminId] ON [UserActivityLogs] ([AdminId]);

CREATE INDEX [IX_UserActivityLogs_UserId] ON [UserActivityLogs] ([UserId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250427145658_InitialCreate', N'9.0.4');

ALTER TABLE [BookedServices] DROP CONSTRAINT [FK_BookedServices_Bookings_BookingId];

ALTER TABLE [BookedServices] DROP CONSTRAINT [FK_BookedServices_Services_ServiceId];

EXEC sp_rename N'[BookedServices].[BookingServiceId]', N'Id', 'COLUMN';

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BookedServices]') AND [c].[name] = N'BookingId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [BookedServices] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [BookedServices] ALTER COLUMN [BookingId] int NULL;

ALTER TABLE [BookedServices] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [BookedServices] ADD [Notes] nvarchar(500) NULL;

ALTER TABLE [BookedServices] ADD [RequestDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [BookedServices] ADD [RequestTime] time NOT NULL DEFAULT '00:00:00';

ALTER TABLE [BookedServices] ADD [ServiceId1] int NULL;

ALTER TABLE [BookedServices] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [BookedServices] ADD [Quantity] int NOT NULL DEFAULT 1;

ALTER TABLE [BookedServices] ADD [TotalPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [BookedServices] ADD [UserId] nvarchar(max) NULL;

CREATE INDEX [IX_BookedServices_ServiceId1] ON [BookedServices] ([ServiceId1]);

ALTER TABLE [BookedServices] ADD CONSTRAINT [FK_BookedServices_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]);

ALTER TABLE [BookedServices] ADD CONSTRAINT [FK_BookedServices_HotelServices_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [HotelServices] ([Id]) ON DELETE CASCADE;

ALTER TABLE [BookedServices] ADD CONSTRAINT [FK_BookedServices_Services_ServiceId1] FOREIGN KEY ([ServiceId1]) REFERENCES [Services] ([ServiceId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250429104245_UpdateBookedServiceTable', N'9.0.4');

ALTER TABLE [HotelServices] ADD [ServiceType] int NOT NULL DEFAULT 0;

UPDATE [HotelServices] SET [ServiceType] = 1
WHERE [Id] = 1;
SELECT @@ROWCOUNT;


UPDATE [HotelServices] SET [ServiceType] = 1
WHERE [Id] = 2;
SELECT @@ROWCOUNT;


UPDATE [HotelServices] SET [ServiceType] = 1
WHERE [Id] = 3;
SELECT @@ROWCOUNT;


UPDATE [HotelServices] SET [ServiceType] = 1
WHERE [Id] = 4;
SELECT @@ROWCOUNT;


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'IsAvailable', N'Name', N'Price', N'ServiceType') AND [object_id] = OBJECT_ID(N'[HotelServices]'))
    SET IDENTITY_INSERT [HotelServices] ON;
INSERT INTO [HotelServices] ([Id], [Description], [IsAvailable], [Name], [Price], [ServiceType])
VALUES (5, N'Private luxury transportation to and from the airport', CAST(1 AS bit), N'Airport Transfers', 2500.0, 0),
(6, N'Explore the city at your own pace with our premium bikes', CAST(1 AS bit), N'Bike Rentals', 800.0, 0),
(7, N'Assistance with reservations, tickets, and local recommendations', CAST(1 AS bit), N'Concierge Services', 0.0, 0),
(8, N'Professional translators available upon request', CAST(1 AS bit), N'Translation Services', 1500.0, 0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'IsAvailable', N'Name', N'Price', N'ServiceType') AND [object_id] = OBJECT_ID(N'[HotelServices]'))
    SET IDENTITY_INSERT [HotelServices] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250429150229_AddServiceTypeColumn', N'9.0.4');

COMMIT;
GO

