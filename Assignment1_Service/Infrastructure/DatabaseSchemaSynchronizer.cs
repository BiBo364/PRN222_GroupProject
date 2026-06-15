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
        var defaultSql = property.GetDefaultValueSql();

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
