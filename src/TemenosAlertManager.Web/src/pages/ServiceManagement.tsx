import React, { useState, useEffect } from 'react';
import apiService from '../services/apiService';
import { ServiceStatus, ServiceAction } from '../types';
import './ServiceManagement.css';

const ServiceManagement: React.FC = () => {
  const [services, setServices] = useState<ServiceStatus[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  useEffect(() => {
    loadServices();
    // Refresh services every 30 seconds
    const interval = setInterval(loadServices, 30000);
    return () => clearInterval(interval);
  }, []);

  const loadServices = async () => {
    try {
      setError(null);
      const data = await apiService.getServiceStatus();
      setServices(data);
    } catch (err) {
      console.error('Failed to load services:', err);
      setError('Failed to load services. Please check your connection and try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleServiceAction = async (serviceId: number, action: string) => {
    const actionKey = `${serviceId}-${action}`;
    setActionLoading(actionKey);
    
    try {
      await apiService.performServiceAction({ serviceId, action } as ServiceAction);
      // Refresh services after action
      await loadServices();
    } catch (err) {
      console.error(`Failed to ${action} service:`, err);
      setError(`Failed to ${action} service. Please try again.`);
    } finally {
      setActionLoading(null);
    }
  };

  const getStatusClassName = (status: string) => {
    switch (status.toLowerCase()) {
      case 'healthy':
        return 'status-healthy';
      case 'warning':
        return 'status-warning';
      case 'critical':
        return 'status-critical';
      default:
        return 'status-unknown';
    }
  };

  const formatLastChecked = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    return date.toLocaleDateString();
  };

  if (loading) {
    return <div className="loading">Loading services...</div>;
  }

  return (
    <div className="service-management">
      <div className="page-header">
        <h1>Service Management</h1>
        <div className="page-actions">
          <button className="btn btn-secondary" onClick={loadServices}>
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

      <div className="services-overview">
        <div className="overview-stats">
          <div className="stat-card">
            <div className="stat-value">{services.length}</div>
            <div className="stat-label">Total Services</div>
          </div>
          <div className="stat-card">
            <div className="stat-value">{services.filter(s => s.status === 'Healthy').length}</div>
            <div className="stat-label">Healthy</div>
          </div>
          <div className="stat-card">
            <div className="stat-value">{services.filter(s => s.status === 'Warning').length}</div>
            <div className="stat-label">Warning</div>
          </div>
          <div className="stat-card">
            <div className="stat-value">{services.filter(s => s.status === 'Critical').length}</div>
            <div className="stat-label">Critical</div>
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <h3 className="card-title">Services</h3>
          <div className="services-filter">
            <select className="form-control">
              <option value="">All Types</option>
              <option value="TPH">TPH</option>
              <option value="T24">T24</option>
              <option value="MQ">MQ</option>
              <option value="MSSQL">MSSQL</option>
              <option value="Host">Host</option>
              <option value="JVM">JVM</option>
            </select>
          </div>
        </div>

        {services.length > 0 ? (
          <div className="services-table-container">
            <table className="table services-table">
              <thead>
                <tr>
                  <th>Service Name</th>
                  <th>Host</th>
                  <th>Type</th>
                  <th>Status</th>
                  <th>Last Checked</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {services.map((service) => (
                  <tr key={service.id}>
                    <td>
                      <div className="service-name">
                        <strong>{service.name}</strong>
                      </div>
                    </td>
                    <td>{service.host}</td>
                    <td>
                      <span className="service-type">{service.type}</span>
                    </td>
                    <td>
                      <span className={`status-badge ${getStatusClassName(service.status)}`}>
                        {service.status}
                      </span>
                    </td>
                    <td>
                      <span className="last-checked">
                        {formatLastChecked(service.lastChecked)}
                      </span>
                    </td>
                    <td>
                      <div className="service-actions">
                        {service.canStart && (
                          <button
                            className="btn btn-sm btn-success"
                            onClick={() => handleServiceAction(service.id, 'start')}
                            disabled={actionLoading === `${service.id}-start`}
                          >
                            {actionLoading === `${service.id}-start` ? '⏳' : '▶️'} Start
                          </button>
                        )}
                        {service.canStop && (
                          <button
                            className="btn btn-sm btn-danger"
                            onClick={() => handleServiceAction(service.id, 'stop')}
                            disabled={actionLoading === `${service.id}-stop`}
                          >
                            {actionLoading === `${service.id}-stop` ? '⏳' : '⏸️'} Stop
                          </button>
                        )}
                        {service.canRestart && (
                          <button
                            className="btn btn-sm btn-warning"
                            onClick={() => handleServiceAction(service.id, 'restart')}
                            disabled={actionLoading === `${service.id}-restart`}
                          >
                            {actionLoading === `${service.id}-restart` ? '⏳' : '🔄'} Restart
                          </button>
                        )}
                        <button
                          className="btn btn-sm btn-secondary"
                          onClick={() => handleServiceAction(service.id, 'healthcheck')}
                          disabled={actionLoading === `${service.id}-healthcheck`}
                        >
                          {actionLoading === `${service.id}-healthcheck` ? '⏳' : '🔍'} Check
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="no-services">
            <p>No services found.</p>
            <button className="btn btn-primary" onClick={loadServices}>
              Refresh Services
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default ServiceManagement;