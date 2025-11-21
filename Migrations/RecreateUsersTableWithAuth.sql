-- Drop and Recreate Users Table with Authentication Fields
-- Run this script manually in Azure Data Studio or SQL Server Management Studio

-- Step 1: Drop foreign key constraints
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Conversations_Users_UserId')
BEGIN
    ALTER TABLE Conversations DROP CONSTRAINT FK_Conversations_Users_UserId;
    PRINT 'Dropped foreign key constraint FK_Conversations_Users_UserId';
END

-- Step 2: Drop existing Users table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    DROP TABLE Users;
    PRINT 'Dropped Users table';
END

-- Step 3: Create new Users table with authentication fields
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(MAX) NOT NULL,
    Email NVARCHAR(450) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'User',
    EmailVerified BIT NOT NULL DEFAULT 0,
    EmailVerificationToken NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create unique index on Email
CREATE UNIQUE INDEX IX_Users_Email ON Users(Email);

PRINT 'Created new Users table with authentication fields';

-- Step 4: Recreate foreign key constraint
ALTER TABLE Conversations 
ADD CONSTRAINT FK_Conversations_Users_UserId 
FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE;

PRINT 'Recreated foreign key constraint';

PRINT 'Migration complete! Users table has been recreated with authentication support.';
