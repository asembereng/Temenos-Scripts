using Microsoft.EntityFrameworkCore;
using TemenosAlertManager.Core.Entities.Configuration;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Infrastructure.Data;

namespace TemenosAlertManager.Infrastructure.Repositories;

public class ConfigurationRepository : IConfigurationRepository
{
    private readonly TemenosAlertContext _context;

    public ConfigurationRepository(TemenosAlertContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<ServiceConfig>> GetServiceConfigsAsync(bool enabledOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceConfigs.AsQueryable();
        
        if (enabledOnly)
        {
            query = query.Where(sc => sc.IsEnabled);
        }

        return await query
            .OrderBy(sc => sc.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<QueueConfig>> GetQueueConfigsAsync(bool enabledOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.QueueConfigs.AsQueryable();
        
        if (enabledOnly)
        {
            query = query.Where(qc => qc.IsEnabled);
        }

        return await query
            .OrderBy(qc => qc.QueueManager)
            .ThenBy(qc => qc.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SqlTargetConfig>> GetSqlTargetConfigsAsync(bool enabledOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.SqlTargetConfigs.AsQueryable();
        
        if (enabledOnly)
        {
            query = query.Where(stc => stc.IsEnabled);
        }

        return await query
            .OrderBy(stc => stc.InstanceName)
            .ThenBy(stc => stc.DatabaseName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuthConfig>> GetAuthConfigsAsync(bool enabledOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.AuthConfigs.AsQueryable();
        
        if (enabledOnly)
        {
            query = query.Where(ac => ac.IsEnabled);
        }

        return await query
            .OrderBy(ac => ac.Role)
            .ThenBy(ac => ac.AdGroupName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AlertConfig>> GetAlertConfigsAsync(bool enabledOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.AlertConfigs.AsQueryable();
        
        if (enabledOnly)
        {
            query = query.Where(ac => ac.IsEnabled);
        }

        return await query
            .OrderBy(ac => ac.Domain)
            .ThenBy(ac => ac.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SystemConfig>> GetSystemConfigsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigs
            .OrderBy(sc => sc.Category)
            .ThenBy(sc => sc.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<SystemConfig> AddAsync(SystemConfig entity, CancellationToken cancellationToken = default)
    {
        _context.SystemConfigs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(SystemConfig entity, CancellationToken cancellationToken = default)
    {
        _context.SystemConfigs.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthConfig?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.AuthConfigs.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<AuthConfig> AddAsync(AuthConfig entity, CancellationToken cancellationToken = default)
    {
        _context.AuthConfigs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(AuthConfig entity, CancellationToken cancellationToken = default)
    {
        _context.AuthConfigs.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var authConfig = await _context.AuthConfigs.FindAsync(new object[] { id }, cancellationToken);
        if (authConfig != null)
        {
            _context.AuthConfigs.Remove(authConfig);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}