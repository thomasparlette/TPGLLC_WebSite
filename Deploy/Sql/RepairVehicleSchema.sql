-- Run against the database used by ConnectionStrings:WebsiteDb, after confirming
-- the database name and taking a backup. Existing tables and data are untouched.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.AppointmentRequests', N'U') IS NULL
    THROW 50001, 'Expected AppointmentRequests table is absent. Verify the target database before running this repair.', 1;

IF COL_LENGTH(N'dbo.AppointmentRequests', N'VehicleSubmodel') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD VehicleSubmodel nvarchar(120) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'BodyStyle') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD BodyStyle nvarchar(80) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'EngineFuel') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD EngineFuel nvarchar(160) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'Transmission') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD Transmission nvarchar(120) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'DriveType') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD DriveType nvarchar(60) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'Brake') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD Brake nvarchar(80) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'Gvw') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD Gvw nvarchar(40) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'LicensePlate') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD LicensePlate nvarchar(25) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'StateProvince') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD StateProvince nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'UnitNumber') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD UnitNumber nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'FleetNumber') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD FleetNumber nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'Color') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD Color nvarchar(60) NULL;
IF COL_LENGTH(N'dbo.AppointmentRequests', N'VehicleMemo') IS NULL
    ALTER TABLE dbo.AppointmentRequests ADD VehicleMemo nvarchar(2000) NULL;

IF OBJECT_ID(N'dbo.VehicleCatalogOptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleCatalogOptions
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleCatalogOptions PRIMARY KEY,
        Category nvarchar(40) NOT NULL,
        Value nvarchar(200) NOT NULL,
        Source nvarchar(40) NOT NULL,
        SyncedAtUtc datetimeoffset NOT NULL
    );
    CREATE UNIQUE INDEX IX_VehicleCatalogOptions_Category_Value
        ON dbo.VehicleCatalogOptions(Category, Value);
    CREATE INDEX IX_VehicleCatalogOptions_Category ON dbo.VehicleCatalogOptions(Category);
END;
COMMIT TRANSACTION;
