IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[User]'))
ALTER FULLTEXT INDEX ON [dbo].[User] DISABLE

GO
IF  EXISTS (SELECT * FROM sys.fulltext_indexes fti WHERE fti.object_id = OBJECT_ID(N'[dbo].[User]'))
BEGIN
	DROP FULLTEXT INDEX ON [dbo].[User]
End

Go
IF EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE [name]='FTCUser')
BEGIN
	DROP FULLTEXT CATALOG FTCUser
END

CREATE FULLTEXT CATALOG FTCUser AS DEFAULT;
CREATE FULLTEXT INDEX ON [dbo].[User]("Name", "NickName", "Id") KEY INDEX PK_User ON FTCUser WITH STOPLIST = OFF, CHANGE_TRACKING AUTO;
