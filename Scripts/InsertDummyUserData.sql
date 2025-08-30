-- =====================================================
-- Insert Dummy User Data for Friends Feature Testing
-- =====================================================

BEGIN TRANSACTION;

-- Variables for our test data
DECLARE @DummyUserId NVARCHAR(450) = 'dummy_spotify_user_12345';
DECLARE @ArtistId NVARCHAR(450) = '0uj6QiPsPfK8ywLC7uwBE1';
DECLARE @AlbumId NVARCHAR(450) = '075lV4wdtLwFvIvCUdSYhL';
DECLARE @AllStarTrackId NVARCHAR(450) = '0Vige5jUDF3YDLMuvdbjAo';
DECLARE @KillsTrackId NVARCHAR(450) = '77ipBv6VTnz7UewTIVScho';

-- 1. Insert dummy User (table name is User, not Users)
IF NOT EXISTS (SELECT 1 FROM [User] WHERE Id = @DummyUserId)
BEGIN
    INSERT INTO [User] (Id, Name, NickName, Email, ProfilePicURL, AccessToken, RefreshToken, ExpiresAt, IsSetup)
    VALUES (
        @DummyUserId,
        'Test Friend',
        'TestBuddy',
        'testfriend@example.com',
        'https://i.pravatar.cc/300?img=12', -- Random avatar
        'fake_access_token_12345',
        'fake_refresh_token_12345',
        DATEADD(HOUR, 1, GETDATE()), -- Expires in 1 hour (fake)
        1 -- IsSetup = true
    );
    PRINT 'Inserted dummy user';
END
ELSE
BEGIN
    PRINT 'Dummy user already exists';
END

-- 2. Insert or update Artist (table name is Artist, not Artists)
IF NOT EXISTS (SELECT 1 FROM Artist WHERE Id = @ArtistId)
BEGIN
    INSERT INTO Artist (Id, Name)
    VALUES (@ArtistId, 'OsamaSon');
    PRINT 'Inserted artist: OsamaSon';
END
ELSE
BEGIN
    PRINT 'Artist OsamaSon already exists';
END

-- 3. Insert artist images (table name is Image, not Images)
IF NOT EXISTS (SELECT 1 FROM Image WHERE ArtistId = @ArtistId)
BEGIN
    INSERT INTO Image (Url, Height, Width, ArtistId)
    VALUES 
        ('https://i.scdn.co/image/ab6761610000e5eb7f87ae635e2cd1647e710d73', 640, 640, @ArtistId),
        ('https://i.scdn.co/image/ab676161000051747f87ae635e2cd1647e710d73', 320, 320, @ArtistId),
        ('https://i.scdn.co/image/ab6761610000f1787f87ae635e2cd1647e710d73', 160, 160, @ArtistId);
    PRINT 'Inserted artist images';
END

-- 4. Insert or update Album (table name is Album, not Albums)
IF NOT EXISTS (SELECT 1 FROM Album WHERE Id = @AlbumId)
BEGIN
    INSERT INTO Album (Id, Name)
    VALUES (@AlbumId, 'Flex Musix');
    PRINT 'Inserted album: Flex Musix';
END
ELSE
BEGIN
    PRINT 'Album Flex Musix already exists';
END

-- 5. Insert album images
IF NOT EXISTS (SELECT 1 FROM Image WHERE AlbumId = @AlbumId)
BEGIN
    INSERT INTO Image (Url, Height, Width, AlbumId)
    VALUES 
        ('https://i.scdn.co/image/ab67616d0000b2735331adabe62a482b2daebed6', 640, 640, @AlbumId),
        ('https://i.scdn.co/image/ab67616d00001e025331adabe62a482b2daebed6', 300, 300, @AlbumId),
        ('https://i.scdn.co/image/ab67616d000048515331adabe62a482b2daebed6', 64, 64, @AlbumId);
    PRINT 'Inserted album images';
END

-- 6. Create Album-Artist relationship
IF NOT EXISTS (SELECT 1 FROM AlbumArtist WHERE AlbumId = @AlbumId AND ArtistsId = @ArtistId)
BEGIN
    INSERT INTO AlbumArtist (AlbumId, ArtistsId)
    VALUES (@AlbumId, @ArtistId);
    PRINT 'Created Album-Artist relationship';
END

-- 7. Insert tracks (table name is Track, not Tracks - note AlbumId is required)
IF NOT EXISTS (SELECT 1 FROM Track WHERE Id = @AllStarTrackId)
BEGIN
    INSERT INTO Track (Id, Name, DurationMs, AlbumId)
    VALUES (@AllStarTrackId, 'All Star', 121690, @AlbumId);
    PRINT 'Inserted track: All Star';
END

IF NOT EXISTS (SELECT 1 FROM Track WHERE Id = @KillsTrackId)
BEGIN
    INSERT INTO Track (Id, Name, DurationMs, AlbumId)
    VALUES (@KillsTrackId, 'Kills', 248192, @AlbumId);
    PRINT 'Inserted track: Kills';
END

-- 8. Create Artist-Track relationships
IF NOT EXISTS (SELECT 1 FROM ArtistTrack WHERE ArtistsId = @ArtistId AND TrackId = @AllStarTrackId)
BEGIN
    INSERT INTO ArtistTrack (ArtistsId, TrackId)
    VALUES (@ArtistId, @AllStarTrackId);
    PRINT 'Created Artist-Track relationship for All Star';
END

IF NOT EXISTS (SELECT 1 FROM ArtistTrack WHERE ArtistsId = @ArtistId AND TrackId = @KillsTrackId)
BEGIN
    INSERT INTO ArtistTrack (ArtistsId, TrackId)
    VALUES (@ArtistId, @KillsTrackId);
    PRINT 'Created Artist-Track relationship for Kills';
END

-- 9. Insert UserArtist data (table name is UserArtist, not UserArtists)
-- 2 hours = 7,200,000 ms
IF NOT EXISTS (SELECT 1 FROM UserArtist WHERE UserId = @DummyUserId AND ArtistId = @ArtistId)
BEGIN
    INSERT INTO UserArtist (UserId, ArtistId, TotalTime, IsFavorite, IsPinned, PersonalRatingScore, RatingPosition)
    VALUES (@DummyUserId, @ArtistId, 7200000, 1, 0, 10.0, 1);
    PRINT 'Inserted UserArtist data for OsamaSon';
END

-- 10. Insert UserAlbum data (table name is UserAlbum, not UserAlbums)
-- 2 hours = 7,200,000 ms
IF NOT EXISTS (SELECT 1 FROM UserAlbum WHERE UserId = @DummyUserId AND AlbumId = @AlbumId)
BEGIN
    INSERT INTO UserAlbum (UserId, AlbumId, TotalTime, IsFavorite, IsPinned, PersonalRatingScore, RatingPosition)
    VALUES (@DummyUserId, @AlbumId, 7200000, 1, 0, 10.0, 1);
    PRINT 'Inserted UserAlbum data for Flex Musix';
END

-- 11. Insert UserTrack data (table name is UserTrack, not UserTracks)
-- All Star: 1 hour = 3,600,000 ms, Position #2
IF NOT EXISTS (SELECT 1 FROM UserTrack WHERE UserId = @DummyUserId AND TrackId = @AllStarTrackId)
BEGIN
    INSERT INTO UserTrack (UserId, TrackId, TotalTime, IsFavorite, IsPinned, PersonalRatingScore, RatingPosition)
    VALUES (@DummyUserId, @AllStarTrackId, 3600000, 1, 0, 9.5, 2);
    PRINT 'Inserted UserTrack data for All Star';
END

-- Kills: 1 hour = 3,600,000 ms, Position #1
IF NOT EXISTS (SELECT 1 FROM UserTrack WHERE UserId = @DummyUserId AND TrackId = @KillsTrackId)
BEGIN
    INSERT INTO UserTrack (UserId, TrackId, TotalTime, IsFavorite, IsPinned, PersonalRatingScore, RatingPosition)
    VALUES (@DummyUserId, @KillsTrackId, 3600000, 1, 0, 10.0, 1);
    PRINT 'Inserted UserTrack data for Kills';
END

COMMIT TRANSACTION;

PRINT '=== DUMMY USER DATA INSERTION COMPLETE ===';
PRINT 'Dummy User ID: ' + @DummyUserId;
PRINT 'You can now add this user as a friend in your app to test the friends features.';

-- Optional: Display inserted data for verification
SELECT 'Users' as TableName, Id, Name, NickName, Email FROM [User] WHERE Id = @DummyUserId
UNION ALL
SELECT 'Artists' as TableName, Id, Name, '', '' FROM Artist WHERE Id = @ArtistId
UNION ALL
SELECT 'Albums' as TableName, Id, Name, '', '' FROM Album WHERE Id = @AlbumId;