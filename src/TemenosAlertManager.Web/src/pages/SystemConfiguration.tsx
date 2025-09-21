import React, { useState, useEffect } from 'react';
import { apiService } from '../services/apiService';
import './SystemConfiguration.css';

interface SystemConfig {
  id: number;
  key: string;
  value: string;
  category: string;
  description: string;
  isEncrypted: boolean;
  createdAt: string;
  updatedAt: string;
}

interface ServiceHost {
  id: number;
  name: string;
  host: string;
  type: string;
  description: string;
  isEnabled: boolean;
}

const SystemConfiguration: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'hosts' | 'system'>('hosts');
  const [serviceHosts, setServiceHosts] = useState<ServiceHost[]>([]);
  const [systemConfigs, setSystemConfigs] = useState<SystemConfig[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Form states for service hosts
  const [showAddHostForm, setShowAddHostForm] = useState(false);
  const [editingHost, setEditingHost] = useState<ServiceHost | null>(null);
  const [newHostForm, setNewHostForm] = useState({
    name: '',
    host: '',
    type: 'T24',
    description: '',
    isEnabled: true
  });

  // Form states for system config
  const [showAddConfigForm, setShowAddConfigForm] = useState(false);
  const [editingConfig, setEditingConfig] = useState<SystemConfig | null>(null);
  const [newConfigForm, setNewConfigForm] = useState({
    key: '',
    value: '',
    category: 'Core Banking',
    description: '',
    isEncrypted: false
  });

  useEffect(() => {
    loadServiceHosts();
    loadSystemConfigs();
  }, []);

  const loadServiceHosts = async () => {
    try {
      setLoading(true);
      const hosts = await apiService.get('/api/system-configuration/service-hosts');
      setServiceHosts(hosts.map((host: any) => ({
        id: host.id,
        name: host.name,
        host: host.host,
        type: host.type,
        description: host.description,
        isEnabled: host.isEnabled
      })));
    } catch (err: any) {
      setError('Failed to load service hosts: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const loadSystemConfigs = async () => {
    try {
      setLoading(true);
      const configs = await apiService.get('/api/system-configuration/system-configs');
      setSystemConfigs(configs.map((config: any) => ({
        id: config.id,
        key: config.key,
        value: config.value,
        category: config.category,
        description: config.description,
        isEncrypted: config.isEncrypted,
        createdAt: config.createdAt,
        updatedAt: config.updatedAt
      })));
    } catch (err: any) {
      setError('Failed to load system configurations: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleAddHost = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const newHost = await apiService.post('/api/system-configuration/service-hosts', newHostForm);
      setServiceHosts([...serviceHosts, {
        id: newHost.id,
        name: newHost.name,
        host: newHost.host,
        type: newHost.type,
        description: newHost.description,
        isEnabled: newHost.isEnabled
      }]);
      setShowAddHostForm(false);
      setNewHostForm({ name: '', host: '', type: 'T24', description: '', isEnabled: true });
      setSuccess('Service host added successfully');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: any) {
      setError('Failed to add service host: ' + err.message);
    }
  };

  const handleUpdateHost = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingHost) return;
    
    try {
      const updatedHost = await apiService.put(`/api/system-configuration/service-hosts/${editingHost.id}`, newHostForm);
      const updatedHosts = serviceHosts.map(host => 
        host.id === editingHost.id 
          ? { id: updatedHost.id, name: updatedHost.name, host: updatedHost.host, type: updatedHost.type, description: updatedHost.description, isEnabled: updatedHost.isEnabled }
          : host
      );
      setServiceHosts(updatedHosts);
      setEditingHost(null);
      setNewHostForm({ name: '', host: '', type: 'T24', description: '', isEnabled: true });
      setSuccess('Service host updated successfully');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: any) {
      setError('Failed to update service host: ' + err.message);
    }
  };

  const handleDeleteHost = async (hostId: number) => {
    if (!confirm('Are you sure you want to delete this service host?')) return;
    
    try {
      await apiService.delete(`/api/system-configuration/service-hosts/${hostId}`);
      setServiceHosts(serviceHosts.filter(host => host.id !== hostId));
      setSuccess('Service host deleted successfully');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: any) {
      setError('Failed to delete service host: ' + err.message);
    }
  };

  const handleAddConfig = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const newConfig = await apiService.post('/api/system-configuration/system-configs', newConfigForm);
      setSystemConfigs([...systemConfigs, {
        id: newConfig.id,
        key: newConfig.key,
        value: newConfig.value,
        category: newConfig.category,
        description: newConfig.description,
        isEncrypted: newConfig.isEncrypted,
        createdAt: newConfig.createdAt,
        updatedAt: newConfig.updatedAt
      }]);
      setShowAddConfigForm(false);
      setNewConfigForm({ key: '', value: '', category: 'Core Banking', description: '', isEncrypted: false });
      setSuccess('System configuration added successfully');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: any) {
      setError('Failed to add system configuration: ' + err.message);
    }
  };

  const formatTimeAgo = (dateString: string) => {
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

  if (loading && serviceHosts.length === 0 && systemConfigs.length === 0) {
    return <div className="loading">Loading configuration...</div>;
  }

  return (
    <div className="system-configuration">
      <div className="config-header">
        <h2>System Configuration</h2>
        <p>Manage service host IPs and system settings for Temenos components</p>
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
          <button className="btn btn-sm btn-primary" onClick={() => setSuccess(null)}>
            Dismiss
          </button>
        </div>
      )}

      <div className="config-tabs">
        <button
          className={`tab-button ${activeTab === 'hosts' ? 'tab-active' : ''}`}
          onClick={() => setActiveTab('hosts')}
        >
          Service Hosts
        </button>
        <button
          className={`tab-button ${activeTab === 'system' ? 'tab-active' : ''}`}
          onClick={() => setActiveTab('system')}
        >
          System Settings
        </button>
      </div>

      {activeTab === 'hosts' && (
        <div className="hosts-section">
          <div className="section-header">
            <h3>Service Host Configuration</h3>
            <button 
              className="btn btn-primary"
              onClick={() => setShowAddHostForm(true)}
            >
              + Add Service Host
            </button>
          </div>

          {showAddHostForm && (
            <div className="form-modal">
              <div className="form-container">
                <h4>Add Service Host</h4>
                <form onSubmit={handleAddHost}>
                  <div className="form-group">
                    <label>Service Name</label>
                    <input
                      type="text"
                      value={newHostForm.name}
                      onChange={(e) => setNewHostForm({...newHostForm, name: e.target.value})}
                      placeholder="e.g., T24 Core Banking Server"
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label>Host IP/FQDN</label>
                    <input
                      type="text"
                      value={newHostForm.host}
                      onChange={(e) => setNewHostForm({...newHostForm, host: e.target.value})}
                      placeholder="e.g., 192.168.1.100 or t24-server.bank.local"
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label>Service Type</label>
                    <select
                      value={newHostForm.type}
                      onChange={(e) => setNewHostForm({...newHostForm, type: e.target.value})}
                    >
                      <option value="T24">T24 Core Banking</option>
                      <option value="TPH">TPH Payment Hub</option>
                      <option value="MQ">Message Queue</option>
                      <option value="MSSQL">SQL Server</option>
                      <option value="Host">Host Server</option>
                      <option value="JVM">JVM Service</option>
                    </select>
                  </div>

                  <div className="form-group">
                    <label>Description</label>
                    <textarea
                      value={newHostForm.description}
                      onChange={(e) => setNewHostForm({...newHostForm, description: e.target.value})}
                      placeholder="Brief description of this service"
                      rows={3}
                    />
                  </div>

                  <div className="form-group">
                    <label className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={newHostForm.isEnabled}
                        onChange={(e) => setNewHostForm({...newHostForm, isEnabled: e.target.checked})}
                      />
                      Enabled
                    </label>
                  </div>

                  <div className="form-actions">
                    <button type="button" className="btn btn-secondary" onClick={() => setShowAddHostForm(false)}>
                      Cancel
                    </button>
                    <button type="submit" className="btn btn-primary">
                      Add Host
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}

          <div className="hosts-table-container">
            <table className="table hosts-table">
              <thead>
                <tr>
                  <th>Service Name</th>
                  <th>Host</th>
                  <th>Type</th>
                  <th>Status</th>
                  <th>Description</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {serviceHosts.map((host) => (
                  <tr key={host.id}>
                    <td>
                      <div className="host-name">
                        <strong>{host.name}</strong>
                      </div>
                    </td>
                    <td>
                      <code className="host-address">{host.host}</code>
                    </td>
                    <td>
                      <span className={`type-badge type-${host.type.toLowerCase()}`}>
                        {host.type}
                      </span>
                    </td>
                    <td>
                      <span className={`status-badge ${host.isEnabled ? 'status-enabled' : 'status-disabled'}`}>
                        {host.isEnabled ? 'Enabled' : 'Disabled'}
                      </span>
                    </td>
                    <td>
                      <div className="host-description">
                        {host.description}
                      </div>
                    </td>
                    <td>
                      <div className="host-actions">
                        <button
                          className="btn btn-sm btn-secondary"
                          onClick={() => {
                            setEditingHost(host);
                            setNewHostForm({
                              name: host.name,
                              host: host.host,
                              type: host.type,
                              description: host.description,
                              isEnabled: host.isEnabled
                            });
                          }}
                        >
                          Edit
                        </button>
                        <button
                          className="btn btn-sm btn-danger"
                          onClick={() => handleDeleteHost(host.id)}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab === 'system' && (
        <div className="system-section">
          <div className="section-header">
            <h3>System Settings</h3>
            <button 
              className="btn btn-primary"
              onClick={() => setShowAddConfigForm(true)}
            >
              + Add Configuration
            </button>
          </div>

          {showAddConfigForm && (
            <div className="form-modal">
              <div className="form-container">
                <h4>Add System Configuration</h4>
                <form onSubmit={handleAddConfig}>
                  <div className="form-group">
                    <label>Configuration Key</label>
                    <input
                      type="text"
                      value={newConfigForm.key}
                      onChange={(e) => setNewConfigForm({...newConfigForm, key: e.target.value})}
                      placeholder="e.g., core.banking.endpoint"
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label>Value</label>
                    <input
                      type="text"
                      value={newConfigForm.value}
                      onChange={(e) => setNewConfigForm({...newConfigForm, value: e.target.value})}
                      placeholder="Configuration value"
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label>Category</label>
                    <select
                      value={newConfigForm.category}
                      onChange={(e) => setNewConfigForm({...newConfigForm, category: e.target.value})}
                    >
                      <option value="Core Banking">Core Banking</option>
                      <option value="Payment Hub">Payment Hub</option>
                      <option value="Messaging">Messaging</option>
                      <option value="Database">Database</option>
                      <option value="Security">Security</option>
                      <option value="Monitoring">Monitoring</option>
                    </select>
                  </div>

                  <div className="form-group">
                    <label>Description</label>
                    <textarea
                      value={newConfigForm.description}
                      onChange={(e) => setNewConfigForm({...newConfigForm, description: e.target.value})}
                      placeholder="Describe this configuration setting"
                      rows={3}
                    />
                  </div>

                  <div className="form-group">
                    <label className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={newConfigForm.isEncrypted}
                        onChange={(e) => setNewConfigForm({...newConfigForm, isEncrypted: e.target.checked})}
                      />
                      Encrypted Value
                    </label>
                  </div>

                  <div className="form-actions">
                    <button type="button" className="btn btn-secondary" onClick={() => setShowAddConfigForm(false)}>
                      Cancel
                    </button>
                    <button type="submit" className="btn btn-primary">
                      Add Configuration
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}

          <div className="configs-table-container">
            <table className="table configs-table">
              <thead>
                <tr>
                  <th>Key</th>
                  <th>Value</th>
                  <th>Category</th>
                  <th>Description</th>
                  <th>Last Updated</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {systemConfigs.map((config) => (
                  <tr key={config.id}>
                    <td>
                      <code className="config-key">{config.key}</code>
                    </td>
                    <td>
                      <div className="config-value">
                        {config.isEncrypted ? '••••••••' : config.value}
                        {config.isEncrypted && <span className="encrypted-badge">🔐</span>}
                      </div>
                    </td>
                    <td>
                      <span className={`category-badge category-${config.category.toLowerCase().replace(/\s+/g, '-')}`}>
                        {config.category}
                      </span>
                    </td>
                    <td>
                      <div className="config-description">
                        {config.description}
                      </div>
                    </td>
                    <td>
                      <span className="time-ago">
                        {formatTimeAgo(config.updatedAt)}
                      </span>
                    </td>
                    <td>
                      <div className="config-actions">
                        <button
                          className="btn btn-sm btn-secondary"
                          onClick={() => {
                            setEditingConfig(config);
                            setNewConfigForm({
                              key: config.key,
                              value: config.value,
                              category: config.category,
                              description: config.description,
                              isEncrypted: config.isEncrypted
                            });
                          }}
                        >
                          Edit
                        </button>
                        <button
                          className="btn btn-sm btn-danger"
                          onClick={() => {
                            if (confirm('Are you sure you want to delete this configuration?')) {
                              setSystemConfigs(systemConfigs.filter(c => c.id !== config.id));
                              setSuccess('Configuration deleted successfully');
                              setTimeout(() => setSuccess(null), 3000);
                            }
                          }}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};

export default SystemConfiguration;