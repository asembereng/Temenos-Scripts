using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Entities.Configuration;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Core.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
}

public interface IAlertRepository : IRepository<Alert>
{
    Task<IEnumerable<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Alert>> GetAlertsByFingerprintAsync(string fingerprint, DateTime? since = null, CancellationToken cancellationToken = default);
    Task<Alert?> GetLatestAlertByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default);
}

public interface IAlertOutboxRepository : IRepository<AlertOutbox>
{
    Task<IEnumerable<AlertOutbox>> GetPendingAlertsAsync(int maxAttempts = 5, CancellationToken cancellationToken = default);
    Task<IEnumerable<AlertOutbox>> GetRetryableAlertsAsync(CancellationToken cancellationToken = default);
}

public interface ICheckResultRepository : IRepository<CheckResult>
{
    Task<IEnumerable<CheckResult>> GetLatestResultsByDomainAsync(string domain, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckResult>> GetResultsByRunIdAsync(string runId, CancellationToken cancellationToken = default);
}

public interface IConfigurationRepository
{
    Task<IEnumerable<ServiceConfig>> GetServiceConfigsAsync(bool enabledOnly = true, CancellationToken cancellationToken = default);
    Task<IEnumerable<QueueConfig>> GetQueueConfigsAsync(bool enabledOnly = true, CancellationToken cancellationToken = default);
    Task<IEnumerable<SqlTargetConfig>> GetSqlTargetConfigsAsync(bool enabledOnly = true, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuthConfig>> GetAuthConfigsAsync(bool enabledOnly = true, CancellationToken cancellationToken = default);
    Task<IEnumerable<AlertConfig>> GetAlertConfigsAsync(bool enabledOnly = true, CancellationToken cancellationToken = default);
}

public interface ISODEODOperationRepository : IRepository<SODEODOperation>
{
    Task<SODEODOperation?> GetByOperationCodeAsync(string operationCode, CancellationToken cancellationToken = default);
    Task<SODEODOperation?> GetActiveByTypeAsync(string operationType, CancellationToken cancellationToken = default);
    Task<PagedResult<SODEODOperation>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

public interface IOperationStepRepository : IRepository<OperationStep>
{
    Task<IEnumerable<OperationStep>> GetByOperationIdAsync(int operationId, CancellationToken cancellationToken = default);
}

public interface IServiceActionRepository : IRepository<ServiceAction>
{
    Task<PagedResult<ServiceAction>> GetByServiceIdPagedAsync(int serviceId, int page, int pageSize, CancellationToken cancellationToken = default);
}

public interface IServiceConfigRepository : IRepository<ServiceConfig>
{
    new Task<IEnumerable<ServiceConfig>> GetAllAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IDisposable
{
    IAlertRepository Alerts { get; }
    IAlertOutboxRepository AlertOutbox { get; }
    ICheckResultRepository CheckResults { get; }
    IRepository<AuditEvent> AuditEvents { get; }
    IConfigurationRepository Configuration { get; }
    
    // SOD/EOD repositories
    ISODEODOperationRepository SODEODOperations { get; }
    IOperationStepRepository OperationSteps { get; }
    IServiceActionRepository ServiceActions { get; }
    
    // Phase 3 repositories
    IRepository<ScheduledOperation> ScheduledOperations { get; }
    IRepository<PerformanceBaseline> PerformanceBaselines { get; }
    IRepository<Core.Entities.PerformanceThreshold> PerformanceThresholds { get; }
    IRepository<GeneratedReport> GeneratedReports { get; }
    IRepository<DRCheckpoint> DRCheckpoints { get; }
    IRepository<DRTest> DRTests { get; }
    IRepository<AutomationWorkflow> AutomationWorkflows { get; }
    IRepository<WorkflowExecution> WorkflowExecutions { get; }
    IRepository<Core.Entities.OptimizationRecommendation> OptimizationRecommendations { get; }
    IServiceConfigRepository ServiceConfigs { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}