using System.Globalization;
using System.Text;
using Assignment1_Repository.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Data.SqlClient;

namespace Assignment1_Service.Infrastructure;

public static class DatabaseSchemaSynchronizer
{
    public static async Task UpdateAsync(RagEduContext context)
    {
        var entityTypes = context.Model.GetEntityTypes()
            .Where(entity => !entity.IsOwned() && entity.GetTableName() is not null)
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName()!;
            var schema = entityType.GetSchema() ?? "dbo";
            if (!await TableExistsAsync(context, schema, tableName))
            {
                await CreateTableAsync(context, entityType, schema, tableName);
                continue;
            }

            await SyncColumnsAsync(context, entityType, schema, tableName);
        }

        await NormalizeSoftDeleteColumnsAsync(context);
        await EnsureLearningSchemaConstraintsAsync(context);
    }

    private static async Task CreateTableAsync(
        RagEduContext context,
        IEntityType entityType,
        string schema,
        string tableName)
    {
        var storeObject = StoreObjectIdentifier.Table(tableName, schema);
        var sql = new StringBuilder();
        sql.AppendLine($"IF OBJECT_ID(N'[{schema}].[{tableName}]', N'U') IS NULL");
        sql.AppendLine("BEGIN");
        sql.AppendLine($"    CREATE TABLE [{schema}].[{tableName}] (");

        var propertyDefinitions = new List<string>();
        var primaryKey = entityType.FindPrimaryKey();
        foreach (var property in entityType.GetProperties().OrderBy(p => p.Name))
        {
            var columnName = ResolveColumnName(property, storeObject);
            var isPrimaryKey = primaryKey?.Properties.Contains(property) == true;
            propertyDefinitions.Add($"        {BuildColumnDefinition(property, columnName, storeObject, isPrimaryKey, true)}");
        }

        if (primaryKey is not null && primaryKey.Properties.Count > 0)
        {
            var keyColumns = string.Join(", ", primaryKey.Properties.Select(p => $"[{ResolveColumnName(p, storeObject)}]"));
            propertyDefinitions.Add($"        CONSTRAINT [{GetPrimaryKeyName(entityType)}] PRIMARY KEY ({keyColumns})");
        }

        sql.AppendLine(string.Join("," + Environment.NewLine, propertyDefinitions));
        sql.AppendLine("    );");
        sql.AppendLine("END");

        await context.Database.ExecuteSqlRawAsync(sql.ToString());
        await SyncIndexesAsync(context, entityType, schema, tableName, storeObject);
    }

    private static async Task SyncColumnsAsync(
        RagEduContext context,
        IEntityType entityType,
        string schema,
        string tableName)
    {
        var storeObject = StoreObjectIdentifier.Table(tableName, schema);
        foreach (var property in entityType.GetProperties())
        {
            var columnName = ResolveColumnName(property, storeObject);
            if (await ColumnExistsAsync(context, schema, tableName, columnName))
                continue;

            var isPrimaryKey = entityType.FindPrimaryKey()?.Properties.Contains(property) == true;
            var definition = BuildColumnDefinition(property, columnName, storeObject, isPrimaryKey, false);
            var sql = $"ALTER TABLE [{schema}].[{tableName}] ADD {definition};";
            await context.Database.ExecuteSqlRawAsync(sql);
        }

        await SyncIndexesAsync(context, entityType, schema, tableName, storeObject);
        await SyncDefaultConstraintsAsync(context, entityType, schema, tableName, storeObject);
    }

    private static async Task SyncDefaultConstraintsAsync(
        RagEduContext context,
        IEntityType entityType,
        string schema,
        string tableName,
        StoreObjectIdentifier storeObject)
    {
        foreach (var property in entityType.GetProperties())
        {
            var defaultExpression = ResolveDefaultExpression(property);
            if (string.IsNullOrWhiteSpace(defaultExpression))
                continue;

            var columnName = ResolveColumnName(property, storeObject);
            if (await DefaultConstraintExistsAsync(context, schema, tableName, columnName))
                continue;

            var constraintName = GetDefaultConstraintName(storeObject, columnName);
            var sql =
                $"ALTER TABLE [{schema}].[{tableName}] ADD CONSTRAINT [{constraintName}] " +
                $"DEFAULT ({defaultExpression}) FOR [{columnName}];";
            await context.Database.ExecuteSqlRawAsync(sql);
        }
    }

    private static async Task SyncIndexesAsync(
        RagEduContext context,
        IEntityType entityType,
        string schema,
        string tableName,
        StoreObjectIdentifier storeObject)
    {
        foreach (var index in entityType.GetIndexes())
        {
            var indexName = index.GetDatabaseName();
            if (string.IsNullOrWhiteSpace(indexName))
                continue;

            if (await IndexExistsAsync(context, schema, tableName, indexName))
                continue;

            var columns = string.Join(", ", index.Properties.Select(p => $"[{ResolveColumnName(p, storeObject)}]"));
            var unique = index.IsUnique ? "UNIQUE " : string.Empty;
            var filter = index.GetFilter();
            var sql = new StringBuilder();
            sql.AppendLine($"CREATE {unique}INDEX [{indexName}] ON [{schema}].[{tableName}] ({columns})");
            if (!string.IsNullOrWhiteSpace(filter))
            {
                sql.AppendLine($"WHERE {filter}");
            }
            sql.AppendLine(";");

            await context.Database.ExecuteSqlRawAsync(sql.ToString());
        }
    }

    private static async Task NormalizeSoftDeleteColumnsAsync(RagEduContext context)
    {
        await NormalizeSoftDeleteColumnAsync(context, "dbo", "documents");
        await NormalizeSoftDeleteColumnAsync(context, "dbo", "subjects");
        await NormalizeSoftDeleteColumnAsync(context, "dbo", "learning_sets");
    }

    private static Task EnsureLearningSchemaConstraintsAsync(RagEduContext context)
    {
        const string sql = """
            IF OBJECT_ID(N'[dbo].[question_bank_items]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_question_bank_subject')
                    ALTER TABLE [dbo].[question_bank_items] WITH CHECK ADD CONSTRAINT [FK_question_bank_subject]
                    FOREIGN KEY ([subject_id]) REFERENCES [dbo].[subjects] ([id]);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_question_bank_chapter')
                    ALTER TABLE [dbo].[question_bank_items] WITH CHECK ADD CONSTRAINT [FK_question_bank_chapter]
                    FOREIGN KEY ([chapter_id]) REFERENCES [dbo].[chapters] ([id]) ON DELETE SET NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_question_bank_creator')
                    ALTER TABLE [dbo].[question_bank_items] WITH CHECK ADD CONSTRAINT [FK_question_bank_creator]
                    FOREIGN KEY ([created_by_user_id]) REFERENCES [dbo].[users] ([id]);
            END;

            IF OBJECT_ID(N'[dbo].[learning_sets]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_learning_sets_subject')
                    ALTER TABLE [dbo].[learning_sets] WITH CHECK ADD CONSTRAINT [FK_learning_sets_subject]
                    FOREIGN KEY ([subject_id]) REFERENCES [dbo].[subjects] ([id]);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_learning_sets_creator')
                    ALTER TABLE [dbo].[learning_sets] WITH CHECK ADD CONSTRAINT [FK_learning_sets_creator]
                    FOREIGN KEY ([created_by_user_id]) REFERENCES [dbo].[users] ([id]);
            END;

            IF OBJECT_ID(N'[dbo].[learning_set_items]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_learning_set_items_set')
                    ALTER TABLE [dbo].[learning_set_items] WITH CHECK ADD CONSTRAINT [FK_learning_set_items_set]
                    FOREIGN KEY ([learning_set_id]) REFERENCES [dbo].[learning_sets] ([id]) ON DELETE CASCADE;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_learning_set_items_question')
                    ALTER TABLE [dbo].[learning_set_items] WITH CHECK ADD CONSTRAINT [FK_learning_set_items_question]
                    FOREIGN KEY ([question_bank_item_id]) REFERENCES [dbo].[question_bank_items] ([id]);
            END;

            IF OBJECT_ID(N'[dbo].[learning_attempts]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_learning_attempts_set')
                    ALTER TABLE [dbo].[learning_attempts] WITH CHECK ADD CONSTRAINT [FK_learning_attempts_set]
                    FOREIGN KEY ([learning_set_id]) REFERENCES [dbo].[learning_sets] ([id]) ON DELETE CASCADE;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_learning_attempts_user')
                    ALTER TABLE [dbo].[learning_attempts] WITH CHECK ADD CONSTRAINT [FK_learning_attempts_user]
                    FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([id]);
            END;

            IF OBJECT_ID(N'[dbo].[learning_attempt_answers]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_learning_answers_attempt')
                    ALTER TABLE [dbo].[learning_attempt_answers] WITH CHECK ADD CONSTRAINT [FK_learning_answers_attempt]
                    FOREIGN KEY ([learning_attempt_id]) REFERENCES [dbo].[learning_attempts] ([id]) ON DELETE CASCADE;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_learning_answers_question')
                    ALTER TABLE [dbo].[learning_attempt_answers] WITH CHECK ADD CONSTRAINT [FK_learning_answers_question]
                    FOREIGN KEY ([question_bank_item_id]) REFERENCES [dbo].[question_bank_items] ([id]);
            END;

            IF OBJECT_ID(N'[dbo].[learning_set_versions]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_learning_set_versions_set')
                    ALTER TABLE [dbo].[learning_set_versions] WITH CHECK ADD CONSTRAINT [FK_learning_set_versions_set]
                    FOREIGN KEY ([learning_set_id]) REFERENCES [dbo].[learning_sets] ([id]) ON DELETE CASCADE;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_learning_set_versions_creator')
                    ALTER TABLE [dbo].[learning_set_versions] WITH CHECK ADD CONSTRAINT [FK_learning_set_versions_creator]
                    FOREIGN KEY ([created_by_user_id]) REFERENCES [dbo].[users] ([id]);
            END;

            IF OBJECT_ID(N'[dbo].[audit_logs]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_audit_logs_user')
                    ALTER TABLE [dbo].[audit_logs] WITH CHECK ADD CONSTRAINT [FK_audit_logs_user]
                    FOREIGN KEY ([user_id]) REFERENCES [dbo].[users] ([id]) ON DELETE SET NULL;
            END;
            """;

        return context.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task NormalizeSoftDeleteColumnAsync(
        RagEduContext context,
        string schema,
        string tableName)
    {
        if (!await TableExistsAsync(context, schema, tableName)
            || !await ColumnExistsAsync(context, schema, tableName, "is_deleted"))
        {
            return;
        }

        var sql = "UPDATE [" + schema + "].[" + tableName + "] SET [is_deleted] = 0 WHERE [is_deleted] IS NULL;";
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    private static string BuildColumnDefinition(
        IProperty property,
        string columnName,
        StoreObjectIdentifier storeObject,
        bool allowIdentity,
        bool isNewTable)
    {
        var sqlType = GetSqlType(property);
        var identity = allowIdentity && property.ValueGenerated == ValueGenerated.OnAdd && IsIntegerType(property.ClrType)
            ? " IDENTITY(1,1)"
            : string.Empty;
        var defaultSql = ResolveDefaultExpression(property);

        if (identity.Length > 0)
        {
            return $"[{columnName}] {sqlType}{identity} NOT NULL";
        }

        if (isNewTable)
        {
            if (property.IsNullable)
                return $"[{columnName}] {sqlType} NULL";

            if (!string.IsNullOrWhiteSpace(defaultSql))
                return $"[{columnName}] {sqlType} NOT NULL CONSTRAINT [{GetDefaultConstraintName(storeObject, columnName)}] DEFAULT ({defaultSql})";

            return $"[{columnName}] {sqlType} NOT NULL";
        }

        if (!string.IsNullOrWhiteSpace(defaultSql))
            return $"[{columnName}] {sqlType} NOT NULL CONSTRAINT [{GetDefaultConstraintName(storeObject, columnName)}] DEFAULT ({defaultSql})";

        return $"[{columnName}] {sqlType} NULL";
    }

    private static string GetSqlType(IProperty property)
    {
        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        if (clrType == typeof(int))
            return "INT";
        if (clrType == typeof(long))
            return "BIGINT";
        if (clrType == typeof(short))
            return "SMALLINT";
        if (clrType == typeof(bool))
            return "BIT";
        if (clrType == typeof(DateTime))
            return "DATETIME2";
        if (clrType == typeof(decimal))
        {
            var precision = property.GetPrecision() ?? 18;
            var scale = property.GetScale() ?? 2;
            return $"DECIMAL({precision},{scale})";
        }
        if (clrType == typeof(double))
            return "FLOAT";
        if (clrType == typeof(float))
            return "REAL";
        if (clrType == typeof(Guid))
            return "UNIQUEIDENTIFIER";
        if (clrType == typeof(byte[]))
            return "VARBINARY(MAX)";

        var maxLength = property.GetMaxLength();
        if (maxLength.HasValue && maxLength.Value > 0 && maxLength.Value <= 4000)
            return $"NVARCHAR({maxLength.Value})";

        return "NVARCHAR(MAX)";
    }

    private static string? ResolveDefaultExpression(IProperty property)
    {
        var defaultSql = property.GetDefaultValueSql();
        if (!string.IsNullOrWhiteSpace(defaultSql))
            return defaultSql;

        var defaultValueAnnotation = property.FindAnnotation(RelationalAnnotationNames.DefaultValue);
        if (defaultValueAnnotation is null)
            return null;

        var defaultValue = defaultValueAnnotation.Value;
        return defaultValue switch
        {
            null => null,
            bool value => value ? "1" : "0",
            string value => $"N'{value.Replace("'", "''")}'",
            char value => $"N'{value.ToString().Replace("'", "''")}'",
            DateTime value => $"'{value:yyyy-MM-ddTHH:mm:ss.fffffff}'",
            DateTimeOffset value => $"'{value:yyyy-MM-ddTHH:mm:ss.fffffffzzz}'",
            Guid value => $"'{value:D}'",
            Enum value => Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            IFormattable value => value.ToString(null, CultureInfo.InvariantCulture),
            _ => null
        };
    }

    private static string GetPrimaryKeyName(IEntityType entityType)
    {
        return $"PK_{entityType.GetTableName()}";
    }

    private static string GetDefaultConstraintName(StoreObjectIdentifier storeObject, string columnName)
    {
        var schema = string.IsNullOrWhiteSpace(storeObject.Schema) ? "dbo" : storeObject.Schema;
        return $"DF_{schema}_{storeObject.Name}_{columnName}";
    }

    private static string ResolveColumnName(IProperty property, StoreObjectIdentifier storeObject)
    {
        return property.GetColumnName(storeObject)
            ?? property.GetColumnName()
            ?? property.FindAnnotation("Relational:ColumnName")?.Value?.ToString()
            ?? property.Name;
    }

    private static async Task<bool> TableExistsAsync(RagEduContext context, string schema, string tableName)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM INFORMATION_SCHEMA.TABLES
                           WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @tableName
                           """;

        return await ExecuteCountAsync(
            context,
            sql,
            new SqlParameter("@schema", schema),
            new SqlParameter("@tableName", tableName)) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(
        RagEduContext context,
        string schema,
        string tableName,
        string columnName)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM INFORMATION_SCHEMA.COLUMNS
                           WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @tableName AND COLUMN_NAME = @columnName
                           """;

        return await ExecuteCountAsync(
            context,
            sql,
            new SqlParameter("@schema", schema),
            new SqlParameter("@tableName", tableName),
            new SqlParameter("@columnName", columnName)) > 0;
    }

    private static async Task<bool> IndexExistsAsync(
        RagEduContext context,
        string schema,
        string tableName,
        string indexName)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM sys.indexes i
                           INNER JOIN sys.objects o ON i.object_id = o.object_id
                           INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                           WHERE s.name = @schema AND o.name = @tableName AND i.name = @indexName
                           """;

        return await ExecuteCountAsync(
            context,
            sql,
            new SqlParameter("@schema", schema),
            new SqlParameter("@tableName", tableName),
            new SqlParameter("@indexName", indexName)) > 0;
    }

    private static async Task<bool> DefaultConstraintExistsAsync(
        RagEduContext context,
        string schema,
        string tableName,
        string columnName)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM sys.default_constraints dc
                           INNER JOIN sys.columns c
                               ON c.object_id = dc.parent_object_id
                               AND c.column_id = dc.parent_column_id
                           INNER JOIN sys.tables t ON t.object_id = dc.parent_object_id
                           INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                           WHERE s.name = @schema
                             AND t.name = @tableName
                             AND c.name = @columnName
                           """;

        return await ExecuteCountAsync(
            context,
            sql,
            new SqlParameter("@schema", schema),
            new SqlParameter("@tableName", tableName),
            new SqlParameter("@columnName", columnName)) > 0;
    }

    private static async Task<int> ExecuteCountAsync(
        RagEduContext context,
        string sql,
        params SqlParameter[] parameters)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            var result = await command.ExecuteScalarAsync();
            return result is null || result == DBNull.Value
                ? 0
                : Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static bool IsIntegerType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying == typeof(int)
            || underlying == typeof(long)
            || underlying == typeof(short);
    }
}
