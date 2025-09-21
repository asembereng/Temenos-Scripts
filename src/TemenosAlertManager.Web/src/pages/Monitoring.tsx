import React, { useState, useEffect } from 'react';
import apiService from '../services/apiService';
import { Alert, SystemMetrics, HealthSummary } from '../types';
import './Dashboard.css';

interface MonitoringData {
  systemMetrics: SystemMetrics;
  alerts: Alert[];
  healthSummaries: HealthSummary[];
  performanceBaselines: any;
  performanceTrends: any;
}

const Monitoring: React.FC = () => {
  const [monitoringData, setMonitoringData] = useState<MonitoringData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'metrics' | 'alerts' | 'health' | 'trends'>('metrics');
  const [alertFilter, setAlertFilter] = useState<'all' | 'critical' | 'warning' | 'info'>('all');

  useEffect(() => {
    loadMonitoringData();
    // Refresh data every 10 seconds for real-time monitoring
    const interval = setInterval(loadMonitoringData, 10000);
    return () => clearInterval(interval);
  }, []);

  const loadMonitoringData = async () => {
    try {
      setError(null);
      const [dashboardData, performanceBaselines, performanceTrends] = await Promise.all([
        apiService.getDashboard(),
        apiService.getPerformanceBaselines().catch(() => ({})),
        apiService.getPerformanceTrends().catch(() => ({}))
      ]);

      setMonitoringData({
        systemMetrics: dashboardData.systemMetrics,
        alerts: dashboardData.recentAlerts || [],
        healthSummaries: dashboardData.domainSummaries || [],
        performanceBaselines,
        performanceTrends
      });
    } catch (err) {
      console.error('Failed to load monitoring data:', err);
      setError('Failed to load monitoring data. Please check your connection and try again.');
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

  const getSeverityClassName = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'info':
        return 'severity-info';
      case 'warning':
        return 'severity-warning';
      case 'critical':
        return 'severity-critical';
      default:
        return 'severity-unknown';
    }
  };

  const formatMetricValue = (value: number, unit?: string) => {
    if (unit === '%') {
      return `${Math.round(value)}%`;
    }
    if (value > 1000) {
      return `${(value / 1000).toFixed(1)}k`;
    }
    return value.toFixed(1);
  };

  const filteredAlerts = monitoringData?.alerts.filter(alert => {
    if (alertFilter === 'all') return true;
    return alert.severity.toLowerCase() === alertFilter;
  }) || [];

  if (loading) {
    return <div className="loading">Loading monitoring dashboard...</div>;
  }

  if (error) {
    return (
      <div className="monitoring">
        <div className="error">{error}</div>
        <button className="btn btn-primary" onClick={loadMonitoringData}>
          Retry
        </button>
      </div>
    );
  }

  if (!monitoringData) {
    return <div className="error">No monitoring data available</div>;
  }

  return (
    <div className="monitoring">
      <div className="page-header">
        <h1>Real-time Monitoring Dashboard</h1>
        <div className="dashboard-actions">
          <button className="btn btn-secondary" onClick={loadMonitoringData}>
            🔄 Refresh
          </button>
          <span className="last-updated">
            Last updated: {new Date().toLocaleString()}
          </span>
        </div>
      </div>

      {/* Navigation Tabs */}
      <div className="tab-navigation">
        <button 
          className={`tab-button ${activeTab === 'metrics' ? 'active' : ''}`}
          onClick={() => setActiveTab('metrics')}
        >
          Live Metrics
        </button>
        <button 
          className={`tab-button ${activeTab === 'alerts' ? 'active' : ''}`}
          onClick={() => setActiveTab('alerts')}
        >
          Alert Monitoring
        </button>
        <button 
          className={`tab-button ${activeTab === 'health' ? 'active' : ''}`}
          onClick={() => setActiveTab('health')}
        >
          System Health
        </button>
        <button 
          className={`tab-button ${activeTab === 'trends' ? 'active' : ''}`}
          onClick={() => setActiveTab('trends')}
        >
          Trend Analysis
        </button>
      </div>

      {/* Live Performance Metrics */}
      {activeTab === 'metrics' && (
        <div className="monitoring-section">
          <h2>Live Performance Metrics</h2>
          <div className="grid grid-4">
            <div className="metric-card">
              <h3>CPU Usage</h3>
              <div className="metric-value">
                {formatMetricValue(monitoringData.systemMetrics.cpuUtilization, '%')}
              </div>
              <div className={`metric-status ${monitoringData.systemMetrics.cpuUtilization > 80 ? 'critical' : 'healthy'}`}>
                {monitoringData.systemMetrics.cpuUtilization > 80 ? '⚠️ High' : '✅ Normal'}
              </div>
            </div>
            <div className="metric-card">
              <h3>Memory Usage</h3>
              <div className="metric-value">
                {formatMetricValue(monitoringData.systemMetrics.memoryUtilization, '%')}
              </div>
              <div className={`metric-status ${monitoringData.systemMetrics.memoryUtilization > 80 ? 'critical' : 'healthy'}`}>
                {monitoringData.systemMetrics.memoryUtilization > 80 ? '⚠️ High' : '✅ Normal'}
              </div>
            </div>
            <div className="metric-card">
              <h3>Disk Usage</h3>
              <div className="metric-value">
                {formatMetricValue(monitoringData.systemMetrics.diskUtilization, '%')}
              </div>
              <div className={`metric-status ${monitoringData.systemMetrics.diskUtilization > 85 ? 'critical' : 'healthy'}`}>
                {monitoringData.systemMetrics.diskUtilization > 85 ? '⚠️ High' : '✅ Normal'}
              </div>
            </div>
            <div className="metric-card">
              <h3>Response Time</h3>
              <div className="metric-value">
                {formatMetricValue(monitoringData.systemMetrics.responseTime)}ms
              </div>
              <div className={`metric-status ${monitoringData.systemMetrics.responseTime > 1000 ? 'critical' : 'healthy'}`}>
                {monitoringData.systemMetrics.responseTime > 1000 ? '⚠️ Slow' : '✅ Fast'}
              </div>
            </div>
          </div>

          <div className="grid grid-2" style={{ marginTop: '20px' }}>
            <div className="metric-card">
              <h3>Network Utilization</h3>
              <div className="metric-value">
                {formatMetricValue(monitoringData.systemMetrics.networkUtilization, '%')}
              </div>
            </div>
            <div className="metric-card">
              <h3>Active Connections</h3>
              <div className="metric-value">
                {monitoringData.systemMetrics.activeConnections}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Alert Monitoring */}
      {activeTab === 'alerts' && (
        <div className="monitoring-section">
          <div className="section-header">
            <h2>Alert Monitoring</h2>
            <div className="alert-filters">
              <button 
                className={`filter-btn ${alertFilter === 'all' ? 'active' : ''}`}
                onClick={() => setAlertFilter('all')}
              >
                All ({monitoringData.alerts.length})
              </button>
              <button 
                className={`filter-btn ${alertFilter === 'critical' ? 'active' : ''}`}
                onClick={() => setAlertFilter('critical')}
              >
                Critical ({monitoringData.alerts.filter(a => a.severity.toLowerCase() === 'critical').length})
              </button>
              <button 
                className={`filter-btn ${alertFilter === 'warning' ? 'active' : ''}`}
                onClick={() => setAlertFilter('warning')}
              >
                Warning ({monitoringData.alerts.filter(a => a.severity.toLowerCase() === 'warning').length})
              </button>
              <button 
                className={`filter-btn ${alertFilter === 'info' ? 'active' : ''}`}
                onClick={() => setAlertFilter('info')}
              >
                Info ({monitoringData.alerts.filter(a => a.severity.toLowerCase() === 'info').length})
              </button>
            </div>
          </div>

          <div className="alerts-list">
            {filteredAlerts.length === 0 ? (
              <div className="no-alerts">
                <p>No {alertFilter === 'all' ? '' : alertFilter} alerts found</p>
              </div>
            ) : (
              filteredAlerts.map((alert) => (
                <div key={alert.id} className={`alert-item ${getSeverityClassName(alert.severity)}`}>
                  <div className="alert-header">
                    <h4>{alert.title}</h4>
                    <span className={`alert-severity ${getSeverityClassName(alert.severity)}`}>
                      {alert.severity}
                    </span>
                  </div>
                  <p className="alert-description">{alert.description}</p>
                  <div className="alert-meta">
                    <span>Domain: {alert.domain}</span>
                    <span>Source: {alert.source}</span>
                    <span>Created: {new Date(alert.createdAt).toLocaleString()}</span>
                  </div>
                  {alert.metricValue && (
                    <div className="alert-metrics">
                      <span>Value: {alert.metricValue}</span>
                      {alert.threshold && <span>Threshold: {alert.threshold}</span>}
                    </div>
                  )}
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* System Health */}
      {activeTab === 'health' && (
        <div className="monitoring-section">
          <h2>System Health Overview</h2>
          <div className="health-overview">
            {monitoringData.healthSummaries.map((summary) => (
              <div key={summary.domain} className="health-card">
                <div className="health-header">
                  <h3>{summary.domain}</h3>
                  <span className={`health-status ${getStatusClassName(summary.overallStatus)}`}>
                    {summary.overallStatus}
                  </span>
                </div>
                <div className="health-metrics">
                  <div className="health-metric">
                    <span className="metric-label">Active Alerts:</span>
                    <span className="metric-value">{summary.activeAlerts}</span>
                  </div>
                  <div className="health-metric">
                    <span className="metric-label">Critical:</span>
                    <span className="metric-value critical">{summary.criticalAlerts}</span>
                  </div>
                  <div className="health-metric">
                    <span className="metric-label">Warning:</span>
                    <span className="metric-value warning">{summary.warningAlerts}</span>
                  </div>
                  <div className="health-metric">
                    <span className="metric-label">Last Checked:</span>
                    <span className="metric-value">{new Date(summary.lastChecked).toLocaleString()}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Historical Trends */}
      {activeTab === 'trends' && (
        <div className="monitoring-section">
          <h2>Historical Trend Analysis</h2>
          <div className="trends-content">
            <div className="card">
              <div className="card-header">
                <h3>Performance Trends</h3>
              </div>
              <div className="card-body">
                <p>Historical performance data and trend analysis will be displayed here.</p>
                <div className="trend-placeholder">
                  <div className="trend-chart">
                    📈 Response Time Trends
                  </div>
                  <div className="trend-chart">
                    📊 Throughput Analysis
                  </div>
                  <div className="trend-chart">
                    ⚠️ Error Rate Trends
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Monitoring;