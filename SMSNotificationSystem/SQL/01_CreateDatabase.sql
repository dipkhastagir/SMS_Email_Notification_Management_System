/* =====================================================================
   NotifyHub - SMS/Email Notification Management System
   Script 01 : Create the database
   Run in SQL Server Management Studio (SSMS) connected to your server
   (e.g. "." , "localhost" or ".\SQLEXPRESS").
   ===================================================================== */
USE master;
GO

IF DB_ID(N'SmsNotificationDB') IS NULL
BEGIN
    CREATE DATABASE SmsNotificationDB;
    PRINT 'Database SmsNotificationDB created.';
END
ELSE
    PRINT 'Database SmsNotificationDB already exists - skipping.';
GO

USE SmsNotificationDB;
GO
