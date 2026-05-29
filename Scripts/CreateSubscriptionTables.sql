-- Run on database rag_edu before using subscription features.

IF OBJECT_ID(N'dbo.subscription_plans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.subscription_plans (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        description NVARCHAR(MAX) NULL,
        price DECIMAL(18, 2) NOT NULL,
        duration_days INT NOT NULL,
        is_active BIT NOT NULL CONSTRAINT DF_subscription_plans_active DEFAULT (1),
        created_at DATETIME2 NOT NULL CONSTRAINT DF_subscription_plans_created DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.payment_tickets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.payment_tickets (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id INT NOT NULL,
        plan_id INT NOT NULL,
        amount DECIMAL(18, 2) NOT NULL,
        transfer_reference NVARCHAR(500) NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_payment_tickets_status DEFAULT ('pending'),
        admin_note NVARCHAR(MAX) NULL,
        reviewed_by INT NULL,
        reviewed_at DATETIME2 NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_payment_tickets_created DEFAULT (GETDATE()),
        CONSTRAINT FK_payment_tickets_user FOREIGN KEY (user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_payment_tickets_plan FOREIGN KEY (plan_id) REFERENCES dbo.subscription_plans(id),
        CONSTRAINT FK_payment_tickets_reviewer FOREIGN KEY (reviewed_by) REFERENCES dbo.users(id)
    );

    CREATE INDEX idx_payment_tickets_user ON dbo.payment_tickets(user_id);
    CREATE INDEX idx_payment_tickets_status ON dbo.payment_tickets(status);
END
GO

IF OBJECT_ID(N'dbo.user_subscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.user_subscriptions (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
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

    CREATE INDEX idx_user_subscriptions_user ON dbo.user_subscriptions(user_id);
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.payment_tickets')
      AND name = N'transfer_reference'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.payment_tickets ALTER COLUMN transfer_reference NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.subscription_plans)
BEGIN
    INSERT INTO dbo.subscription_plans (name, description, price, duration_days, is_active)
    VALUES
        (N'Gói tháng', N'Truy cập Chat RAG trong 30 ngày', 99000, 30, 1),
        (N'Gói quý', N'Truy cập Chat RAG trong 90 ngày', 249000, 90, 1);
END
GO
