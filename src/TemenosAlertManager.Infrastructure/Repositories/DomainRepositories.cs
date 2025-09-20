using Microsoft.EntityFrameworkCore;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Enums;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Infrastructure.Data;

namespace TemenosAlertManager.Infrastructure.Repositories;

public class AlertRepository : Repository<Alert>, IAlertRepository
{
    public AlertRepository(TemenosAlertContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.State == AlertState.Active)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Alert>> GetAlertsByFingerprintAsync(string fingerprint, DateTime? since = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(a => a.Fingerprint == fingerprint);
        
        if (since.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= since.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Alert?> GetLatestAlertByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Fingerprint == fingerprint)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class AlertOutboxRepository : Repository<AlertOutbox>, IAlertOutboxRepository
{
    public AlertOutboxRepository(TemenosAlertContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AlertOutbox>> GetPendingAlertsAsync(int maxAttempts = 5, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ao => ao.Alert)
            .Where(ao => ao.Status == AlertDeliveryStatus.Pending && ao.Attempts < maxAttempts)
            .OrderBy(ao => ao.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AlertOutbox>> GetRetryableAlertsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(ao => ao.Alert)
            .Where(ao => ao.Status == AlertDeliveryStatus.Retrying && 
                        ao.NextRetryAt.HasValue && 
                        ao.NextRetryAt.Value <= now &&
                        ao.Attempts < ao.MaxAttempts)
            .OrderBy(ao => ao.NextRetryAt)
            .ToListAsync(cancellationToken);
    }
}

public class CheckResultRepository : Repository<CheckResult>, ICheckResultRepository
{
    public CheckResultRepository(TemenosAlertContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CheckResult>> GetLatestResultsByDomainAsync(string domain, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cr => cr.Domain.ToString() == domain)
            .OrderByDescending(cr => cr.CheckedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CheckResult>> GetResultsByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cr => cr.RunId == runId)
            .OrderBy(cr => cr.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}