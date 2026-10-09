using Npgsql;

namespace NetCha.Api.Audit;

public sealed class PostgresAuditRepository(NpgsqlDataSource dataSource) : IAuditRepository
{
    public async Task RecordAsync(bool succeeded, string? error, string? clientIp, string? userAgent, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("INSERT INTO captcha_audit (succeeded, error_code, client_ip, user_agent) VALUES ($1, $2, $3, $4)");
        command.Parameters.AddWithValue(succeeded);
        command.Parameters.AddWithValue((object?)error ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)clientIp ?? DBNull.Value);
        command.Parameters.AddWithValue((object?)userAgent ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntry>> ListAsync(int limit, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("SELECT id, occurred_at, succeeded, error_code, client_ip, user_agent FROM captcha_audit ORDER BY id DESC LIMIT $1");
        command.Parameters.AddWithValue(limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<AuditEntry>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new AuditEntry(reader.GetInt64(0), reader.GetFieldValue<DateTimeOffset>(1), reader.GetBoolean(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5)));
        return result;
    }

    public async Task<AuditStats> GetStatsAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("SELECT COUNT(*), COUNT(*) FILTER (WHERE succeeded), COUNT(*) FILTER (WHERE NOT succeeded), MAX(occurred_at) FROM captcha_audit");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new AuditStats(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3));
    }
}
