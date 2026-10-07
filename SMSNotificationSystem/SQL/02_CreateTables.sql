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
