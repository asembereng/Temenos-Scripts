import React, { useState, useEffect } from 'react';
import apiService from '../services/apiService';
import { DashboardData, Alert, SystemMetrics, OperationResult } from '../types';
import './Dashboard.css';

const Dashboard: React.FC = () => {
  const [dashboardData, setDashboardData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadDashboardData();
    // Refresh data every 30 seconds
    const interval = setInterval(loadDashboardData, 30000);
    return () => clearInterval(interval);
  }, []);

  const loadDashboardData = async () => {
    try {
      setError(null);
      const data = await apiService.getDashboard();
      setDashboardData(data);
    } catch (err) {
      console.error('Failed to load dashboard data:', err);
      setError('Failed to load dashboard data. Please check your connection and try again.');
    } finally {
      setLoading(false);
    }
  };

  const getStatusClassName = (status: string) => {
    switch (status.toLowerCase()) {
      case 'success':
      case 'healthy':
      case 'completed':
        return 'status-healthy';
      case 'warning':
      case 'running':
        return 'status-warning';
      case 'critical':
      case 'error':
      case 'failed':
        return 'status-critical';
      default:
        return 'status-unknown';
    }
  };

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const formatPercentage = (value: number) => {
    return `${Math.round(value)}%`;
  };

  if (loading) {
    return <div className="loading">Loading dashboard...</div>;
  }

  if (error) {
    return (
      <div>
        <div className="error">{error}</div>
        <button className="btn btn-primary" onClick={loadDashboardData}>
          Retry
        </button>
      </div>
    );
  }

  if (!dashboardData) {
    return <div className="error">No dashboard data available</div>;
  }

  return (
    <div className="dashboard">
      <div className="dashboard-header">
        <h1>System Dashboard</h1>
        <div className="dashboard-actions">
          <button className="btn btn-secondary" onClick={loadDashboardData}>
            🔄 Refresh
          </button>
          <span className="last-updated">
            Last updated: {new Date().toLocaleString()}
          </span>
        </div>
      </div>

      {/* System Health Overview */}
      <div className="grid grid-4">
        {dashboardData.domainSummaries?.map((summary) => (
          <div key={summary.domain} className="card">
            <div className="card-header">
              <h3 className="card-title">{summary.domain}</h3>
              <span className={`status-badge ${getStatusClassName(summary.overallStatus)}`}>
                {summary.overallStatus}
              </span>
            </div>
            <div className="domain-metrics">
              <div className="metric">
                <span className="metric-label">Active Alerts:</span>
                <span className="metric-value">{summary.activeAlerts}</span>
              </div>
              <div className="metric">
                <span className="metric-label">Critical:</span>
                <span className="metric-value critical">{summary.criticalAlerts}</span>
              </div>
              <div className="metric">
                <span className="metric-label">Warning:</span>
                <span className="metric-value warning">{summary.warningAlerts}</span>
              </div>
              <div className="metric-small">
                Last checked: {formatDateTime(summary.lastChecked)}
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="grid grid-2">
        {/* System Metrics */}
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">System Metrics</h3>
          </div>
          {dashboardData.systemMetrics && (
            <div className="system-metrics">
              <div className="metric-row">
                <span className="metric-label">CPU Utilization:</span>
                <div className="metric-progress">
                  <div className="progress">
                    <div 
                      className={`progress-bar ${dashboardData.systemMetrics.cpuUtilization > 80 ? 'danger' : dashboardData.systemMetrics.cpuUtilization > 60 ? 'warning' : 'success'}`}
                      style={{ width: `${dashboardData.systemMetrics.cpuUtilization}%` }}
                    >
                      {formatPercentage(dashboardData.systemMetrics.cpuUtilization)}
                    </div>
                  </div>
                </div>
              </div>
              <div className="metric-row">
                <span className="metric-label">Memory Utilization:</span>
                <div className="metric-progress">
                  <div className="progress">
                    <div 
                      className={`progress-bar ${dashboardData.systemMetrics.memoryUtilization > 85 ? 'danger' : dashboardData.systemMetrics.memoryUtilization > 70 ? 'warning' : 'success'}`}
                      style={{ width: `${dashboardData.systemMetrics.memoryUtilization}%` }}
                    >
                      {formatPercentage(dashboardData.systemMetrics.memoryUtilization)}
                    </div>
                  </div>
                </div>
              </div>
              <div className="metric-row">
                <span className="metric-label">Disk Utilization:</span>
                <div className="metric-progress">
                  <div className="progress">
                    <div 
                      className={`progress-bar ${dashboardData.systemMetrics.diskUtilization > 90 ? 'danger' : dashboardData.systemMetrics.diskUtilization > 75 ? 'warning' : 'success'}`}
                      style={{ width: `${dashboardData.systemMetrics.diskUtilization}%` }}
                    >
                      {formatPercentage(dashboardData.systemMetrics.diskUtilization)}
                    </div>
                  </div>
                </div>
              </div>
              <div className="metric-grid">
                <div className="metric">
                  <span className="metric-label">Active Connections:</span>
                  <span className="metric-value">{dashboardData.systemMetrics.activeConnections}</span>
                </div>
                <div className="metric">
                  <span className="metric-label">Response Time:</span>
                  <span className="metric-value">{Math.round(dashboardData.systemMetrics.responseTime)}ms</span>
                </div>
                <div className="metric">
                  <span className="metric-label">Throughput:</span>
                  <span className="metric-value">{Math.round(dashboardData.systemMetrics.throughput)}/s</span>
                </div>
                <div className="metric">
                  <span className="metric-label">Error Rate:</span>
                  <span className="metric-value">{formatPercentage(dashboardData.systemMetrics.errorRate)}</span>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Recent Alerts */}
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">Recent Alerts</h3>
          </div>
          <div className="recent-alerts">
            {dashboardData.recentAlerts?.length > 0 ? (
              dashboardData.recentAlerts.slice(0, 5).map((alert) => (
                <div key={alert.id} className="alert-item">
                  <div className="alert-header">
                    <span className={`status-badge ${getStatusClassName(alert.severity)}`}>
                      {alert.severity}
                    </span>
                    <span className="alert-time">{formatDateTime(alert.createdAt)}</span>
                  </div>
                  <div className="alert-title">{alert.title}</div>
                  <div className="alert-source">{alert.domain} - {alert.source}</div>
                </div>
              ))
            ) : (
              <div className="no-alerts">No recent alerts</div>
            )}
          </div>
        </div>
      </div>

      {/* Active Operations */}
      {dashboardData.activeOperations?.length > 0 && (
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">Active Operations</h3>
          </div>
          <div className="active-operations">
            {dashboardData.activeOperations.map((operation) => (
              <div key={operation.operationId} className="operation-item">
                <div className="operation-header">
                  <h4 className="operation-title">{operation.operationId}</h4>
                  <span className={`status-badge ${getStatusClassName(operation.status)}`}>
                    {operation.status}
                  </span>
                </div>
                <div className="operation-progress">
                  <div className="progress">
                    <div 
                      className="progress-bar"
                      style={{ width: `${operation.progressPercentage}%` }}
                    >
                      {formatPercentage(operation.progressPercentage)}
                    </div>
                  </div>
                  <div className="operation-step">{operation.currentStep}</div>
                </div>
                <div className="operation-time">
                  Started: {formatDateTime(operation.startTime)}
                  {operation.estimatedDuration && (
                    <span> • ETA: {operation.estimatedDuration}</span>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default Dashboard;