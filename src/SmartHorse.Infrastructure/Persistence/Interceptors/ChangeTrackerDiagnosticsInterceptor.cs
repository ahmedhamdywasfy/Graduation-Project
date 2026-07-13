using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SmartHorse.Infrastructure.Persistence.Interceptors;

/// <summary>
/// DIAGNOSTIC INTERCEPTOR — added to trace the Login DbUpdateConcurrencyException
/// (root-cause investigation, see LoginCommandHandler).
///
/// Dumps the FULL ChangeTracker state — every tracked entity's CLR type,
/// EntityState, primary key value, and (for Modified entities) every changed
/// property's original vs. current value — immediately before every
/// SaveChangesAsync call, and again (at Error level, with the exception) if
/// SaveChanges fails. It also logs the physical connection (server/database)
/// SaveChanges is about to run against, so Register's log line and the failing
/// Login's log line can be diffed to prove/disprove they hit the same database.
///
/// REMOVE OR DISABLE once the root cause is confirmed — this is verbose and not
/// meant to run permanently in production.
/// </summary>
public class ChangeTrackerDiagnosticsInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<ChangeTrackerDiagnosticsInterceptor> _logger;

    public ChangeTrackerDiagnosticsInterceptor(ILogger<ChangeTrackerDiagnosticsInterceptor> logger)
    {
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Dump(eventData.Context, "SavingChanges (sync)");
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Dump(eventData.Context, "SavingChangesAsync");
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        DumpOnFailure(eventData);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        DumpOnFailure(eventData);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Dump(DbContext? context, string phase)
    {
        if (context is null)
        {
            return;
        }

        var connection = context.Database.GetDbConnection();
        _logger.LogInformation(
            "[CHANGETRACKER-DIAG] {Phase} | ContextId={ContextId} | Target DB = {DataSource}/{Database} | " +
            "{EntryCount} tracked entries",
            phase, context.ContextId, connection.DataSource, connection.Database, context.ChangeTracker.Entries().Count());

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
            {
                // Not being written this SaveChanges — skip to keep the log focused
                // on what's actually about to hit the database. Comment this
                // condition out if you need to see Unchanged/Detached entries too.
                continue;
            }

            _logger.LogInformation(
                "[CHANGETRACKER-DIAG]   Entity={EntityType} State={State} PK={PrimaryKey} {Values}",
                entry.Metadata.ClrType.Name, entry.State, DescribePrimaryKey(entry), DescribeValues(entry));
        }
    }

    private void DumpOnFailure(DbContextErrorEventData eventData)
    {
        var context = eventData.Context;
        var connection = context?.Database.GetDbConnection();

        _logger.LogError(eventData.Exception,
            "[CHANGETRACKER-DIAG] *** SaveChanges FAILED *** ContextId={ContextId} Target DB = {DataSource}/{Database}",
            context?.ContextId, connection?.DataSource, connection?.Database);

        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            _logger.LogError(
                "[CHANGETRACKER-DIAG]   (at failure) Entity={EntityType} State={State} PK={PrimaryKey} {Values}",
                entry.Metadata.ClrType.Name, entry.State, DescribePrimaryKey(entry), DescribeValues(entry));
        }
    }

    private static string DescribePrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return "(no PK)";
        }

        var parts = key.Properties.Select(p => $"{p.Name}={entry.Property(p.Name).CurrentValue}");
        return string.Join(", ", parts);
    }

    /// <summary>
    /// For Modified entities: every property whose CurrentValue differs from
    /// OriginalValue, shown as OriginalValue -> CurrentValue. For Added entities:
    /// all current values (OriginalValues aren't meaningful pre-insert). For
    /// Deleted entities: the original values being removed.
    /// </summary>
    private static string DescribeValues(EntityEntry entry)
    {
        var sb = new StringBuilder();

        if (entry.State == EntityState.Added)
        {
            sb.Append("CurrentValues={ ");
            foreach (var prop in entry.Properties)
            {
                sb.Append(prop.Metadata.Name).Append('=').Append(prop.CurrentValue ?? "NULL").Append("; ");
            }
            sb.Append('}');
            return sb.ToString();
        }

        if (entry.State == EntityState.Deleted)
        {
            sb.Append("OriginalValues={ ");
            foreach (var prop in entry.Properties)
            {
                sb.Append(prop.Metadata.Name).Append('=').Append(prop.OriginalValue ?? "NULL").Append("; ");
            }
            sb.Append('}');
            return sb.ToString();
        }

        // Modified: show only the properties that actually changed.
        sb.Append("ChangedValues={ ");
        foreach (var prop in entry.Properties.Where(p => p.IsModified))
        {
            sb.Append(prop.Metadata.Name).Append(": ")
              .Append(prop.OriginalValue ?? "NULL").Append(" -> ").Append(prop.CurrentValue ?? "NULL")
              .Append("; ");
        }
        sb.Append('}');
        return sb.ToString();
    }
}
