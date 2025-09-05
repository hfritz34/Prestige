-- Add indexes for LastUpdatedAt fields to optimize "recently updated" queries
-- Run this script to add performance indexes for the LastUpdatedAt fields

-- Index for UserTrack LastUpdatedAt (for recently updated tracks)
CREATE NONCLUSTERED INDEX IX_UserTrack_LastUpdatedAt
ON UserTrack (LastUpdatedAt DESC);

-- Composite index for user-specific recently updated tracks
CREATE NONCLUSTERED INDEX IX_UserTrack_UserId_LastUpdatedAt  
ON UserTrack (UserId, LastUpdatedAt DESC);

-- Index for UserAlbum LastUpdatedAt (for recently updated albums)
CREATE NONCLUSTERED INDEX IX_UserAlbum_LastUpdatedAt
ON UserAlbum (LastUpdatedAt DESC);

-- Composite index for user-specific recently updated albums
CREATE NONCLUSTERED INDEX IX_UserAlbum_UserId_LastUpdatedAt
ON UserAlbum (UserId, LastUpdatedAt DESC);

-- Index for UserArtist LastUpdatedAt (for recently updated artists)  
CREATE NONCLUSTERED INDEX IX_UserArtist_LastUpdatedAt
ON UserArtist (LastUpdatedAt DESC);

-- Composite index for user-specific recently updated artists
CREATE NONCLUSTERED INDEX IX_UserArtist_UserId_LastUpdatedAt
ON UserArtist (UserId, LastUpdatedAt DESC);

-- Additional useful composite indexes for performance
-- These indexes support queries that filter by user and order by multiple fields

-- Combined TotalTime + LastUpdatedAt for complex sorting
CREATE NONCLUSTERED INDEX IX_UserTrack_UserId_TotalTime_LastUpdatedAt
ON UserTrack (UserId, TotalTime DESC, LastUpdatedAt DESC);

CREATE NONCLUSTERED INDEX IX_UserAlbum_UserId_TotalTime_LastUpdatedAt  
ON UserAlbum (UserId, TotalTime DESC, LastUpdatedAt DESC);

CREATE NONCLUSTERED INDEX IX_UserArtist_UserId_TotalTime_LastUpdatedAt
ON UserArtist (UserId, TotalTime DESC, LastUpdatedAt DESC);