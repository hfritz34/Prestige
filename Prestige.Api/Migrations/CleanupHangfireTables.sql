-- ============================================
-- Hangfire SQL Server Tables Cleanup Script
-- ============================================
-- WARNING: Only run this AFTER confirming Hangfire is successfully using Redis
-- and all SQL-based jobs have completed or been migrated.
-- 
-- IMPORTANT: Your Hangfire is now configured to use Redis as of the code review.
-- It's safe to drop these tables after verifying no pending jobs.

-- Step 1: Check if Hangfire schema exists and count jobs (DO THIS FIRST!)
SELECT 'Checking for Hangfire schema and remaining jobs...' AS Status;

-- Check if HangFire schema exists
IF EXISTS (SELECT * FROM sys.schemas WHERE name = 'HangFire')
BEGIN
    SELECT 'HangFire schema EXISTS' AS SchemaStatus;
    
    -- Check for remaining jobs if tables exist
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'HangFire' AND TABLE_NAME = 'Job')
    BEGIN
        SELECT COUNT(*) AS PendingJobs FROM [HangFire].[Job] WHERE StateName = 'Enqueued';
        SELECT COUNT(*) AS ProcessingJobs FROM [HangFire].[Job] WHERE StateName = 'Processing';
        SELECT COUNT(*) AS ScheduledJobs FROM [HangFire].[Job] WHERE StateName = 'Scheduled';
        SELECT COUNT(*) AS FailedJobs FROM [HangFire].[Job] WHERE StateName = 'Failed';
        SELECT COUNT(*) AS TotalJobs FROM [HangFire].[Job];
    END
    ELSE
    BEGIN
        SELECT 'HangFire.Job table does not exist' AS TableStatus;
    END
END
ELSE
BEGIN
    SELECT 'HangFire schema does NOT exist - nothing to clean up' AS SchemaStatus;
END

-- If the counts above are all 0, proceed with cleanup
-- Otherwise, wait for jobs to complete or manually handle them

-- Step 2: Drop Hangfire tables (uncomment these lines when ready to execute)
/*
BEGIN TRANSACTION;

BEGIN TRY
    -- Drop all Hangfire tables
    DROP TABLE IF EXISTS [HangFire].[AggregatedCounter];
    DROP TABLE IF EXISTS [HangFire].[Counter];
    DROP TABLE IF EXISTS [HangFire].[Hash];
    DROP TABLE IF EXISTS [HangFire].[JobParameter];
    DROP TABLE IF EXISTS [HangFire].[JobQueue];
    DROP TABLE IF EXISTS [HangFire].[List];
    DROP TABLE IF EXISTS [HangFire].[Server];
    DROP TABLE IF EXISTS [HangFire].[Set];
    DROP TABLE IF EXISTS [HangFire].[State];
    DROP TABLE IF EXISTS [HangFire].[Job];
    DROP TABLE IF EXISTS [HangFire].[Schema];

    -- Drop the Hangfire schema
    DROP SCHEMA IF EXISTS [HangFire];

    PRINT 'Hangfire tables and schema dropped successfully.';
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error occurred during cleanup:';
    PRINT ERROR_MESSAGE();
END CATCH;
*/

-- Step 3: Check space saved (run after dropping tables)
/*
-- Check database size reduction
EXEC sp_spaceused;

-- Check individual table sizes (should show no HangFire tables)
SELECT 
    s.Name AS SchemaName,
    t.Name AS TableName,
    p.rows AS RowCounts,
    CAST(ROUND((SUM(a.total_pages) * 8) / 1024.0, 2) AS NUMERIC(18, 2)) AS TotalSpaceMB
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.indexes i ON t.object_id = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.is_ms_shipped = 0 AND i.object_id > 255
GROUP BY s.Name, t.Name, p.Rows
ORDER BY TotalSpaceMB DESC;
*/

-- Step 4: Reclaim space (optional - causes brief lock)
/*
-- Shrink database to reclaim space
DBCC SHRINKDATABASE (N'prestigesqldb', 10);

-- Update statistics after cleanup
UPDATE STATISTICS Ratings;
UPDATE STATISTICS Users;
UPDATE STATISTICS UserTrack;
UPDATE STATISTICS UserAlbum;
UPDATE STATISTICS UserArtist;
*/