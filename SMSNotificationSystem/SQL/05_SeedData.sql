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
