using Microsoft.EntityFrameworkCore.Storage;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Infrastructure.Data;

namespace TemenosAlertManager.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly TemenosAlertContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    // Lazy-loaded repositories
    private IAlertRepository? _alerts;
    private IAlertOutboxRepository? _alertOutbox;
    private ICheckResultRepository? _checkResults;
    private IRepository<AuditEvent>? _auditEvents;
    private IConfigurationRepository? _configuration;
    
    // SOD/EOD repositories
    private ISODEODOperationRepository? _sodEodOperations;
    private IOperationStepRepository? _operationSteps;
    private IServiceActionRepository? _serviceActions;
    private IServiceConfigRepository? _serviceConfigs;

    public UnitOfWork(TemenosAlertContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IAlertRepository Alerts => 
        _alerts ??= new AlertRepository(_context);

    public IAlertOutboxRepository AlertOutbox => 
        _alertOutbox ??= new AlertOutboxRepository(_context);

    public ICheckResultRepository CheckResults => 
        _checkResults ??= new CheckResultRepository(_context);

    public IRepository<AuditEvent> AuditEvents => 
        _auditEvents ??= new Repository<AuditEvent>(_context);

    public IConfigurationRepository Configuration => 
        _configuration ??= new ConfigurationRepository(_context);

    // SOD/EOD repositories
    public ISODEODOperationRepository SODEODOperations => 
        _sodEodOperations ??= new SODEODOperationRepository(_context);

    public IOperationStepRepository OperationSteps => 
        _operationSteps ??= new OperationStepRepository(_context);

    public IServiceActionRepository ServiceActions => 
        _serviceActions ??= new ServiceActionRepository(_context);

    public IServiceConfigRepository ServiceConfigs => 
        _serviceConfigs ??= new ServiceConfigRepository(_context);

    // Phase 3 repositories
    private IRepository<ScheduledOperation>? _scheduledOperations;
    private IRepository<PerformanceBaseline>? _performanceBaselines;
    private IRepository<Core.Entities.PerformanceThreshold>? _performanceThresholds;
    private IRepository<GeneratedReport>? _generatedReports;
    private IRepository<DRCheckpoint>? _drCheckpoints;
    private IRepository<DRTest>? _drTests;
    private IRepository<AutomationWorkflow>? _automationWorkflows;
    private IRepository<WorkflowExecution>? _workflowExecutions;
    private IRepository<Core.Entities.OptimizationRecommendation>? _optimizationRecommendations;

    public IRepository<ScheduledOperation> ScheduledOperations => 
        _scheduledOperations ??= new Repository<ScheduledOperation>(_context);

    public IRepository<PerformanceBaseline> PerformanceBaselines => 
        _performanceBaselines ??= new Repository<PerformanceBaseline>(_context);

    public IRepository<Core.Entities.PerformanceThreshold> PerformanceThresholds => 
        _performanceThresholds ??= new Repository<Core.Entities.PerformanceThreshold>(_context);

    public IRepository<GeneratedReport> GeneratedReports => 
        _generatedReports ??= new Repository<GeneratedReport>(_context);

    public IRepository<DRCheckpoint> DRCheckpoints => 
        _drCheckpoints ??= new Repository<DRCheckpoint>(_context);

    public IRepository<DRTest> DRTests => 
        _drTests ??= new Repository<DRTest>(_context);

    public IRepository<AutomationWorkflow> AutomationWorkflows => 
        _automationWorkflows ??= new Repository<AutomationWorkflow>(_context);

    public IRepository<WorkflowExecution> WorkflowExecutions => 
        _workflowExecutions ??= new Repository<WorkflowExecution>(_context);

    public IRepository<Core.Entities.OptimizationRecommendation> OptimizationRecommendations => 
        _optimizationRecommendations ??= new Repository<Core.Entities.OptimizationRecommendation>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await _transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
            _disposed = true;
        }
    }
}