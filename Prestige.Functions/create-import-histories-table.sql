-- Create ImportHistories table for tracking Spotify data imports
CREATE TABLE ImportHistories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    FileHash NVARCHAR(64) NOT NULL, -- SHA-256 hash is 64 characters
    FileName NVARCHAR(500) NOT NULL,
    BatchId NVARCHAR(50) NOT NULL,
    ImportDate DATETIME2 NOT NULL,
    Status NVARCHAR(100) NOT NULL,
    RecordCount INT NOT NULL,
    MinTimestamp DATETIME2 NULL,
    MaxTimestamp DATETIME2 NULL,
    
    -- Index for fast duplicate checking
    INDEX IX_ImportHistories_UserIdFileHash (UserId, FileHash),
    
    -- Index for overlap checking
    INDEX IX_ImportHistories_UserIdTimestamps (UserId, MinTimestamp, MaxTimestamp)
);