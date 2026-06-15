-- Run on database rag_edu before using subscription + MoMo payment features.
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.subscription_plans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.subscription_plans (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        description NVARCHAR(MAX) NULL,
        price DECIMAL(18,2) NOT NULL,
        duration_days INT NOT NULL,
        is_active BIT NOT NULL CONSTRAINT DF_subscription_plans_active DEFAULT (1),
        created_at DATETIME2 NOT NULL CONSTRAINT DF_subscription_plans_created DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.payment_tickets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.payment_tickets (
        id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        plan_id INT NOT NULL,
        amount DECIMAL(18,2) NOT NULL,
        transfer_reference NVARCHAR(500) NULL,
        payment_method NVARCHAR(20) NULL,
        momo_order_id NVARCHAR(100) NULL,
        momo_request_id NVARCHAR(100) NULL,
        momo_trans_id NVARCHAR(100) NULL,
        momo_pay_url NVARCHAR(1000) NULL,
        momo_response_json NVARCHAR(MAX) NULL,
        momo_ipn_json NVARCHAR(MAX) NULL,
        momo_result_code INT NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_payment_tickets_status DEFAULT ('pending'),
        admin_note NVARCHAR(MAX) NULL,
        reviewed_by INT NULL,
        reviewed_at DATETIME2 NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_payment_tickets_created DEFAULT (GETDATE()),
        CONSTRAINT FK_payment_tickets_user FOREIGN KEY (user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_payment_tickets_plan FOREIGN KEY (plan_id) REFERENCES dbo.subscription_plans(id),
        CONSTRAINT FK_payment_tickets_reviewer FOREIGN KEY (reviewed_by) REFERENCES dbo.users(id)
    );
END
GO

IF OBJECT_ID(N'dbo.payment_tickets', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.payment_tickets', 'payment_method') IS NULL
        ALTER TABLE dbo.payment_tickets ADD payment_method NVARCHAR(20) NULL;

    IF COL_LENGTH('dbo.payment_tickets', 'momo_order_id') IS NULL
        ALTER TABLE dbo.payment_tickets ADD momo_order_id NVARCHAR(100) NULL;

    IF COL_LENGTH('dbo.payment_tickets', 'momo_request_id') IS NULL
        ALTER TABLE dbo.payment_tickets ADD momo_request_id NVARCHAR(100) NULL;

    IF COL_LENGTH('dbo.payment_tickets', 'momo_trans_id') IS NULL
        ALTER TABLE dbo.payment_tickets ADD momo_trans_id NVARCHAR(100) NULL;

    IF COL_LENGTH('dbo.payment_tickets', 'momo_pay_url') IS NULL
        ALTER TABLE dbo.payment_tickets ADD momo_pay_url NVARCHAR(1000) NULL;

    IF COL_LENGTH('dbo.payment_tickets', 'momo_response_json') IS NULL
        ALTER TABLE dbo.payment_tickets ADD momo_response_json NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.payment_tickets', 'momo_ipn_json') IS NULL
        ALTER TABLE dbo.payment_tickets ADD momo_ipn_json NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.payment_tickets', 'momo_result_code') IS NULL
        ALTER TABLE dbo.payment_tickets ADD momo_result_code INT NULL;

    IF COL_LENGTH('dbo.payment_tickets', 'transfer_reference') IS NOT NULL
        ALTER TABLE dbo.payment_tickets ALTER COLUMN transfer_reference NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'idx_payment_tickets_user'
      AND object_id = OBJECT_ID(N'dbo.payment_tickets')
)
BEGIN
    CREATE INDEX idx_payment_tickets_user ON dbo.payment_tickets(user_id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'idx_payment_tickets_status'
      AND object_id = OBJECT_ID(N'dbo.payment_tickets')
)
BEGIN
    CREATE INDEX idx_payment_tickets_status ON dbo.payment_tickets(status);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UQ_payment_tickets_momo_order_id'
      AND object_id = OBJECT_ID(N'dbo.payment_tickets')
)
BEGIN
    CREATE UNIQUE INDEX UQ_payment_tickets_momo_order_id
        ON dbo.payment_tickets(momo_order_id)
        WHERE momo_order_id IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'idx_payment_tickets_momo_request_id'
      AND object_id = OBJECT_ID(N'dbo.payment_tickets')
)
BEGIN
    CREATE INDEX idx_payment_tickets_momo_request_id
        ON dbo.payment_tickets(momo_request_id)
        WHERE momo_request_id IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.user_subscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.user_subscriptions (
        id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        plan_id INT NOT NULL,
        start_at DATETIME2 NOT NULL,
        end_at DATETIME2 NOT NULL,
        is_active BIT NOT NULL CONSTRAINT DF_user_subscriptions_active DEFAULT (1),
        payment_ticket_id INT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_user_subscriptions_created DEFAULT (GETDATE()),
        CONSTRAINT FK_user_subscriptions_user FOREIGN KEY (user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_user_subscriptions_plan FOREIGN KEY (plan_id) REFERENCES dbo.subscription_plans(id),
        CONSTRAINT FK_user_subscriptions_ticket FOREIGN KEY (payment_ticket_id) REFERENCES dbo.payment_tickets(id),
        CONSTRAINT UQ_user_subscriptions_ticket UNIQUE (payment_ticket_id)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'idx_user_subscriptions_user'
      AND object_id = OBJECT_ID(N'dbo.user_subscriptions')
)
BEGIN
    CREATE INDEX idx_user_subscriptions_user ON dbo.user_subscriptions(user_id);
END
GO

IF OBJECT_ID(N'dbo.student_chat_usages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.student_chat_usages (
        id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        subject_id INT NOT NULL,
        window_start DATETIME2 NOT NULL,
        question_count INT NOT NULL CONSTRAINT DF_student_chat_usages_count DEFAULT (0),
        created_at DATETIME2 NOT NULL CONSTRAINT DF_student_chat_usages_created DEFAULT (GETDATE()),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_student_chat_usages_updated DEFAULT (GETDATE()),
        CONSTRAINT FK_student_chat_usages_user FOREIGN KEY (user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_student_chat_usages_subject FOREIGN KEY (subject_id) REFERENCES dbo.subjects(id)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'idx_student_chat_usages_user'
      AND object_id = OBJECT_ID(N'dbo.student_chat_usages')
)
BEGIN
    CREATE INDEX idx_student_chat_usages_user ON dbo.student_chat_usages(user_id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'idx_student_chat_usages_subject'
      AND object_id = OBJECT_ID(N'dbo.student_chat_usages')
)
BEGIN
    CREATE INDEX idx_student_chat_usages_subject ON dbo.student_chat_usages(subject_id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UQ_student_chat_usages_window'
      AND object_id = OBJECT_ID(N'dbo.student_chat_usages')
)
BEGIN
    CREATE UNIQUE INDEX UQ_student_chat_usages_window
        ON dbo.student_chat_usages(user_id, subject_id, window_start);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.subscription_plans)
BEGIN
    INSERT INTO dbo.subscription_plans (name, description, price, duration_days, is_active)
    VALUES
        (N'Plus 30 ngay', N'Kich hoat Plus trong 30 ngay', 49000, 30, 1),
        (N'Plus 90 ngay', N'Kich hoat Plus trong 90 ngay', 129000, 90, 1),
        (N'Plus 365 ngay', N'Kich hoat Plus trong 365 ngay', 399000, 365, 1);
END
GO
