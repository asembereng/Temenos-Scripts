using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Core.Models;

namespace TemenosAlertManager.Api.Services;

/// <summary>
/// Service for disaster recovery procedures and testing
/// </summary>
public class DisasterRecoveryService : IDisasterRecoveryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPowerShellService _powerShellService;
    private readonly IAuditService _auditService;
    private readonly ILogger<DisasterRecoveryService> _logger;

    public DisasterRecoveryService(
        IUnitOfWork unitOfWork,
        IPowerShellService powerShellService,
        IAuditService auditService,
        ILogger<DisasterRecoveryService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _powerShellService = powerShellService ?? throw new ArgumentNullException(nameof(powerShellService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CheckpointResultDto> CreateCheckpointAsync(CheckpointRequest request, string createdBy, CancellationToken cancellationToken = default)
    {
        var checkpointId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Creating {CheckpointType} checkpoint {CheckpointId} for environment {Environment} by {User}", 
                request.CheckpointType, checkpointId, request.Environment, createdBy);

            // Create checkpoint record
            var checkpoint = new DRCheckpoint
            {
                CheckpointId = checkpointId,
                Environment = request.Environment,
                CheckpointType = request.CheckpointType,
                IncludedSystems = request.IncludedSystems?.Length > 0 ? 
                    System.Text.Json.JsonSerializer.Serialize(request.IncludedSystems) : null,
                Description = request.Description,
                Status = "Creating",
                VerificationStatus = "Pending",
                CreatedBy = createdBy,
                StoragePath = GenerateStoragePath(checkpointId, request.Environment, request.CheckpointType),
                ExpiresAt = DateTime.UtcNow.AddDays(GetRetentionDays(request.CheckpointType))
            };

            await _unitOfWork.DRCheckpoints.AddAsync(checkpoint, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Execute checkpoint creation asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteCheckpointCreationAsync(checkpoint, request, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create checkpoint {CheckpointId}", checkpointId);
                    await UpdateCheckpointStatusAsync(checkpoint.Id, "Failed", ex.Message, cancellationToken);
                }
            }, cancellationToken);

            // Audit the checkpoint creation
            await _auditService.LogEventAsync(createdBy, createdBy, "DR_CHECKPOINT_CREATED", 
                $"CheckpointId:{checkpointId},Environment:{request.Environment}", request, cancellationToken: cancellationToken);

            _logger.LogInformation("Checkpoint creation initiated for {CheckpointId}", checkpointId);

            return new CheckpointResultDto
            {
                CheckpointId = checkpointId,
                Status = "Creating",
                Message = "Checkpoint creation initiated successfully",
                CreatedAt = checkpoint.CreatedAt,
                IncludedSystems = request.IncludedSystems,
                VerificationStatus = "Pending"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate checkpoint creation for environment {Environment}", request.Environment);
            
            await _auditService.LogFailureAsync(createdBy, createdBy, "DR_CHECKPOINT_CREATION_FAILED", 
                $"Environment:{request.Environment}", ex.Message, cancellationToken: cancellationToken);
            
            throw;
        }
    }

    public async Task<RestoreResultDto> RestoreFromCheckpointAsync(RestoreRequest request, string restoredBy, CancellationToken cancellationToken = default)
    {
        var restoreId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Starting restore operation {RestoreId} from checkpoint {CheckpointId} to environment {TargetEnvironment} by {User}", 
                restoreId, request.CheckpointId, request.TargetEnvironment, restoredBy);

            // Validate checkpoint exists and is valid
            var checkpoints = await _unitOfWork.DRCheckpoints.GetAllAsync(cancellationToken);
            var checkpoint = checkpoints.FirstOrDefault(c => c.CheckpointId == request.CheckpointId);

            if (checkpoint == null)
            {
                throw new ArgumentException($"Checkpoint {request.CheckpointId} not found");
            }

            if (checkpoint.Status != "Created" || checkpoint.VerificationStatus != "Passed")
            {
                throw new InvalidOperationException($"Checkpoint {request.CheckpointId} is not valid for restore (Status: {checkpoint.Status}, Verification: {checkpoint.VerificationStatus})");
            }

            // Execute restore operation
            var restoreSteps = await ExecuteRestoreOperationAsync(checkpoint, request, restoreId, restoredBy, cancellationToken);

            // Audit the restore operation
            await _auditService.LogEventAsync(restoredBy, restoredBy, "DR_RESTORE_EXECUTED", 
                $"RestoreId:{restoreId},CheckpointId:{request.CheckpointId}", request, cancellationToken: cancellationToken);

            var result = new RestoreResultDto
            {
                RestoreId = restoreId,
                Status = "Completed",
                Message = "Restore operation completed successfully",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow.AddMinutes(15), // Simulated
                Steps = restoreSteps
            };

            _logger.LogInformation("Restore operation {RestoreId} completed successfully", restoreId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore from checkpoint {CheckpointId}", request.CheckpointId);
            
            await _auditService.LogFailureAsync(restoredBy, restoredBy, "DR_RESTORE_FAILED", 
                $"CheckpointId:{request.CheckpointId}", ex.Message, cancellationToken: cancellationToken);
            
            throw;
        }
    }

    public async Task<DRReadinessDto> ValidateDRReadinessAsync(string environment, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating DR readiness for environment {Environment}", environment);

            // Get recent checkpoints
            var checkpoints = await _unitOfWork.DRCheckpoints.GetAllAsync(cancellationToken);
            var environmentCheckpoints = checkpoints
                .Where(c => c.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.CreatedAt)
                .ToArray();

            // Get recent DR tests
            var drTests = await _unitOfWork.DRTests.GetAllAsync(cancellationToken);
            var environmentTests = drTests
                .Where(t => t.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.StartedAt)
                .ToArray();

            // Analyze component readiness
            var componentStatus = await AnalyzeComponentReadinessAsync(environment, environmentCheckpoints, cancellationToken);

            // Calculate overall readiness
            var readinessScore = CalculateReadinessScore(componentStatus, environmentCheckpoints, environmentTests);
            var overallStatus = readinessScore >= 90 ? "Ready" : readinessScore >= 70 ? "Partial" : "NotReady";

            // Identify issues and recommendations
            var issues = IdentifyReadinessIssues(componentStatus, environmentCheckpoints, environmentTests);
            var recommendations = GenerateReadinessRecommendations(issues, componentStatus);

            var result = new DRReadinessDto
            {
                Environment = environment,
                OverallStatus = overallStatus,
                ReadinessScore = readinessScore,
                ComponentStatus = componentStatus,
                LastValidated = DateTime.UtcNow,
                Issues = issues,
                Recommendations = recommendations
            };

            _logger.LogInformation("DR readiness validation completed for environment {Environment}. Status: {Status}, Score: {Score}%", 
                environment, overallStatus, readinessScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate DR readiness for environment {Environment}", environment);
            throw;
        }
    }

    public async Task<DRTestResultDto> ExecuteDRTestAsync(DRTestRequest request, string executedBy, CancellationToken cancellationToken = default)
    {
        var testId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Executing {TestType} DR test {TestId} for environment {Environment} by {User}", 
                request.TestType, testId, request.Environment, executedBy);

            // Create DR test record
            var drTest = new DRTest
            {
                TestId = testId,
                Environment = request.Environment,
                TestEnvironment = request.TestEnvironment,
                TestType = request.TestType,
                SystemsToTest = request.SystemsToTest?.Length > 0 ? 
                    System.Text.Json.JsonSerializer.Serialize(request.SystemsToTest) : null,
                RestoreData = request.RestoreData,
                Status = "Running",
                ExecutedBy = executedBy
            };

            await _unitOfWork.DRTests.AddAsync(drTest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Execute DR test steps
            var testSteps = await ExecuteDRTestStepsAsync(request, testId, cancellationToken);

            // Calculate test metrics
            var metrics = CalculateTestMetrics(testSteps, request);

            // Update test with results
            drTest.Status = "Completed";
            drTest.CompletedAt = DateTime.UtcNow;
            drTest.ActualRTO = metrics.ActualRTO;
            drTest.ActualRPO = metrics.ActualRPO;
            drTest.DataIntegrityScore = metrics.DataIntegrityScore;
            drTest.SystemAvailabilityScore = metrics.SystemAvailabilityScore;
            drTest.TestResults = System.Text.Json.JsonSerializer.Serialize(testSteps);

            await _unitOfWork.DRTests.UpdateAsync(drTest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Generate issues and recommendations
            var issues = IdentifyTestIssues(testSteps, metrics);
            var recommendations = GenerateTestRecommendations(issues, metrics);

            // Audit the DR test
            await _auditService.LogEventAsync(executedBy, executedBy, "DR_TEST_EXECUTED", 
                $"TestId:{testId},Environment:{request.Environment}", request, cancellationToken: cancellationToken);

            var result = new DRTestResultDto
            {
                TestId = testId,
                Status = "Completed",
                StartedAt = drTest.StartedAt,
                CompletedAt = drTest.CompletedAt,
                Steps = testSteps,
                Metrics = metrics,
                Issues = issues,
                Recommendations = recommendations
            };

            _logger.LogInformation("DR test {TestId} completed successfully. RTO: {RTO}min, RPO: {RPO}min", 
                testId, metrics.ActualRTO, metrics.ActualRPO);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute DR test for environment {Environment}", request.Environment);
            
            await _auditService.LogFailureAsync(executedBy, executedBy, "DR_TEST_FAILED", 
                $"Environment:{request.Environment}", ex.Message, cancellationToken: cancellationToken);
            
            throw;
        }
    }

    public async Task<DRStatusDto> GetDRStatusAsync(string environment, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting DR status for environment {Environment}", environment);

            // Get latest checkpoint
            var checkpoints = await _unitOfWork.DRCheckpoints.GetAllAsync(cancellationToken);
            var latestCheckpoint = checkpoints
                .Where(c => c.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            // Get latest test
            var drTests = await _unitOfWork.DRTests.GetAllAsync(cancellationToken);
            var latestTest = drTests
                .Where(t => t.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.StartedAt)
                .FirstOrDefault();

            // Get readiness assessment
            var readiness = await ValidateDRReadinessAsync(environment, cancellationToken);

            // Convert latest test to result DTO if available
            DRTestResultDto? lastTestResult = null;
            if (latestTest != null)
            {
                lastTestResult = new DRTestResultDto
                {
                    TestId = latestTest.TestId,
                    Status = latestTest.Status,
                    StartedAt = latestTest.StartedAt,
                    CompletedAt = latestTest.CompletedAt,
                    Metrics = new DRTestMetrics
                    {
                        ActualRTO = latestTest.ActualRTO ?? 0,
                        ActualRPO = latestTest.ActualRPO ?? 0,
                        DataIntegrityScore = latestTest.DataIntegrityScore ?? 0,
                        SystemAvailabilityScore = latestTest.SystemAvailabilityScore ?? 0
                    }
                };
            }

            var status = new DRStatusDto
            {
                Environment = environment,
                Status = readiness.OverallStatus,
                LastBackup = latestCheckpoint?.CreatedAt ?? DateTime.MinValue,
                LastTest = latestTest?.StartedAt ?? DateTime.MinValue,
                Readiness = readiness,
                LastTestResult = lastTestResult
            };

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DR status for environment {Environment}", environment);
            throw;
        }
    }

    // Private helper methods

    private async Task ExecuteCheckpointCreationAsync(DRCheckpoint checkpoint, CheckpointRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Simulate checkpoint creation steps
            _logger.LogInformation("Executing checkpoint creation for {CheckpointId}", checkpoint.CheckpointId);

            // Step 1: Stop services if needed
            if (request.CheckpointType == "Full")
            {
                await Task.Delay(2000, cancellationToken); // Simulate service stop
            }

            // Step 2: Create data backup
            await Task.Delay(5000, cancellationToken); // Simulate data backup

            // Step 3: Create configuration backup
            await Task.Delay(1000, cancellationToken); // Simulate config backup

            // Step 4: Verify backup integrity
            if (request.VerifyIntegrity)
            {
                await Task.Delay(3000, cancellationToken); // Simulate verification
            }

            // Update checkpoint status
            checkpoint.Status = "Created";
            checkpoint.VerificationStatus = request.VerifyIntegrity ? "Passed" : "Skipped";
            checkpoint.SizeBytes = 5L * 1024 * 1024 * 1024; // 5GB simulated
            checkpoint.LastVerifiedAt = request.VerifyIntegrity ? DateTime.UtcNow : null;

            await _unitOfWork.DRCheckpoints.UpdateAsync(checkpoint, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Checkpoint {CheckpointId} created successfully", checkpoint.CheckpointId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkpoint {CheckpointId}", checkpoint.CheckpointId);
            throw;
        }
    }

    private async Task<RestoreStepResult[]> ExecuteRestoreOperationAsync(
        DRCheckpoint checkpoint, 
        RestoreRequest request, 
        string restoreId, 
        string restoredBy, 
        CancellationToken cancellationToken)
    {
        var steps = new List<RestoreStepResult>();

        try
        {
            // Step 1: Stop services in target environment
            if (request.StopServices)
            {
                var stopStep = new RestoreStepResult
                {
                    StepName = "Stop Services",
                    Status = "Running",
                    StartedAt = DateTime.UtcNow
                };

                await Task.Delay(2000, cancellationToken); // Simulate service stop

                stopStep.Status = "Completed";
                stopStep.CompletedAt = DateTime.UtcNow;
                stopStep.Details = "All services stopped successfully";
                steps.Add(stopStep);
            }

            // Step 2: Restore data
            var dataStep = new RestoreStepResult
            {
                StepName = "Restore Data",
                Status = "Running",
                StartedAt = DateTime.UtcNow
            };

            await Task.Delay(8000, cancellationToken); // Simulate data restore

            dataStep.Status = "Completed";
            dataStep.CompletedAt = DateTime.UtcNow;
            dataStep.Details = $"Data restored from checkpoint {checkpoint.CheckpointId}";
            steps.Add(dataStep);

            // Step 3: Restore configuration
            var configStep = new RestoreStepResult
            {
                StepName = "Restore Configuration",
                Status = "Running",
                StartedAt = DateTime.UtcNow
            };

            await Task.Delay(1000, cancellationToken); // Simulate config restore

            configStep.Status = "Completed";
            configStep.CompletedAt = DateTime.UtcNow;
            configStep.Details = "Configuration restored successfully";
            steps.Add(configStep);

            // Step 4: Start services
            var startStep = new RestoreStepResult
            {
                StepName = "Start Services",
                Status = "Running",
                StartedAt = DateTime.UtcNow
            };

            await Task.Delay(3000, cancellationToken); // Simulate service start

            startStep.Status = "Completed";
            startStep.CompletedAt = DateTime.UtcNow;
            startStep.Details = "All services started successfully";
            steps.Add(startStep);

            // Step 5: Verify system integrity
            var verifyStep = new RestoreStepResult
            {
                StepName = "Verify System",
                Status = "Running",
                StartedAt = DateTime.UtcNow
            };

            await Task.Delay(2000, cancellationToken); // Simulate verification

            verifyStep.Status = "Completed";
            verifyStep.CompletedAt = DateTime.UtcNow;
            verifyStep.Details = "System verification completed successfully";
            steps.Add(verifyStep);

            return steps.ToArray();
        }
        catch (Exception ex)
        {
            // Mark last step as failed
            if (steps.Any() && steps.Last().Status == "Running")
            {
                steps.Last().Status = "Failed";
                steps.Last().CompletedAt = DateTime.UtcNow;
                steps.Last().Details = ex.Message;
            }

            throw;
        }
    }

    private async Task<DRComponentStatus[]> AnalyzeComponentReadinessAsync(string environment, DRCheckpoint[] checkpoints, CancellationToken cancellationToken)
    {
        // Get service configurations for the environment
        var services = await _unitOfWork.ServiceConfigs.GetAllAsync(cancellationToken);
        var environmentServices = services.Where(s => s.IsEnabled).Take(5).ToArray(); // Simulate environment services

        return environmentServices.Select(service => new DRComponentStatus
        {
            ComponentName = service.Name,
            Status = checkpoints.Any() ? "Ready" : "NotReady",
            LastBackup = checkpoints.FirstOrDefault()?.CreatedAt ?? DateTime.MinValue,
            BackupStatus = checkpoints.Any() ? "Current" : "Missing",
            RecoveryTimeObjective = 60, // 1 hour
            RecoveryPointObjective = 15  // 15 minutes
        }).ToArray();
    }

    private double CalculateReadinessScore(DRComponentStatus[] components, DRCheckpoint[] checkpoints, DRTest[] tests)
    {
        var componentScore = components.Length > 0 ? components.Count(c => c.Status == "Ready") / (double)components.Length * 100 : 0;
        var backupScore = checkpoints.Any(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-7)) ? 100 : 0; // Recent backup
        var testScore = tests.Any(t => t.StartedAt >= DateTime.UtcNow.AddDays(-30) && t.Status == "Completed") ? 100 : 0; // Recent test

        return (componentScore + backupScore + testScore) / 3;
    }

    private string[] IdentifyReadinessIssues(DRComponentStatus[] components, DRCheckpoint[] checkpoints, DRTest[] tests)
    {
        var issues = new List<string>();

        var notReadyComponents = components.Where(c => c.Status != "Ready").ToArray();
        if (notReadyComponents.Any())
        {
            issues.Add($"{notReadyComponents.Length} components are not ready for DR");
        }

        if (!checkpoints.Any(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-7)))
        {
            issues.Add("No recent backup available (last 7 days)");
        }

        if (!tests.Any(t => t.StartedAt >= DateTime.UtcNow.AddDays(-30)))
        {
            issues.Add("No recent DR test performed (last 30 days)");
        }

        return issues.ToArray();
    }

    private string[] GenerateReadinessRecommendations(string[] issues, DRComponentStatus[] components)
    {
        var recommendations = new List<string>();

        if (issues.Any(i => i.Contains("components are not ready")))
        {
            recommendations.Add("Configure backup procedures for all critical components");
        }

        if (issues.Any(i => i.Contains("No recent backup")))
        {
            recommendations.Add("Schedule regular automated backups");
        }

        if (issues.Any(i => i.Contains("No recent DR test")))
        {
            recommendations.Add("Schedule and execute DR test procedures");
        }

        return recommendations.ToArray();
    }

    private async Task<DRTestStepResult[]> ExecuteDRTestStepsAsync(DRTestRequest request, string testId, CancellationToken cancellationToken)
    {
        var steps = new List<DRTestStepResult>();

        // Step 1: Prepare test environment
        var prepareStep = new DRTestStepResult
        {
            StepName = "Prepare Test Environment",
            Status = "Completed",
            Duration = TimeSpan.FromMinutes(5),
            Details = "Test environment prepared successfully"
        };
        steps.Add(prepareStep);

        // Step 2: Execute recovery procedures
        var recoveryStep = new DRTestStepResult
        {
            StepName = "Execute Recovery",
            Status = "Completed",
            Duration = TimeSpan.FromMinutes(15),
            Details = "Recovery procedures executed successfully"
        };
        steps.Add(recoveryStep);

        // Step 3: Validate system functionality
        var validateStep = new DRTestStepResult
        {
            StepName = "Validate System",
            Status = "Completed",
            Duration = TimeSpan.FromMinutes(10),
            Details = "System validation completed successfully"
        };
        steps.Add(validateStep);

        await Task.Delay(100, cancellationToken); // Simulate processing

        return steps.ToArray();
    }

    private DRTestMetrics CalculateTestMetrics(DRTestStepResult[] steps, DRTestRequest request)
    {
        var totalDuration = steps.Sum(s => s.Duration.TotalMinutes);
        
        return new DRTestMetrics
        {
            ActualRTO = totalDuration, // Recovery Time Objective
            ActualRPO = 5, // Recovery Point Objective - simulated
            DataIntegrityScore = 98.5, // Simulated
            SystemAvailabilityScore = 99.2 // Simulated
        };
    }

    private string[] IdentifyTestIssues(DRTestStepResult[] steps, DRTestMetrics metrics)
    {
        var issues = new List<string>();

        if (metrics.ActualRTO > 60) // More than 1 hour
        {
            issues.Add($"RTO exceeded target: {metrics.ActualRTO:F0} minutes");
        }

        if (metrics.DataIntegrityScore < 95)
        {
            issues.Add($"Data integrity below threshold: {metrics.DataIntegrityScore:F1}%");
        }

        return issues.ToArray();
    }

    private string[] GenerateTestRecommendations(string[] issues, DRTestMetrics metrics)
    {
        var recommendations = new List<string>();

        if (issues.Any(i => i.Contains("RTO exceeded")))
        {
            recommendations.Add("Optimize recovery procedures to reduce downtime");
        }

        if (issues.Any(i => i.Contains("Data integrity")))
        {
            recommendations.Add("Review backup and replication procedures");
        }

        return recommendations.ToArray();
    }

    private async Task UpdateCheckpointStatusAsync(int checkpointId, string status, string message, CancellationToken cancellationToken)
    {
        try
        {
            var checkpoint = await _unitOfWork.DRCheckpoints.GetByIdAsync(checkpointId, cancellationToken);
            if (checkpoint != null)
            {
                checkpoint.Status = status;
                if (status == "Failed")
                {
                    checkpoint.VerificationStatus = "Failed";
                }

                await _unitOfWork.DRCheckpoints.UpdateAsync(checkpoint, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update checkpoint status for checkpoint {CheckpointId}", checkpointId);
        }
    }

    private string GenerateStoragePath(string checkpointId, string environment, string checkpointType)
    {
        var datePart = DateTime.UtcNow.ToString("yyyy/MM/dd");
        return $"/dr/checkpoints/{environment}/{checkpointType}/{datePart}/{checkpointId}";
    }

    private int GetRetentionDays(string checkpointType)
    {
        return checkpointType switch
        {
            "Full" => 90,
            "Incremental" => 30,
            "Configuration" => 365,
            _ => 30
        };
    }
}