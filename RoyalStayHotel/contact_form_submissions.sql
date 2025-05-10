-- Create the ContactFormSubmissions table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ContactFormSubmissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ContactFormSubmissions](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Email] [nvarchar](100) NOT NULL,
        [PhoneNumber] [nvarchar](20) NOT NULL,
        [Subject] [nvarchar](100) NOT NULL,
        [Message] [nvarchar](max) NOT NULL,
        [SubmissionDate] [datetime2](7) NOT NULL,
        [IsRead] [bit] NOT NULL,
        CONSTRAINT [PK_ContactFormSubmissions] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        )
    )
END
GO

-- Create index on SubmissionDate for efficient sorting
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ContactFormSubmissions_SubmissionDate')
BEGIN
    CREATE INDEX [IX_ContactFormSubmissions_SubmissionDate] ON [dbo].[ContactFormSubmissions] ([SubmissionDate] DESC)
END
GO

-- Create index on IsRead for efficient filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ContactFormSubmissions_IsRead')
BEGIN
    CREATE INDEX [IX_ContactFormSubmissions_IsRead] ON [dbo].[ContactFormSubmissions] ([IsRead])
END
GO

-- Insert sample data
IF NOT EXISTS (SELECT TOP 1 * FROM [dbo].[ContactFormSubmissions])
BEGIN
    INSERT INTO [dbo].[ContactFormSubmissions] 
        ([Name], [Email], [PhoneNumber], [Subject], [Message], [SubmissionDate], [IsRead])
    VALUES 
        ('John Doe', 'john.doe@example.com', '+1234567890', 'Reservation Inquiry', 'I would like to inquire about availability for a Deluxe Room for 2 adults from June 15-20. What are the rates and is airport pickup available?', DATEADD(DAY, -5, GETDATE()), 1),
        ('Jane Smith', 'jane.smith@example.com', '+9876543210', 'Special Request', 'We are celebrating our anniversary and would like to request a room with a view. Also, is it possible to arrange for a surprise cake and champagne?', DATEADD(DAY, -2, GETDATE()), 0),
        ('Robert Johnson', 'robert.j@example.com', '+2345678901', 'Feedback', 'I recently stayed at your hotel and wanted to express my appreciation for the excellent service provided by your staff, particularly by Maria at the front desk who went above and beyond to make our stay memorable.', DATEADD(HOUR, -12, GETDATE()), 0)
END
GO 