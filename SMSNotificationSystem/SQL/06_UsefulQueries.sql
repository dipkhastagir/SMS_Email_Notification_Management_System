/* =====================================================================
   Script 06 : Handy queries for checking the system from SSMS
   (not required to run the application - select a block and press F5)
   ===================================================================== */
USE SmsNotificationDB;
GO

-- 1. Dashboard numbers (same view the web dashboard uses)
SELECT * FROM dbo.vw_DashboardStats;

-- 2. Queue grouped by status
SELECT Status, Channel, COUNT(*) AS Messages
FROM dbo.MessageQueue
GROUP BY Status, Channel
ORDER BY Status, Channel;

-- 3. Latest 20 delivery attempts with recipient and trigger
SELECT TOP (20) * FROM dbo.vw_DeliveryReport ORDER BY AttemptedAt DESC;

-- 4. Delivery success rate per channel
SELECT q.Channel,
       COUNT(*)                                                    AS Attempts,
       SUM(CASE WHEN dl.Status = N'Sent' THEN 1 ELSE 0 END)        AS Sent,
       CAST(100.0 * SUM(CASE WHEN dl.Status = N'Sent' THEN 1 ELSE 0 END) / NULLIF(COUNT(*), 0) AS DECIMAL(5,1)) AS SuccessRatePct
FROM dbo.DeliveryLogs dl
JOIN dbo.MessageQueue q ON q.MessageId = dl.MessageId
GROUP BY q.Channel;

-- 5. Most common failure reasons
SELECT FailureReason, COUNT(*) AS Occurrences
FROM dbo.DeliveryLogs
WHERE Status = N'Failed'
GROUP BY FailureReason
ORDER BY Occurrences DESC;

-- 6. Messages produced by each trigger
SELECT Name, EventType, IsActive, MessagesQueued, LastRunAt FROM dbo.vw_TriggerDetails ORDER BY MessagesQueued DESC;

-- 7. Records that currently match the seeded triggers
SELECT InvoiceNo, CustomerName, Amount, DueDate, DATEDIFF(DAY, CAST(SYSDATETIME() AS DATE), DueDate) AS DaysUntilDue
FROM dbo.Invoices WHERE IsPaid = 0 AND DATEDIFF(DAY, CAST(SYSDATETIME() AS DATE), DueDate) <= 3;

SELECT ProductName, StockQty, ReorderLevel FROM dbo.Products WHERE StockQty <= ReorderLevel;

SELECT EmployeeName, Department, Status FROM dbo.vw_AttendanceDetails
WHERE AttendanceDate = CAST(SYSDATETIME() AS DATE) AND Status = N'Absent';

-- 8. Daily chart data used by the dashboard
EXEC dbo.sp_GetDailyDeliveryStats @Days = 7;

-- 9. Put every failed message back into the queue (the web "Retry" button does this for one message)
-- UPDATE dbo.MessageQueue SET Status = N'Pending', RetryCount = 0 WHERE Status = N'Failed';

-- 10. Hangfire job history (tables are created by the app on first run)
-- SELECT TOP (20) Id, StateName, CreatedAt FROM HangFire.Job ORDER BY Id DESC;
GO
