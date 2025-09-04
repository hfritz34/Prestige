-- Script to fix LastUpdatedAt values for existing records
-- This addresses the issue where existing records have invalid DateTime values after migration

-- Update UserTrack records with invalid LastUpdatedAt
UPDATE UserTrack
SET LastUpdatedAt = GETUTCDATE()
WHERE LastUpdatedAt = '0001-01-01 00:00:00.0000000'
   OR LastUpdatedAt IS NULL;

-- Update UserAlbum records with invalid LastUpdatedAt  
UPDATE UserAlbum
SET LastUpdatedAt = GETUTCDATE()
WHERE LastUpdatedAt = '0001-01-01 00:00:00.0000000'
   OR LastUpdatedAt IS NULL;

-- Update UserArtist records with invalid LastUpdatedAt
UPDATE UserArtist
SET LastUpdatedAt = GETUTCDATE()
WHERE LastUpdatedAt = '0001-01-01 00:00:00.0000000'
   OR LastUpdatedAt IS NULL;

-- Verify the updates
SELECT 'UserTrack' as TableName, COUNT(*) as InvalidRecords 
FROM UserTrack 
WHERE LastUpdatedAt = '0001-01-01 00:00:00.0000000'
UNION ALL
SELECT 'UserAlbum', COUNT(*) 
FROM UserAlbum 
WHERE LastUpdatedAt = '0001-01-01 00:00:00.0000000'
UNION ALL
SELECT 'UserArtist', COUNT(*) 
FROM UserArtist 
WHERE LastUpdatedAt = '0001-01-01 00:00:00.0000000';