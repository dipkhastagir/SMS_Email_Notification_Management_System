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
