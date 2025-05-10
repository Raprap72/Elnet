-- Check if Quantity column doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'Quantity' AND Object_ID = Object_ID('BookedServices'))
BEGIN
    ALTER TABLE BookedServices
    ADD Quantity INT NOT NULL DEFAULT 1
END

-- Check if TotalPrice column doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'TotalPrice' AND Object_ID = Object_ID('BookedServices'))
BEGIN
    ALTER TABLE BookedServices
    ADD TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0
END

-- Check if Notes column doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'Notes' AND Object_ID = Object_ID('BookedServices'))
BEGIN
    ALTER TABLE BookedServices
    ADD Notes NVARCHAR(500) NULL
END

-- Check if RequestDate column doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'RequestDate' AND Object_ID = Object_ID('BookedServices'))
BEGIN
    ALTER TABLE BookedServices
    ADD RequestDate DATETIME2 NOT NULL DEFAULT GETDATE()
END

-- Check if RequestTime column doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'RequestTime' AND Object_ID = Object_ID('BookedServices'))
BEGIN
    ALTER TABLE BookedServices
    ADD RequestTime TIME NOT NULL DEFAULT '12:00:00'
END

-- Check if Status column doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'Status' AND Object_ID = Object_ID('BookedServices'))
BEGIN
    ALTER TABLE BookedServices
    ADD Status NVARCHAR(50) NOT NULL DEFAULT 'Pending'
END

-- Check if CreatedAt column doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'CreatedAt' AND Object_ID = Object_ID('BookedServices'))
BEGIN
    ALTER TABLE BookedServices
    ADD CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
END

-- Check if UserId column doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'UserId' AND Object_ID = Object_ID('BookedServices'))
BEGIN
    ALTER TABLE BookedServices
    ADD UserId NVARCHAR(450) NULL
END 