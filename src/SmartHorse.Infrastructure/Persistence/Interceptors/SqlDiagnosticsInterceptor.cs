using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SmartHorse.Infrastructure.Persistence.Interceptors;

/// <summary>
/// DIAGNOSTIC INTERCEPTOR — added to trace the Login DbUpdateConcurrencyException
/// (root-cause investigation, see LoginCommandHandler). Logs the exact SQL text,
/// every parameter name/value, and — most importantly — the actual rows-affected
/// count returned by SQL Server for every INSERT/UPDATE/DELETE. That last number
/// is the smoking gun: EF's DbUpdateConcurrencyException fires when this value is
/// 0 while EF expected 1, so <see cref="NonQueryExecutedAsync"/> logs a Warning
/// the moment that happens, with the exact statement and parameters that caused it.
///
/// REMOVE OR DISABLE (or at minimum turn off <c>EnableSensitiveDataLogging</c> /
/// this interceptor) once the root cause is confirmed — this deliberately logs
/// parameter values (emails, token hashes, IPs) which is not appropriate to leave
/// enabled permanently in a real production environment.
/// </summary>
public class SqlDiagnosticsInterceptor : DbCommandInterceptor
{
    private readonly ILogger<SqlDiagnosticsInterceptor> _logger;

    public SqlDiagnosticsInterceptor(ILogger<SqlDiagnosticsInterceptor> logger)
    {
        _logger = logger;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        LogCommand("QUERY", command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        LogCommand("QUERY", command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        LogCommand("NON-QUERY (about to execute)", command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        LogCommand("NON-QUERY (about to execute)", command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    // ---- THE CRITICAL HOOK ----
    // This fires AFTER the command actually ran against SQL Server, with the real
    // rows-affected count. If this is 0 for an UPDATE/DELETE, EF's SaveChanges will
    // throw DbUpdateConcurrencyException immediately after this returns.
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogResult(command, result);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        LogResult(command, result);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        _logger.LogError(eventData.Exception,
            "[SQL-DIAG] Command FAILED. Connection={DataSource}/{Database} CommandText={CommandText} {Parameters}",
            command.Connection?.DataSource, command.Connection?.Database, command.CommandText, DescribeParameters(command));
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogError(eventData.Exception,
            "[SQL-DIAG] Command FAILED. Connection={DataSource}/{Database} CommandText={CommandText} {Parameters}",
            command.Connection?.DataSource, command.Connection?.Database, command.CommandText, DescribeParameters(command));
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private void LogCommand(string phase, DbCommand command)
    {
        _logger.LogInformation(
            "[SQL-DIAG] {Phase} on {DataSource}/{Database}:\n{CommandText}\n{Parameters}",
            phase, command.Connection?.DataSource, command.Connection?.Database, command.CommandText, DescribeParameters(command));
    }

    private void LogResult(DbCommand command, int rowsAffected)
    {
        var isWriteStatement = command.CommandText.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
            || command.CommandText.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
            || command.CommandText.Contains("MERGE", StringComparison.OrdinalIgnoreCase);

        if (isWriteStatement && rowsAffected == 0)
        {
            // THIS is the line to grep for in the logs after the next failing Login.
            _logger.LogWarning(
                "[SQL-DIAG] *** ZERO ROWS AFFECTED on write statement — this is what triggers " +
                "DbUpdateConcurrencyException *** Connection={DataSource}/{Database} " +
                "CommandText={CommandText} {Parameters}",
                command.Connection?.DataSource, command.Connection?.Database, command.CommandText, DescribeParameters(command));
        }
        else
        {
            _logger.LogInformation(
                "[SQL-DIAG] Executed on {DataSource}/{Database}. RowsAffected={RowsAffected} CommandText={CommandText} {Parameters}",
                command.Connection?.DataSource, command.Connection?.Database, rowsAffected, command.CommandText, DescribeParameters(command));
        }
    }

    private static string DescribeParameters(DbCommand command)
    {
        if (command.Parameters.Count == 0)
        {
            return "Parameters=(none)";
        }

        var sb = new StringBuilder("Parameters={ ");
        foreach (DbParameter p in command.Parameters)
        {
            sb.Append(p.ParameterName).Append('=').Append(p.Value ?? "NULL").Append("; ");
        }

        sb.Append('}');
        return sb.ToString();
    }
}
