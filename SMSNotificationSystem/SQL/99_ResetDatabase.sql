/* =====================================================================
   Script 99 : DANGER - drops the whole database so you can start over.
   Stop the web application first (Hangfire keeps connections open).
   ===================================================================== */
USE master;
GO
IF DB_ID(N'SmsNotificationDB') IS NOT NULL
BEGIN
    ALTER DATABASE SmsNotificationDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SmsNotificationDB;
    PRINT 'SmsNotificationDB dropped.';
END
GO
