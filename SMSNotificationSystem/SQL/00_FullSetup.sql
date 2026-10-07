/* =====================================================================
   00_FullSetup.sql  -  ONE-CLICK SETUP
   Open this single file in SSMS and press Execute (F5).
   It is scripts 01 to 05 joined together, in order.
   ===================================================================== */

/* ---------------------------- 01_CreateDatabase.sql ---------------------------- */
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


/* ---------------------------- 02_CreateTables.sql ---------------------------- */
/* =====================================================================
   Script 02 : Tables, keys, constraints and indexes
   Safe to re-run: every table is created only if it does not exist.
   Users are never hard-deleted (they are deactivated), so user FKs use NO ACTION.
   Hangfire creates its own tables automatically (schema [HangFire])
   the first time the web application starts.
   ===================================================================== */
USE SmsNotificationDB;
GO

/* Stop if an OLDER version of this database (different columns) already exists */
IF (OBJECT_ID(N'dbo.EventTriggers', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.EventTriggers', N'Name') IS NULL)
   OR (OBJECT_ID(N'dbo.MessageQueue', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.MessageQueue', N'ReferenceKey') IS NULL)
   OR (OBJECT_ID(N'dbo.GatewaySettings', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.GatewaySettings', N'Channel') IS NULL)
BEGIN
    RAISERROR(N'SmsNotificationDB contains tables from an older version of the project. Stop the web app, run SQL\99_ResetDatabase.sql, then run 00_FullSetup.sql again.', 16, 1);
    SET NOEXEC ON;   -- skip the rest of the script
END
GO

/* ---------- Users & roles (Administrator, Accountant, HR, Manager) ---------- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users
(
    UserId        INT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
    FullName      NVARCHAR(120)  NOT NULL,
    Email         NVARCHAR(150)  NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    PasswordHash  NVARCHAR(200)  NOT NULL,           -- BCrypt (one-way, salted)
    Role          NVARCHAR(30)   NOT NULL
                  CONSTRAINT CK_Users_Role CHECK (Role IN (N'Administrator', N'Accountant', N'HR', N'Manager')),
    IsActive      BIT            NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt     DATETIME2(0)   NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSDATETIME()),
    LastLoginAt   DATETIME2(0)   NULL
);
GO

/* ---------- Message template categories ---------- */
IF OBJECT_ID(N'dbo.TemplateCategories', N'U') IS NULL
CREATE TABLE dbo.TemplateCategories
(
    CategoryId   INT IDENTITY(1,1) CONSTRAINT PK_TemplateCategories PRIMARY KEY,
    Name         NVARCHAR(80)  NOT NULL CONSTRAINT UQ_TemplateCategories_Name UNIQUE,
    Description  NVARCHAR(250) NULL
);
GO

/* ---------- Reusable, placeholder-based message templates ---------- */
IF OBJECT_ID(N'dbo.MessageTemplates', N'U') IS NULL
CREATE TABLE dbo.MessageTemplates
(
    TemplateId  INT IDENTITY(1,1) CONSTRAINT PK_MessageTemplates PRIMARY KEY,
    Name        NVARCHAR(150)  NOT NULL,
    CategoryId  INT            NOT NULL
                CONSTRAINT FK_MessageTemplates_Category REFERENCES dbo.TemplateCategories(CategoryId),
    Channel     NVARCHAR(10)   NOT NULL
                CONSTRAINT CK_MessageTemplates_Channel CHECK (Channel IN (N'SMS', N'Email')),
    Subject     NVARCHAR(200)  NULL,                 -- used by Email channel only
    Body        NVARCHAR(1000) NOT NULL,             -- e.g. Dear {CustomerName}, your due is Tk {DueAmount}
    Status      NVARCHAR(10)   NOT NULL CONSTRAINT DF_MessageTemplates_Status DEFAULT (N'Active')
                CONSTRAINT CK_MessageTemplates_Status CHECK (Status IN (N'Active', N'Inactive')),
    CreatedBy   INT            NULL
                CONSTRAINT FK_MessageTemplates_User REFERENCES dbo.Users(UserId),
    CreatedAt   DATETIME2(0)   NOT NULL CONSTRAINT DF_MessageTemplates_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt   DATETIME2(0)   NULL
);
GO

/* ---------- Event trigger rules (business condition -> template) ---------- */
IF OBJECT_ID(N'dbo.EventTriggers', N'U') IS NULL
CREATE TABLE dbo.EventTriggers
(
    TriggerId          INT IDENTITY(1,1) CONSTRAINT PK_EventTriggers PRIMARY KEY,
    Name               NVARCHAR(150) NOT NULL,
    EventType          NVARCHAR(30)  NOT NULL
                       CONSTRAINT CK_EventTriggers_EventType
                       CHECK (EventType IN (N'InvoiceDue', N'StockLow', N'AttendanceAbsent', N'SalaryDisbursed')),
    TemplateId         INT           NOT NULL
                       CONSTRAINT FK_EventTriggers_Template REFERENCES dbo.MessageTemplates(TemplateId),
    ConditionField     NVARCHAR(50)  NOT NULL,       -- e.g. StockQty
    ConditionOperator  NVARCHAR(3)   NOT NULL
                       CONSTRAINT CK_EventTriggers_Operator CHECK (ConditionOperator IN (N'<', N'<=', N'>', N'>=', N'=', N'!=')),
    ConditionValue     NVARCHAR(50)  NOT NULL,       -- literal (3) or another field (ReorderLevel)
    RecipientName      NVARCHAR(120) NULL,           -- fixed recipient for internal alerts (e.g. store manager)
    RecipientContact   NVARCHAR(150) NULL,
    CooldownHours      INT           NOT NULL CONSTRAINT DF_EventTriggers_Cooldown DEFAULT (24)
                       CONSTRAINT CK_EventTriggers_Cooldown CHECK (CooldownHours BETWEEN 1 AND 720),
    IsActive           BIT           NOT NULL CONSTRAINT DF_EventTriggers_IsActive DEFAULT (1),
    LastRunAt          DATETIME2(0)  NULL,
    CreatedAt          DATETIME2(0)  NOT NULL CONSTRAINT DF_EventTriggers_CreatedAt DEFAULT (SYSDATETIME())
);
GO

/* ---------- Message queue (separates generation from delivery) ---------- */
IF OBJECT_ID(N'dbo.MessageQueue', N'U') IS NULL
CREATE TABLE dbo.MessageQueue
(
    MessageId         INT IDENTITY(1,1) CONSTRAINT PK_MessageQueue PRIMARY KEY,
    TriggerId         INT            NULL
                      CONSTRAINT FK_MessageQueue_Trigger REFERENCES dbo.EventTriggers(TriggerId) ON DELETE SET NULL,
    TemplateId        INT            NULL
                      CONSTRAINT FK_MessageQueue_Template REFERENCES dbo.MessageTemplates(TemplateId) ON DELETE SET NULL,
    ReferenceKey      NVARCHAR(60)   NULL,           -- INV-12, PRD-4 ... used to avoid duplicate alerts
    RecipientName     NVARCHAR(120)  NOT NULL,
    RecipientContact  NVARCHAR(150)  NOT NULL,
    Channel           NVARCHAR(10)   NOT NULL
                      CONSTRAINT CK_MessageQueue_Channel CHECK (Channel IN (N'SMS', N'Email')),
    Subject           NVARCHAR(200)  NULL,
    MessageBody       NVARCHAR(1000) NOT NULL,
    Status            NVARCHAR(12)   NOT NULL CONSTRAINT DF_MessageQueue_Status DEFAULT (N'Pending')
                      CONSTRAINT CK_MessageQueue_Status
                      CHECK (Status IN (N'Pending', N'Processing', N'Sent', N'Retrying', N'Failed', N'Cancelled')),
    RetryCount        INT            NOT NULL CONSTRAINT DF_MessageQueue_RetryCount DEFAULT (0),
    CreatedBy         INT            NULL
                      CONSTRAINT FK_MessageQueue_User REFERENCES dbo.Users(UserId),
    CreatedAt         DATETIME2(0)   NOT NULL CONSTRAINT DF_MessageQueue_CreatedAt DEFAULT (SYSDATETIME()),
    LastAttemptAt     DATETIME2(0)   NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MessageQueue_Status')
    CREATE INDEX IX_MessageQueue_Status ON dbo.MessageQueue (Status, CreatedAt);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MessageQueue_Dedup')
    CREATE INDEX IX_MessageQueue_Dedup ON dbo.MessageQueue (TriggerId, ReferenceKey, CreatedAt);
GO

/* ---------- Delivery log (one row per dispatch attempt) ---------- */
IF OBJECT_ID(N'dbo.DeliveryLogs', N'U') IS NULL
CREATE TABLE dbo.DeliveryLogs
(
    LogId            INT IDENTITY(1,1) CONSTRAINT PK_DeliveryLogs PRIMARY KEY,
    MessageId        INT            NOT NULL
                     CONSTRAINT FK_DeliveryLogs_Message REFERENCES dbo.MessageQueue(MessageId) ON DELETE CASCADE,
    AttemptNo        INT            NOT NULL,
    GatewayProvider  NVARCHAR(80)   NOT NULL,
    GatewayResponse  NVARCHAR(500)  NOT NULL,
    Status           NVARCHAR(10)   NOT NULL CONSTRAINT CK_DeliveryLogs_Status CHECK (Status IN (N'Sent', N'Failed')),
    FailureReason    NVARCHAR(250)  NULL,
    AttemptedAt      DATETIME2(0)   NOT NULL CONSTRAINT DF_DeliveryLogs_AttemptedAt DEFAULT (SYSDATETIME())
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DeliveryLogs_AttemptedAt')
    CREATE INDEX IX_DeliveryLogs_AttemptedAt ON dbo.DeliveryLogs (AttemptedAt DESC) INCLUDE (Status, MessageId);
GO

/* ---------- SMS / Email gateway configuration ---------- */
IF OBJECT_ID(N'dbo.GatewaySettings', N'U') IS NULL
CREATE TABLE dbo.GatewaySettings
(
    GatewayId   INT IDENTITY(1,1) CONSTRAINT PK_GatewaySettings PRIMARY KEY,
    Provider    NVARCHAR(80)  NOT NULL,              -- Alpha SMS, Twilio, SMTP ...
    Channel     NVARCHAR(10)  NOT NULL
                CONSTRAINT CK_GatewaySettings_Channel CHECK (Channel IN (N'SMS', N'Email')),
    ApiKey      NVARCHAR(200) NOT NULL,
    SenderId    NVARCHAR(100) NOT NULL,              -- masking name / from address
    IsPrimary   BIT           NOT NULL CONSTRAINT DF_GatewaySettings_IsPrimary DEFAULT (0),
    IsActive    BIT           NOT NULL CONSTRAINT DF_GatewaySettings_IsActive DEFAULT (1),
    CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_GatewaySettings_CreatedAt DEFAULT (SYSDATETIME())
);
GO

/* ================= Business data that raises events ================= */

IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
CREATE TABLE dbo.Invoices
(
    InvoiceId      INT IDENTITY(1,1) CONSTRAINT PK_Invoices PRIMARY KEY,
    InvoiceNo      NVARCHAR(30)   NOT NULL CONSTRAINT UQ_Invoices_InvoiceNo UNIQUE,
    CustomerName   NVARCHAR(120)  NOT NULL,
    CustomerPhone  NVARCHAR(20)   NOT NULL,
    CustomerEmail  NVARCHAR(150)  NULL,
    Amount         DECIMAL(18,2)  NOT NULL CONSTRAINT CK_Invoices_Amount CHECK (Amount > 0),
    DueDate        DATE           NOT NULL,
    IsPaid         BIT            NOT NULL CONSTRAINT DF_Invoices_IsPaid DEFAULT (0),
    PaidAt         DATETIME2(0)   NULL,
    CreatedAt      DATETIME2(0)   NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT (SYSDATETIME())
);
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products
(
    ProductId     INT IDENTITY(1,1) CONSTRAINT PK_Products PRIMARY KEY,
    ProductName   NVARCHAR(120)  NOT NULL,
    Sku           NVARCHAR(40)   NULL,
    StockQty      INT            NOT NULL CONSTRAINT CK_Products_Stock CHECK (StockQty >= 0),
    ReorderLevel  INT            NOT NULL CONSTRAINT DF_Products_Reorder DEFAULT (10),
    UnitPrice     DECIMAL(18,2)  NOT NULL CONSTRAINT DF_Products_Price DEFAULT (0),
    UpdatedAt     DATETIME2(0)   NOT NULL CONSTRAINT DF_Products_UpdatedAt DEFAULT (SYSDATETIME())
);
GO

IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
CREATE TABLE dbo.Employees
(
    EmployeeId   INT IDENTITY(1,1) CONSTRAINT PK_Employees PRIMARY KEY,
    FullName     NVARCHAR(120)  NOT NULL,
    Phone        NVARCHAR(20)   NOT NULL,
    Email        NVARCHAR(150)  NULL,
    Department   NVARCHAR(80)   NOT NULL,
    Designation  NVARCHAR(80)   NULL,
    Salary       DECIMAL(18,2)  NOT NULL CONSTRAINT CK_Employees_Salary CHECK (Salary >= 0),
    IsActive     BIT            NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT (1),
    JoinedAt     DATE           NOT NULL CONSTRAINT DF_Employees_JoinedAt DEFAULT (CAST(SYSDATETIME() AS DATE))
);
GO

IF OBJECT_ID(N'dbo.Attendance', N'U') IS NULL
CREATE TABLE dbo.Attendance
(
    AttendanceId    INT IDENTITY(1,1) CONSTRAINT PK_Attendance PRIMARY KEY,
    EmployeeId      INT           NOT NULL
                    CONSTRAINT FK_Attendance_Employee REFERENCES dbo.Employees(EmployeeId),
    AttendanceDate  DATE          NOT NULL,
    Status          NVARCHAR(10)  NOT NULL
                    CONSTRAINT CK_Attendance_Status CHECK (Status IN (N'Present', N'Absent', N'Late', N'Leave')),
    Remarks         NVARCHAR(200) NULL,
    CONSTRAINT UQ_Attendance_EmployeeDate UNIQUE (EmployeeId, AttendanceDate)
);
GO

IF OBJECT_ID(N'dbo.SalaryPayments', N'U') IS NULL
CREATE TABLE dbo.SalaryPayments
(
    PaymentId    INT IDENTITY(1,1) CONSTRAINT PK_SalaryPayments PRIMARY KEY,
    EmployeeId   INT            NOT NULL
                 CONSTRAINT FK_SalaryPayments_Employee REFERENCES dbo.Employees(EmployeeId),
    SalaryMonth  CHAR(7)        NOT NULL,            -- yyyy-MM
    Amount       DECIMAL(18,2)  NOT NULL,
    PaidAt       DATETIME2(0)   NOT NULL CONSTRAINT DF_SalaryPayments_PaidAt DEFAULT (SYSDATETIME()),
    PaidBy       INT            NULL
                 CONSTRAINT FK_SalaryPayments_User REFERENCES dbo.Users(UserId),
    CONSTRAINT UQ_SalaryPayments_EmployeeMonth UNIQUE (EmployeeId, SalaryMonth)
);
GO

PRINT 'All tables are ready.';
GO


/* ---------------------------- 03_CreateViews.sql ---------------------------- */
/* =====================================================================
   Script 03 : Views used by the application for listing & reporting
   ===================================================================== */
USE SmsNotificationDB;
GO

CREATE OR ALTER VIEW dbo.vw_TemplateDetails
AS
SELECT  t.TemplateId, t.Name, c.Name AS CategoryName, t.CategoryId, t.Channel, t.Subject,
        t.Body, t.Status, t.CreatedAt, t.UpdatedAt, u.FullName AS CreatedByName,
        (SELECT COUNT(*) FROM dbo.EventTriggers tr WHERE tr.TemplateId = t.TemplateId) AS TriggerCount
FROM    dbo.MessageTemplates t
        INNER JOIN dbo.TemplateCategories c ON c.CategoryId = t.CategoryId
        LEFT  JOIN dbo.Users u ON u.UserId = t.CreatedBy;
GO

CREATE OR ALTER VIEW dbo.vw_TriggerDetails
AS
SELECT  tr.TriggerId, tr.Name, tr.EventType, tr.TemplateId, mt.Name AS TemplateName, mt.Channel,
        tr.ConditionField, tr.ConditionOperator, tr.ConditionValue,
        tr.RecipientName, tr.RecipientContact, tr.CooldownHours, tr.IsActive, tr.LastRunAt, tr.CreatedAt,
        (SELECT COUNT(*) FROM dbo.MessageQueue q WHERE q.TriggerId = tr.TriggerId) AS MessagesQueued
FROM    dbo.EventTriggers tr
        INNER JOIN dbo.MessageTemplates mt ON mt.TemplateId = tr.TemplateId;
GO

CREATE OR ALTER VIEW dbo.vw_QueueDetails
AS
SELECT  q.MessageId, q.TriggerId, tr.Name AS TriggerName, tr.EventType, q.ReferenceKey,
        q.RecipientName, q.RecipientContact, q.Channel, q.Subject, q.MessageBody,
        q.Status, q.RetryCount, q.CreatedAt, q.LastAttemptAt, u.FullName AS CreatedByName
FROM    dbo.MessageQueue q
        LEFT JOIN dbo.EventTriggers tr ON tr.TriggerId = q.TriggerId
        LEFT JOIN dbo.Users u ON u.UserId = q.CreatedBy;
GO

CREATE OR ALTER VIEW dbo.vw_DeliveryReport
AS
SELECT  dl.LogId, dl.MessageId, dl.AttemptNo, q.RecipientName, q.RecipientContact, q.Channel,
        tr.EventType, ISNULL(tr.Name, N'Manual message') AS TriggerName,
        dl.GatewayProvider, dl.GatewayResponse, dl.Status, dl.FailureReason, dl.AttemptedAt
FROM    dbo.DeliveryLogs dl
        INNER JOIN dbo.MessageQueue q ON q.MessageId = dl.MessageId
        LEFT  JOIN dbo.EventTriggers tr ON tr.TriggerId = q.TriggerId;
GO

CREATE OR ALTER VIEW dbo.vw_DashboardStats
AS
SELECT
    (SELECT COUNT(*) FROM dbo.EventTriggers WHERE IsActive = 1)                                  AS ActiveTriggers,
    (SELECT COUNT(*) FROM dbo.MessageTemplates WHERE Status = N'Active')                         AS ActiveTemplates,
    (SELECT COUNT(*) FROM dbo.MessageQueue WHERE Status IN (N'Pending', N'Processing'))          AS QueuedMessages,
    (SELECT COUNT(*) FROM dbo.MessageQueue WHERE Status = N'Retrying')                           AS RetryingMessages,
    (SELECT COUNT(*) FROM dbo.MessageQueue WHERE Status = N'Failed')                             AS FailedMessages,
    (SELECT COUNT(*) FROM dbo.DeliveryLogs WHERE Status = N'Sent'
            AND CAST(AttemptedAt AS DATE) = CAST(SYSDATETIME() AS DATE))                         AS SentToday,
    (SELECT COUNT(*) FROM dbo.DeliveryLogs WHERE Status = N'Failed'
            AND CAST(AttemptedAt AS DATE) = CAST(SYSDATETIME() AS DATE))                         AS FailedAttemptsToday,
    (SELECT COUNT(*) FROM dbo.DeliveryLogs WHERE Status = N'Sent')                               AS TotalSent,
    (SELECT COUNT(*) FROM dbo.DeliveryLogs)                                                      AS TotalAttempts,
    (SELECT COUNT(*) FROM dbo.Products WHERE StockQty <= ReorderLevel)                           AS LowStockItems,
    (SELECT COUNT(*) FROM dbo.Invoices WHERE IsPaid = 0)                                         AS UnpaidInvoices;
GO

CREATE OR ALTER VIEW dbo.vw_AttendanceDetails
AS
SELECT  a.AttendanceId, a.EmployeeId, e.FullName AS EmployeeName, e.Department, e.Phone, e.Email,
        a.AttendanceDate, a.Status, a.Remarks
FROM    dbo.Attendance a
        INNER JOIN dbo.Employees e ON e.EmployeeId = a.EmployeeId;
GO

CREATE OR ALTER VIEW dbo.vw_SalaryPayments
AS
SELECT  sp.PaymentId, sp.EmployeeId, e.FullName AS EmployeeName, e.Department, e.Phone, e.Email,
        sp.SalaryMonth, sp.Amount, sp.PaidAt, u.FullName AS PaidByName
FROM    dbo.SalaryPayments sp
        INNER JOIN dbo.Employees e ON e.EmployeeId = sp.EmployeeId
        LEFT  JOIN dbo.Users u ON u.UserId = sp.PaidBy;
GO

PRINT 'Views created.';
GO


/* ---------------------------- 04_CreateStoredProcedures.sql ---------------------------- */
/* =====================================================================
   Script 04 : Stored procedures called from the application (Dapper)
   ===================================================================== */
USE SmsNotificationDB;
GO

/* Adds a message to the queue unless the same trigger already produced a
   message for the same record inside the cooldown window.
   Returns the new MessageId, or 0 when it was skipped as a duplicate.   */
CREATE OR ALTER PROCEDURE dbo.sp_EnqueueMessage
    @TriggerId        INT            = NULL,
    @TemplateId       INT            = NULL,
    @ReferenceKey     NVARCHAR(60)   = NULL,
    @RecipientName    NVARCHAR(120),
    @RecipientContact NVARCHAR(150),
    @Channel          NVARCHAR(10),
    @Subject          NVARCHAR(200)  = NULL,
    @MessageBody      NVARCHAR(1000),
    @CreatedBy        INT            = NULL,
    @CooldownHours    INT            = 24,
    @OneTime          BIT            = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @TriggerId IS NOT NULL AND @ReferenceKey IS NOT NULL AND EXISTS
    (
        SELECT 1
        FROM   dbo.MessageQueue WITH (UPDLOCK, HOLDLOCK)
        WHERE  TriggerId = @TriggerId
          AND  ReferenceKey = @ReferenceKey
          AND  RecipientContact = @RecipientContact
          AND  Status <> N'Cancelled'
          AND  (@OneTime = 1 OR CreatedAt >= DATEADD(HOUR, -ISNULL(@CooldownHours, 24), SYSDATETIME()))
    )
    BEGIN
        SELECT CAST(0 AS INT) AS MessageId;
        RETURN;
    END

    INSERT INTO dbo.MessageQueue
        (TriggerId, TemplateId, ReferenceKey, RecipientName, RecipientContact, Channel,
         Subject, MessageBody, Status, RetryCount, CreatedBy, CreatedAt)
    VALUES
        (@TriggerId, @TemplateId, @ReferenceKey, @RecipientName, @RecipientContact, @Channel,
         @Subject, @MessageBody, N'Pending', 0, @CreatedBy, SYSDATETIME());

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS MessageId;
END
GO

/* Atomically claims a batch of messages for dispatch (safe when several
   Hangfire workers run at once) and releases rows stuck in Processing.  */
CREATE OR ALTER PROCEDURE dbo.sp_ClaimPendingMessages
    @BatchSize INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    -- recover messages left in Processing (e.g. app stopped mid-dispatch)
    UPDATE dbo.MessageQueue
    SET    Status = N'Retrying'
    WHERE  Status = N'Processing'
      AND  LastAttemptAt < DATEADD(MINUTE, -10, SYSDATETIME());

    ;WITH batch AS
    (
        SELECT TOP (@BatchSize) *
        FROM   dbo.MessageQueue WITH (ROWLOCK, UPDLOCK, READPAST)
        WHERE  Status IN (N'Pending', N'Retrying')
        ORDER  BY CreatedAt, MessageId
    )
    UPDATE batch
    SET    Status = N'Processing',
           LastAttemptAt = SYSDATETIME()
    OUTPUT inserted.MessageId, inserted.TriggerId, inserted.TemplateId, inserted.ReferenceKey,
           inserted.RecipientName, inserted.RecipientContact, inserted.Channel, inserted.Subject,
           inserted.MessageBody, inserted.Status, inserted.RetryCount, inserted.CreatedBy,
           inserted.CreatedAt, inserted.LastAttemptAt;
END
GO

/* Writes the delivery log row and moves the message to its next state:
   Sent  |  Retrying (attempts left)  |  Failed (no attempts left / permanent). */
CREATE OR ALTER PROCEDURE dbo.sp_RecordDeliveryResult
    @MessageId        INT,
    @IsSuccess        BIT,
    @GatewayProvider  NVARCHAR(80),
    @GatewayResponse  NVARCHAR(500),
    @FailureReason    NVARCHAR(250) = NULL,
    @IsPermanent      BIT = 0,
    @MaxRetries       INT = 3
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @RetryCount INT, @AttemptNo INT, @NewStatus NVARCHAR(12);

    SELECT @RetryCount = RetryCount
    FROM   dbo.MessageQueue WITH (UPDLOCK)
    WHERE  MessageId = @MessageId;

    SET @AttemptNo = ISNULL(@RetryCount, 0) + 1;

    INSERT INTO dbo.DeliveryLogs (MessageId, AttemptNo, GatewayProvider, GatewayResponse, Status, FailureReason, AttemptedAt)
    VALUES (@MessageId, @AttemptNo, @GatewayProvider, @GatewayResponse,
            CASE WHEN @IsSuccess = 1 THEN N'Sent' ELSE N'Failed' END,
            CASE WHEN @IsSuccess = 1 THEN NULL ELSE @FailureReason END,
            SYSDATETIME());

    IF @IsSuccess = 1
        SET @NewStatus = N'Sent';
    ELSE IF @IsPermanent = 1 OR @AttemptNo >= @MaxRetries
        SET @NewStatus = N'Failed';
    ELSE
        SET @NewStatus = N'Retrying';

    UPDATE dbo.MessageQueue
    SET    Status        = @NewStatus,
           RetryCount    = CASE WHEN @IsSuccess = 1 THEN RetryCount ELSE RetryCount + 1 END,
           LastAttemptAt = SYSDATETIME()
    WHERE  MessageId = @MessageId;

    COMMIT TRANSACTION;

    SELECT @NewStatus AS NewStatus;
END
GO

/* Sent / failed delivery attempts per day for the dashboard chart */
CREATE OR ALTER PROCEDURE dbo.sp_GetDailyDeliveryStats
    @Days INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH days AS
    (
        SELECT CAST(SYSDATETIME() AS DATE) AS [Day], 1 AS n
        UNION ALL
        SELECT DATEADD(DAY, -1, [Day]), n + 1 FROM days WHERE n < @Days
    )
    SELECT  d.[Day],
            SUM(CASE WHEN dl.Status = N'Sent'   THEN 1 ELSE 0 END) AS SentCount,
            SUM(CASE WHEN dl.Status = N'Failed' THEN 1 ELSE 0 END) AS FailedCount
    FROM    days d
            LEFT JOIN dbo.DeliveryLogs dl ON CAST(dl.AttemptedAt AS DATE) = d.[Day]
    GROUP BY d.[Day]
    ORDER BY d.[Day]
    OPTION (MAXRECURSION 400);
END
GO

/* Records a salary payment once per employee per month.
   Returns the new PaymentId, or -1 when that month was already paid.  */
CREATE OR ALTER PROCEDURE dbo.sp_DisburseSalary
    @EmployeeId  INT,
    @SalaryMonth CHAR(7),
    @PaidBy      INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.SalaryPayments WHERE EmployeeId = @EmployeeId AND SalaryMonth = @SalaryMonth)
    BEGIN
        SELECT CAST(-1 AS INT) AS PaymentId;
        RETURN;
    END

    INSERT INTO dbo.SalaryPayments (EmployeeId, SalaryMonth, Amount, PaidAt, PaidBy)
    SELECT EmployeeId, @SalaryMonth, Salary, SYSDATETIME(), @PaidBy
    FROM   dbo.Employees
    WHERE  EmployeeId = @EmployeeId AND IsActive = 1;

    IF @@ROWCOUNT = 0
        SELECT CAST(0 AS INT) AS PaymentId;
    ELSE
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS PaymentId;
END
GO

/* Inserts or updates one attendance mark (used by the daily attendance sheet) */
CREATE OR ALTER PROCEDURE dbo.sp_UpsertAttendance
    @EmployeeId     INT,
    @AttendanceDate DATE,
    @Status         NVARCHAR(10),
    @Remarks        NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.Attendance WITH (HOLDLOCK) AS target
    USING (SELECT @EmployeeId AS EmployeeId, @AttendanceDate AS AttendanceDate) AS src
          ON target.EmployeeId = src.EmployeeId AND target.AttendanceDate = src.AttendanceDate
    WHEN MATCHED THEN
        UPDATE SET Status = @Status, Remarks = @Remarks
    WHEN NOT MATCHED THEN
        INSERT (EmployeeId, AttendanceDate, Status, Remarks)
        VALUES (@EmployeeId, @AttendanceDate, @Status, @Remarks);
END
GO

PRINT 'Stored procedures created.';
GO


/* ---------------------------- 05_SeedData.sql ---------------------------- */
/* =====================================================================
   Script 05 : Demo seed data
   Every block only runs when its table is empty, so re-running is safe.

   Demo logins (password for all of them:  Admin@123)
     admin@notifyhub.local       Administrator
     accounts@notifyhub.local    Accountant
     hr@notifyhub.local          HR
     manager@notifyhub.local     Manager
   ===================================================================== */
USE SmsNotificationDB;
GO

SET NOCOUNT ON;

/* ---------- Users (BCrypt hash of "Admin@123") ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    DECLARE @hash NVARCHAR(200) = N'$2a$11$a1Mc2hYNAC0ccs7AyXUOcuVxE66ViPoyFTMfAnTyFjIO.FOEP84la';
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role) VALUES
        (N'System Administrator', N'admin@notifyhub.local',    @hash, N'Administrator'),
        (N'Rafiq Ahmed',          N'accounts@notifyhub.local', @hash, N'Accountant'),
        (N'Nusrat Jahan',         N'hr@notifyhub.local',       @hash, N'HR'),
        (N'Tanvir Hasan',         N'manager@notifyhub.local',  @hash, N'Manager');
    PRINT 'Users seeded.';
END
GO

/* ---------- Template categories ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.TemplateCategories)
BEGIN
    INSERT INTO dbo.TemplateCategories (Name, Description) VALUES
        (N'Invoice',    N'Payment reminders and billing messages'),
        (N'Stock',      N'Inventory and reorder alerts'),
        (N'Attendance', N'Absence and late arrival alerts'),
        (N'Salary',     N'Payroll and salary disbursement notices'),
        (N'General',    N'Announcements and one-off messages');
    PRINT 'Categories seeded.';
END
GO

/* ---------- Message templates ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.MessageTemplates)
BEGIN
    DECLARE @admin INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Role = N'Administrator' ORDER BY UserId);

    INSERT INTO dbo.MessageTemplates (Name, CategoryId, Channel, Subject, Body, Status, CreatedBy) VALUES
    (N'Invoice due reminder (SMS)',
        (SELECT CategoryId FROM dbo.TemplateCategories WHERE Name = N'Invoice'), N'SMS', NULL,
        N'Dear {CustomerName}, your outstanding invoice {InvoiceNo} amount is Tk {DueAmount}, due on {DueDate}. Please pay to avoid late fees. - {CompanyName}',
        N'Active', @admin),
    (N'Low stock alert (Email)',
        (SELECT CategoryId FROM dbo.TemplateCategories WHERE Name = N'Stock'), N'Email', N'Low stock: {ProductName}',
        N'Stock for {ProductName} ({Sku}) has dropped to {StockQty} units, at or below the reorder level of {ReorderLevel}. Please raise a purchase order.',
        N'Active', @admin),
    (N'Absence alert (SMS)',
        (SELECT CategoryId FROM dbo.TemplateCategories WHERE Name = N'Attendance'), N'SMS', NULL,
        N'Dear {EmployeeName}, you were marked {Status} on {Date}. Please contact HR if this is incorrect.',
        N'Active', @admin),
    (N'Salary disbursed (SMS)',
        (SELECT CategoryId FROM dbo.TemplateCategories WHERE Name = N'Salary'), N'SMS', NULL,
        N'Dear {EmployeeName}, your salary of Tk {Amount} for {Month} has been disbursed. - {CompanyName}',
        N'Active', @admin),
    (N'Invoice due reminder (Email)',
        (SELECT CategoryId FROM dbo.TemplateCategories WHERE Name = N'Invoice'), N'Email', N'Payment reminder for invoice {InvoiceNo}',
        N'Dear {CustomerName}, this is a reminder that invoice {InvoiceNo} for Tk {DueAmount} is due in {DaysLeft} day(s), on {DueDate}. Thank you for your business. - {CompanyName}',
        N'Inactive', @admin),
    (N'Office announcement (SMS)',
        (SELECT CategoryId FROM dbo.TemplateCategories WHERE Name = N'General'), N'SMS', NULL,
        N'Dear {RecipientName}, the office will remain closed tomorrow. - {CompanyName}',
        N'Active', @admin);
    PRINT 'Templates seeded.';
END
GO

/* ---------- Event triggers ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.EventTriggers)
BEGIN
    INSERT INTO dbo.EventTriggers
        (Name, EventType, TemplateId, ConditionField, ConditionOperator, ConditionValue, RecipientName, RecipientContact, CooldownHours, IsActive)
    VALUES
    (N'Invoice due within 3 days', N'InvoiceDue',
        (SELECT TemplateId FROM dbo.MessageTemplates WHERE Name = N'Invoice due reminder (SMS)'),
        N'DaysUntilDue', N'<=', N'3', NULL, NULL, 24, 1),
    (N'Low stock to store manager', N'StockLow',
        (SELECT TemplateId FROM dbo.MessageTemplates WHERE Name = N'Low stock alert (Email)'),
        N'StockQty', N'<=', N'ReorderLevel', N'Store Manager', N'manager@notifyhub.local', 24, 1),
    (N'Absent employee alert', N'AttendanceAbsent',
        (SELECT TemplateId FROM dbo.MessageTemplates WHERE Name = N'Absence alert (SMS)'),
        N'Status', N'=', N'Absent', NULL, NULL, 24, 1),
    (N'Salary disbursed confirmation', N'SalaryDisbursed',
        (SELECT TemplateId FROM dbo.MessageTemplates WHERE Name = N'Salary disbursed (SMS)'),
        N'Amount', N'>', N'0', NULL, NULL, 24, 1);
    PRINT 'Triggers seeded.';
END
GO

/* ---------- Gateways (simulated during development) ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.GatewaySettings)
BEGIN
    INSERT INTO dbo.GatewaySettings (Provider, Channel, ApiKey, SenderId, IsPrimary, IsActive) VALUES
        (N'Alpha SMS (simulated)', N'SMS',   N'sandbox-alpha-7f3c91d2', N'TROYEE',               1, 1),
        (N'SMTP (simulated)',      N'Email', N'sandbox-smtp-b21e44a0',  N'noreply@notifyhub.local', 1, 1),
        (N'Twilio (simulated)',    N'SMS',   N'sandbox-twilio-0c9a55e1', N'+15005550006',         0, 0);
    PRINT 'Gateways seeded.';
END
GO

/* ---------- Invoices (dates relative to today) ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Invoices)
BEGIN
    DECLARE @today DATE = CAST(SYSDATETIME() AS DATE);
    INSERT INTO dbo.Invoices (InvoiceNo, CustomerName, CustomerPhone, CustomerEmail, Amount, DueDate, IsPaid, PaidAt) VALUES
        (N'INV-2026-0001', N'Rahim Traders',        N'+8801711000101', N'rahim@example.com',  45000.00, DATEADD(DAY,  2, @today), 0, NULL),
        (N'INV-2026-0002', N'Karim Enterprise',     N'+8801811000102', N'karim@example.com',  12500.50, DATEADD(DAY,  1, @today), 0, NULL),
        (N'INV-2026-0003', N'Dhaka Fabrics Ltd.',   N'+8801911000103', N'accounts@dhakafabrics.example', 98000.00, DATEADD(DAY, 12, @today), 0, NULL),
        (N'INV-2026-0004', N'Green Leaf Agro',      N'+8801511000104', NULL,                  7600.00,  DATEADD(DAY, -2, @today), 0, NULL),
        (N'INV-2026-0005', N'Meghna Electronics',   N'+8801611000105', N'meghna@example.com', 23000.00, DATEADD(DAY, -8, @today), 1, DATEADD(DAY, -9, SYSDATETIME()));
    PRINT 'Invoices seeded.';
END
GO

/* ---------- Products (two already at/below reorder level) ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products (ProductName, Sku, StockQty, ReorderLevel, UnitPrice) VALUES
        (N'A4 Printer Paper (Ream)', N'STN-A4-500', 8,   20, 450.00),
        (N'Toner Cartridge 85A',     N'PRN-85A',    3,    5, 3200.00),
        (N'Wireless Mouse',          N'ACC-WM-01',  128, 25, 850.00),
        (N'Office Chair',            N'FUR-OC-02',  22,  10, 8900.00),
        (N'USB-C Cable 1m',          N'ACC-USBC-1', 45,  30, 350.00),
        (N'Whiteboard Marker Box',   N'STN-WBM-12', 60,  15, 280.00);
    PRINT 'Products seeded.';
END
GO

/* ---------- Employees ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Employees)
BEGIN
    INSERT INTO dbo.Employees (FullName, Phone, Email, Department, Designation, Salary, JoinedAt) VALUES
        (N'Arif Hossain',    N'+8801712000201', N'arif@example.com',    N'Software',   N'Software Engineer',  65000, '2024-02-01'),
        (N'Sadia Islam',     N'+8801812000202', N'sadia@example.com',   N'Accounts',   N'Accounts Officer',   48000, '2023-07-15'),
        (N'Mehedi Rahman',   N'+8801912000203', N'mehedi@example.com',  N'Support',    N'Support Executive',  32000, '2025-01-10'),
        (N'Farzana Akter',   N'+8801512000204', N'farzana@example.com', N'HR',         N'HR Executive',       42000, '2022-11-01'),
        (N'Imran Kabir',     N'+8801612000205', NULL,                   N'Sales',      N'Sales Executive',    35000, '2025-06-01'),
        (N'Nabila Chowdhury',N'+8801312000206', N'nabila@example.com',  N'Software',   N'QA Engineer',        55000, '2024-09-01');
    PRINT 'Employees seeded.';
END
GO

/* ---------- Attendance: yesterday all present, today one absent + one late ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Attendance)
BEGIN
    DECLARE @d0 DATE = CAST(SYSDATETIME() AS DATE);
    DECLARE @d1 DATE = DATEADD(DAY, -1, @d0);

    INSERT INTO dbo.Attendance (EmployeeId, AttendanceDate, Status)
    SELECT EmployeeId, @d1, N'Present' FROM dbo.Employees;

    INSERT INTO dbo.Attendance (EmployeeId, AttendanceDate, Status, Remarks)
    SELECT EmployeeId, @d0,
           CASE FullName WHEN N'Mehedi Rahman' THEN N'Absent'
                         WHEN N'Imran Kabir'   THEN N'Late'
                         ELSE N'Present' END,
           CASE FullName WHEN N'Imran Kabir' THEN N'Arrived 10:40' ELSE NULL END
    FROM dbo.Employees;
    PRINT 'Attendance seeded.';
END
GO

/* ---------- Last month's salary already paid ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.SalaryPayments)
BEGIN
    DECLARE @lastMonth CHAR(7) = CONVERT(CHAR(7), DATEADD(MONTH, -1, SYSDATETIME()), 126);
    INSERT INTO dbo.SalaryPayments (EmployeeId, SalaryMonth, Amount, PaidAt, PaidBy)
    SELECT EmployeeId, @lastMonth, Salary, DATEADD(DAY, -25, SYSDATETIME()),
           (SELECT TOP 1 UserId FROM dbo.Users WHERE Role = N'Accountant')
    FROM dbo.Employees;
    PRINT 'Salary history seeded.';
END
GO

/* ---------- Delivery history for the last 7 days (dashboard chart) ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.MessageQueue)
BEGIN
    DECLARE @i INT = 1, @msgId INT, @created DATETIME2(0), @ch NVARCHAR(10), @name NVARCHAR(120),
            @contact NVARCHAR(150), @trg INT, @body NVARCHAR(1000), @kind INT;

    WHILE @i <= 64
    BEGIN
        SET @created = DATEADD(MINUTE, -((@i % 7) * 1440 + (@i * 37) % 600 + 30), SYSDATETIME());
        SET @ch      = CASE WHEN @i % 3 = 0 THEN N'Email' ELSE N'SMS' END;
        SET @name    = CHOOSE(@i % 5 + 1, N'Rahim Traders', N'Karim Enterprise', N'Arif Hossain', N'Sadia Islam', N'Store Manager');
        SET @contact = CASE WHEN @ch = N'Email'
                            THEN CHOOSE(@i % 5 + 1, N'rahim@example.com', N'karim@example.com', N'arif@example.com', N'sadia@example.com', N'manager@notifyhub.local')
                            ELSE CHOOSE(@i % 5 + 1, N'+8801711000101', N'+8801811000102', N'+8801712000201', N'+8801812000202', N'+8801700000999') END;
        SET @trg     = (SELECT TriggerId FROM dbo.EventTriggers ORDER BY TriggerId OFFSET (@i % 4) ROWS FETCH NEXT 1 ROWS ONLY);
        SET @body    = CHOOSE(@i % 4 + 1,
                        N'Dear ' + @name + N', your outstanding invoice amount is due soon. Please pay to avoid late fees.',
                        N'Stock for Toner Cartridge 85A has dropped below the reorder level.',
                        N'Dear ' + @name + N', you were marked Absent. Please contact HR if this is incorrect.',
                        N'Dear ' + @name + N', your salary for last month has been disbursed.');
        SET @kind    = CASE WHEN @i % 11 = 0 THEN 2 WHEN @i % 6 = 0 THEN 1 ELSE 0 END;  -- 0 sent, 1 sent after retry, 2 failed

        INSERT INTO dbo.MessageQueue (TriggerId, RecipientName, RecipientContact, Channel, Subject, MessageBody,
                                      Status, RetryCount, CreatedAt, LastAttemptAt)
        VALUES (@trg, @name, @contact, @ch, CASE WHEN @ch = N'Email' THEN N'Notification from NotifyHub' END, @body,
                CASE @kind WHEN 2 THEN N'Failed' ELSE N'Sent' END,
                CASE @kind WHEN 0 THEN 0 WHEN 1 THEN 1 ELSE 3 END,
                @created, DATEADD(MINUTE, 3, @created));
        SET @msgId = SCOPE_IDENTITY();

        IF @kind = 0
            INSERT INTO dbo.DeliveryLogs (MessageId, AttemptNo, GatewayProvider, GatewayResponse, Status, AttemptedAt)
            VALUES (@msgId, 1, CASE WHEN @ch = N'SMS' THEN N'Alpha SMS (simulated)' ELSE N'SMTP (simulated)' END,
                    N'ACCEPTED ref=SIM' + RIGHT(N'000000' + CAST(@msgId * 7919 AS NVARCHAR(10)), 6), N'Sent', DATEADD(MINUTE, 1, @created));
        ELSE IF @kind = 1
        BEGIN
            INSERT INTO dbo.DeliveryLogs (MessageId, AttemptNo, GatewayProvider, GatewayResponse, Status, FailureReason, AttemptedAt)
            VALUES (@msgId, 1, CASE WHEN @ch = N'SMS' THEN N'Alpha SMS (simulated)' ELSE N'SMTP (simulated)' END,
                    N'ERROR 504', N'Failed', N'Gateway timeout', DATEADD(MINUTE, 1, @created));
            INSERT INTO dbo.DeliveryLogs (MessageId, AttemptNo, GatewayProvider, GatewayResponse, Status, AttemptedAt)
            VALUES (@msgId, 2, CASE WHEN @ch = N'SMS' THEN N'Alpha SMS (simulated)' ELSE N'SMTP (simulated)' END,
                    N'ACCEPTED ref=SIM' + RIGHT(N'000000' + CAST(@msgId * 7919 AS NVARCHAR(10)), 6), N'Sent', DATEADD(MINUTE, 3, @created));
        END
        ELSE
        BEGIN
            INSERT INTO dbo.DeliveryLogs (MessageId, AttemptNo, GatewayProvider, GatewayResponse, Status, FailureReason, AttemptedAt)
            VALUES (@msgId, 1, N'Alpha SMS (simulated)', N'ERROR 503', N'Failed', N'Network unreachable', DATEADD(MINUTE, 1, @created)),
                   (@msgId, 2, N'Alpha SMS (simulated)', N'ERROR 429', N'Failed', N'Rate limit exceeded',  DATEADD(MINUTE, 2, @created)),
                   (@msgId, 3, N'Alpha SMS (simulated)', N'ERROR 504', N'Failed', N'Gateway timeout',      DATEADD(MINUTE, 3, @created));
        END

        SET @i += 1;
    END
    PRINT 'Delivery history seeded.';
END
GO

PRINT 'Seed data complete. Log in with admin@notifyhub.local / Admin@123';
GO

SET NOEXEC OFF;
GO

