import React, { useState, useEffect } from 'react';
import apiService from '../services/apiService';
import { SODRequest, EODRequest, OperationResult } from '../types';
import './SODEODOperations.css';

const SODEODOperations: React.FC = () => {
  const [activeOperations, setActiveOperations] = useState<OperationResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // SOD Form State
  const [sodForm, setSodForm] = useState<SODRequest>({
    environment: 'DEV',
    servicesFilter: [],
    dryRun: true,
    forceExecution: false,
    comments: ''
  });

  // EOD Form State
  const [eodForm, setEodForm] = useState<EODRequest>({
    environment: 'DEV',
    servicesFilter: [],
    dryRun: true,
    forceExecution: false,
    cutoffTime: '',
    comments: ''
  });

  const environments = ['DEV', 'TEST', 'UAT', 'PROD'];
  const availableServices = [
    'T24AppServer',
    'TPHCoreService',
    'MQServer',
    'DatabaseService',
    'JVMService'
  ];

  useEffect(() => {
    loadActiveOperations();
    // Refresh active operations every 10 seconds
    const interval = setInterval(loadActiveOperations, 10000);
    return () => clearInterval(interval);
  }, []);

  const loadActiveOperations = async () => {
    try {
      const operations = await apiService.getActiveOperations();
      setActiveOperations(operations);
    } catch (err) {
      console.error('Failed to load active operations:', err);
    }
  };

  const handleStartSOD = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await apiService.startSOD(sodForm);
      setSuccess(`SOD operation started successfully. Operation ID: ${result.operationId}`);
      await loadActiveOperations();
      
      // Reset form
      setSodForm({
        environment: 'DEV',
        servicesFilter: [],
        dryRun: true,
        forceExecution: false,
        comments: ''
      });
    } catch (err: any) {
      setError(`Failed to start SOD operation: ${err.message || 'Unknown error'}`);
    } finally {
      setLoading(false);
    }
  };

  const handleStartEOD = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await apiService.startEOD(eodForm);
      setSuccess(`EOD operation started successfully. Operation ID: ${result.operationId}`);
      await loadActiveOperations();
      
      // Reset form
      setEodForm({
        environment: 'DEV',
        servicesFilter: [],
        dryRun: true,
        forceExecution: false,
        cutoffTime: '',
        comments: ''
      });
    } catch (err: any) {
      setError(`Failed to start EOD operation: ${err.message || 'Unknown error'}`);
    } finally {
      setLoading(false);
    }
  };

  const handleCancelOperation = async (operationId: string) => {
    try {
      await apiService.cancelOperation(operationId);
      setSuccess(`Operation ${operationId} cancelled successfully`);
      await loadActiveOperations();
    } catch (err: any) {
      setError(`Failed to cancel operation: ${err.message || 'Unknown error'}`);
    }
  };

  const getStatusClassName = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
        return 'status-healthy';
      case 'running':
        return 'status-warning';
      case 'failed':
      case 'cancelled':
        return 'status-critical';
      default:
        return 'status-unknown';
    }
  };

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  return (
    <div className="sod-eod-operations">
      <div className="page-header">
        <h1>SOD/EOD Operations</h1>
        <div className="page-actions">
          <button className="btn btn-secondary" onClick={loadActiveOperations}>
            🔄 Refresh
          </button>
        </div>
      </div>

      {error && (
        <div className="error">
          {error}
          <button className="btn btn-sm btn-primary" onClick={() => setError(null)}>
            Dismiss
          </button>
        </div>
      )}

      {success && (
        <div className="success">
          {success}
          <button className="btn btn-sm btn-secondary" onClick={() => setSuccess(null)}>
            Dismiss
          </button>
        </div>
      )}

      {/* Active Operations */}
      {activeOperations.length > 0 && (
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">Active Operations</h3>
          </div>
          <div className="active-operations">
            {activeOperations.map((operation) => (
              <div key={operation.operationId} className="operation-card">
                <div className="operation-header">
                  <div className="operation-info">
                    <h4 className="operation-id">{operation.operationId}</h4>
                    <span className={`status-badge ${getStatusClassName(operation.status)}`}>
                      {operation.status}
                    </span>
                  </div>
                  {(operation.status === 'Running' || operation.status === 'Pending') && (
                    <button
                      className="btn btn-sm btn-danger"
                      onClick={() => handleCancelOperation(operation.operationId)}
                    >
                      Cancel
                    </button>
                  )}
                </div>
                <div className="operation-progress">
                  <div className="progress">
                    <div 
                      className="progress-bar"
                      style={{ width: `${operation.progressPercentage}%` }}
                    >
                      {operation.progressPercentage}%
                    </div>
                  </div>
                  <div className="operation-step">{operation.currentStep}</div>
                </div>
                <div className="operation-details">
                  <div className="detail-item">
                    <span className="detail-label">Started:</span>
                    <span className="detail-value">{formatDateTime(operation.startTime)}</span>
                  </div>
                  {operation.estimatedDuration && (
                    <div className="detail-item">
                      <span className="detail-label">ETA:</span>
                      <span className="detail-value">{operation.estimatedDuration}</span>
                    </div>
                  )}
                </div>
                {operation.steps.length > 0 && (
                  <div className="operation-steps">
                    <h5>Steps:</h5>
                    <div className="steps-list">
                      {operation.steps.map((step, index) => (
                        <div key={index} className={`step-item ${step.status.toLowerCase()}`}>
                          <span className="step-name">{step.name}</span>
                          <span className={`step-status ${getStatusClassName(step.status)}`}>
                            {step.status}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="operations-grid">
        {/* SOD Operation Form */}
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">Start of Day (SOD) Operation</h3>
          </div>
          <form onSubmit={handleStartSOD} className="operation-form">
            <div className="form-group">
              <label className="form-label">Environment</label>
              <select
                className="form-control"
                value={sodForm.environment}
                onChange={(e) => setSodForm({ ...sodForm, environment: e.target.value })}
                required
              >
                {environments.map(env => (
                  <option key={env} value={env}>{env}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label className="form-label">Services Filter (optional)</label>
              <div className="services-filter">
                {availableServices.map(service => (
                  <label key={service} className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={sodForm.servicesFilter.includes(service)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSodForm({
                            ...sodForm,
                            servicesFilter: [...sodForm.servicesFilter, service]
                          });
                        } else {
                          setSodForm({
                            ...sodForm,
                            servicesFilter: sodForm.servicesFilter.filter(s => s !== service)
                          });
                        }
                      }}
                    />
                    {service}
                  </label>
                ))}
              </div>
            </div>

            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={sodForm.dryRun}
                  onChange={(e) => setSodForm({ ...sodForm, dryRun: e.target.checked })}
                />
                Dry Run (simulation only)
              </label>
            </div>

            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={sodForm.forceExecution}
                  onChange={(e) => setSodForm({ ...sodForm, forceExecution: e.target.checked })}
                />
                Force Execution
              </label>
            </div>

            <div className="form-group">
              <label className="form-label">Comments</label>
              <textarea
                className="form-control"
                rows={3}
                value={sodForm.comments}
                onChange={(e) => setSodForm({ ...sodForm, comments: e.target.value })}
                placeholder="Enter any comments about this operation..."
              />
            </div>

            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? '⏳ Starting...' : '▶️ Start SOD Operation'}
            </button>
          </form>
        </div>

        {/* EOD Operation Form */}
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">End of Day (EOD) Operation</h3>
          </div>
          <form onSubmit={handleStartEOD} className="operation-form">
            <div className="form-group">
              <label className="form-label">Environment</label>
              <select
                className="form-control"
                value={eodForm.environment}
                onChange={(e) => setEodForm({ ...eodForm, environment: e.target.value })}
                required
              >
                {environments.map(env => (
                  <option key={env} value={env}>{env}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label className="form-label">Services Filter (optional)</label>
              <div className="services-filter">
                {availableServices.map(service => (
                  <label key={service} className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={eodForm.servicesFilter.includes(service)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setEodForm({
                            ...eodForm,
                            servicesFilter: [...eodForm.servicesFilter, service]
                          });
                        } else {
                          setEodForm({
                            ...eodForm,
                            servicesFilter: eodForm.servicesFilter.filter(s => s !== service)
                          });
                        }
                      }}
                    />
                    {service}
                  </label>
                ))}
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Cutoff Time (optional)</label>
              <input
                type="datetime-local"
                className="form-control"
                value={eodForm.cutoffTime}
                onChange={(e) => setEodForm({ ...eodForm, cutoffTime: e.target.value })}
              />
            </div>

            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={eodForm.dryRun}
                  onChange={(e) => setEodForm({ ...eodForm, dryRun: e.target.checked })}
                />
                Dry Run (simulation only)
              </label>
            </div>

            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={eodForm.forceExecution}
                  onChange={(e) => setEodForm({ ...eodForm, forceExecution: e.target.checked })}
                />
                Force Execution
              </label>
            </div>

            <div className="form-group">
              <label className="form-label">Comments</label>
              <textarea
                className="form-control"
                rows={3}
                value={eodForm.comments}
                onChange={(e) => setEodForm({ ...eodForm, comments: e.target.value })}
                placeholder="Enter any comments about this operation..."
              />
            </div>

            <button type="submit" className="btn btn-primary" disabled={loading}>
              {loading ? '⏳ Starting...' : '⏹️ Start EOD Operation'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};

export default SODEODOperations;