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
