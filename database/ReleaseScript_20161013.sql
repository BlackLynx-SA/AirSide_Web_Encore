/*
	Release Scripts for Encore Version 4.5.0.8
	Release date: 2016/10/14
*/

/*
	New Methodology for FB Tech upload
*/
IF OBJECT_ID('dbo.as_photometricProfile', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_photometricProfile];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[as_photometricProfile](
	[id] [uniqueidentifier] NOT NULL,
	[dateOfRun] [datetime] NOT NULL,
	[assetId] [int] NOT NULL,
	[maxIntensity] [int] NOT NULL,
	[averageIntensity] [int] NOT NULL,
	[icaoPercentage] [int] NOT NULL,
	[pictureUrl] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_as_photometricProfile] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];

GO

--Drop Existing FB Tech Table
IF OBJECT_ID('dbo.as_fbTechProfile', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_fbTechProfile];
GO

/*
	Add Id field to make asset status immutable
*/
BEGIN TRANSACTION;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
COMMIT;
BEGIN TRANSACTION;
GO
ALTER TABLE dbo.as_assetStatusProfile
	DROP CONSTRAINT DF_as_assetStatusProfile_dt_lastUpdated;
GO
CREATE TABLE dbo.Tmp_as_assetStatusProfile
	(
	id INT NOT NULL IDENTITY (1, 1),
	i_assetProfileId INT NOT NULL,
	bt_assetStatus BIT NOT NULL,
	i_assetSeverity INT NOT NULL,
	dt_lastUpdated DATETIME NOT NULL
	)  ON [PRIMARY];
GO
ALTER TABLE dbo.Tmp_as_assetStatusProfile SET (LOCK_ESCALATION = TABLE);
GO
ALTER TABLE dbo.Tmp_as_assetStatusProfile ADD CONSTRAINT
	DF_as_assetStatusProfile_dt_lastUpdated DEFAULT (GETDATE()) FOR dt_lastUpdated;
GO
SET IDENTITY_INSERT dbo.Tmp_as_assetStatusProfile OFF;
GO
IF EXISTS(SELECT * FROM dbo.as_assetStatusProfile)
	 EXECUTE('INSERT INTO dbo.Tmp_as_assetStatusProfile (i_assetProfileId, bt_assetStatus, i_assetSeverity, dt_lastUpdated)
		SELECT i_assetProfileId, bt_assetStatus, i_assetSeverity, dt_lastUpdated FROM dbo.as_assetStatusProfile WITH (HOLDLOCK TABLOCKX)');
GO
DROP TABLE dbo.as_assetStatusProfile;
GO
EXECUTE sp_rename N'dbo.Tmp_as_assetStatusProfile', N'as_assetStatusProfile', 'OBJECT'; 
GO
ALTER TABLE dbo.as_assetStatusProfile ADD CONSTRAINT
	PK_as_assetStatusProfile PRIMARY KEY CLUSTERED 
	(
	id
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY];

GO
COMMIT;

/*
	Add Maintenance Id to Validation Task for AdHoc Maintenance Tasks
*/
BEGIN TRANSACTION;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
COMMIT;
BEGIN TRANSACTION;
GO
ALTER TABLE dbo.as_validationTaskProfile ADD
	i_maintenanceId INT NOT NULL CONSTRAINT DF_as_validationTaskProfile_i_maintenanceId_1 DEFAULT 0;
GO
ALTER TABLE dbo.as_validationTaskProfile SET (LOCK_ESCALATION = TABLE);
GO
COMMIT;


/*
	Table Cleanup
*/

IF OBJECT_ID('dbo.as_cacheProfile', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_cacheProfile];
GO

IF OBJECT_ID('dbo.as_electricalNodeProfile', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_electricalNodeProfile];
Go

IF OBJECT_ID('dbo.as_iosLogProfile', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_iosLogProfile];
 go

IF OBJECT_ID('dbo.as_iosImageProfile', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_iosImageProfile];
 go

IF OBJECT_ID('dbo.as_nodeRegionProfile', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_nodeRegionProfile];
 go

IF OBJECT_ID('dbo.as_reportParameters', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_reportParameters];
 go

 IF OBJECT_ID('dbo.as_reportProfile', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_reportProfile];
 go

IF OBJECT_ID('dbo.as_todoCategories', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_todoCategories];
 go

IF OBJECT_ID('dbo.as_userExternalSession', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_userExternalSession];
 go

IF OBJECT_ID('dbo.as_wrenchProfile', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_wrenchProfile];
 go

IF OBJECT_ID('dbo.as_todoProfile', 'U') IS NOT NULL 
	DROP TABLE [dbo].[as_todoProfile];
 go