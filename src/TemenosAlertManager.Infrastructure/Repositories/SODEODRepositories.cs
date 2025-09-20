using Microsoft.EntityFrameworkCore;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Entities.Configuration;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;
using TemenosAlertManager.Infrastructure.Data;

namespace TemenosAlertManager.Infrastructure.Repositories;

public class SODEODOperationRepository : Repository<SODEODOperation>, ISODEODOperationRepository
{
    public SODEODOperationRepository(TemenosAlertContext context) : base(context)
    {
    }

    public async Task<SODEODOperation?> GetByOperationCodeAsync(string operationCode, CancellationToken cancellationToken = default)
    {
        return await _context.SODEODOperations
            .FirstOrDefaultAsync(op => op.OperationCode == operationCode, cancellationToken);
    }

    public async Task<SODEODOperation?> GetActiveByTypeAsync(string operationType, CancellationToken cancellationToken = default)
    {
        return await _context.SODEODOperations
            .Where(op => op.OperationType == operationType && 
                        (op.Status == "Initiated" || op.Status == "Running"))
            .OrderByDescending(op => op.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<SODEODOperation>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.SODEODOperations.OrderByDescending(op => op.StartTime);
        
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SODEODOperation>
        {
            Items = items.ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

public class OperationStepRepository : Repository<OperationStep>, IOperationStepRepository
{
    public OperationStepRepository(TemenosAlertContext context) : base(context)
    {
    }

    public async Task<IEnumerable<OperationStep>> GetByOperationIdAsync(int operationId, CancellationToken cancellationToken = default)
    {
        return await _context.OperationSteps
            .Where(step => step.OperationId == operationId)
            .OrderBy(step => step.StepOrder)
            .ToListAsync(cancellationToken);
    }
}

public class ServiceActionRepository : Repository<ServiceAction>, IServiceActionRepository
{
    public ServiceActionRepository(TemenosAlertContext context) : base(context)
    {
    }

    public async Task<PagedResult<ServiceAction>> GetByServiceIdPagedAsync(int serviceId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceActions
            .Where(action => action.ServiceConfigId == serviceId)
            .OrderByDescending(action => action.StartTime);
        
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceAction>
        {
            Items = items.ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

public class ServiceConfigRepository : Repository<ServiceConfig>, IServiceConfigRepository
{
    public ServiceConfigRepository(TemenosAlertContext context) : base(context)
    {
    }

    public new async Task<IEnumerable<ServiceConfig>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ServiceConfigs
            .Where(sc => sc.IsEnabled)
            .OrderBy(sc => sc.Name)
            .ToListAsync(cancellationToken);
    }
}