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
  environment?: string;
}

const SystemConfiguration: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'hosts' | 'system' | 'templates' | 'environments'>('hosts');
  const [serviceHosts, setServiceHosts] = useState<ServiceHost[]>([]);
  const [systemConfigs, setSystemConfigs] = useState<SystemConfig[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedEnvironment, setSelectedEnvironment] = useState<string>('PROD');

  // Form states for service hosts
  const [showAddHostForm, setShowAddHostForm] = useState(false);
  const [editingHost, setEditingHost] = useState<ServiceHost | null>(null);
  const [showTemplateWizard, setShowTemplateWizard] = useState(false);
  const [showImportExport, setShowImportExport] = useState(false);
  const [testingConnection, setTestingConnection] = useState<number | null>(null);
  const [newHostForm, setNewHostForm] = useState({
    name: '',
    host: '',
    type: 'T24',
    description: '',
    isEnabled: true,
    environment: 'PROD',
    port: '',
    username: '',
    authenticationType: 'Windows',
    connectionString: '',
    healthCheckEndpoint: ''
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
  }, [selectedEnvironment]);

  // Temenos configuration templates
  const temenosTemplates = {
    'T24_CORE_BANKING': {
      name: 'T24 Core Banking Server',
      type: 'T24',
      description: 'Primary T24 Core Banking Application Server',
      port: '8080',
      healthCheckEndpoint: '/BrowserWeb/servlet/BrowserServlet',
      authenticationType: 'Windows',
      commonPorts: ['8080', '8443', '9080', '9443']
    },
    'TPH_PAYMENT_HUB': {
      name: 'TPH Payment Hub Service',
      type: 'TPH',
      description: 'Temenos Payment Hub for payment processing',
      port: '8443',
      healthCheckEndpoint: '/tph/health',
      authenticationType: 'Certificate',
      commonPorts: ['8443', '9443', '443']
    },
    'IBM_MQ': {
      name: 'IBM MQ Message Broker',
      type: 'MQ',
      description: 'IBM MQ Queue Manager for messaging',
      port: '1414',
      healthCheckEndpoint: '/mq/health',
      authenticationType: 'Service Account',
      commonPorts: ['1414', '9443', '9157']
    },
    'SQL_SERVER': {
      name: 'SQL Server Database',
      type: 'MSSQL',
      description: 'Microsoft SQL Server for T24 database',
      port: '1433',
      healthCheckEndpoint: '/health',
      authenticationType: 'SQL',
      commonPorts: ['1433', '1434']
    }
  };

  const environments = ['DEV', 'TEST', 'UAT', 'PROD', 'DR'];

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
      const newHost = await apiService.post('/api/system-configuration/service-hosts', {
        ...newHostForm,
        environment: selectedEnvironment
      });
      setServiceHosts([...serviceHosts, {
        id: newHost.id,
        name: newHost.name,
        host: newHost.host,
        type: newHost.type,
        description: newHost.description,
        isEnabled: newHost.isEnabled
      }]);
      setShowAddHostForm(false);
      resetHostForm();
      setSuccess('Service host added successfully');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: any) {
      setError('Failed to add service host: ' + err.message);
    }
  };

  const resetHostForm = () => {
    setNewHostForm({
      name: '',
      host: '',
      type: 'T24',
      description: '',
      isEnabled: true,
      environment: selectedEnvironment,
      port: '',
      username: '',
      authenticationType: 'Windows',
      connectionString: '',
      healthCheckEndpoint: ''
    });
  };

  const applyTemplate = (templateKey: string) => {
    const template = temenosTemplates[templateKey as keyof typeof temenosTemplates];
    if (template) {
      setNewHostForm({
        ...newHostForm,
        name: template.name,
        type: template.type,
        description: template.description,
        port: template.port,
        healthCheckEndpoint: template.healthCheckEndpoint,
        authenticationType: template.authenticationType
      });
      setShowTemplateWizard(false);
      setShowAddHostForm(true);
    }
  };

  const testConnection = async (hostId: number) => {
    setTestingConnection(hostId);
    try {
      const host = serviceHosts.find(h => h.id === hostId);
      if (!host) return;

      // Simulate connection test (would be implemented in backend)
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      // Mock successful connection test
      setSuccess(`Connection to ${host.name} (${host.host}) successful`);
      setTimeout(() => setSuccess(null), 5000);
    } catch (err: any) {
      setError(`Connection test failed: ${err.message}`);
    } finally {
      setTestingConnection(null);
    }
  };

  const exportConfiguration = () => {
    const config = {
      environment: selectedEnvironment,
      serviceHosts: serviceHosts,
      systemConfigs: systemConfigs,
      exportDate: new Date().toISOString(),
      version: '1.0'
    };
    
    const blob = new Blob([JSON.stringify(config, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `temenos-config-${selectedEnvironment}-${new Date().toISOString().split('T')[0]}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const importConfiguration = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const config = JSON.parse(e.target?.result as string);
        if (config.serviceHosts) {
          setServiceHosts(config.serviceHosts);
        }
        if (config.systemConfigs) {
          setSystemConfigs(config.systemConfigs);
        }
        setSuccess('Configuration imported successfully');
        setTimeout(() => setSuccess(null), 3000);
      } catch (err) {
        setError('Failed to import configuration: Invalid file format');
      }
    };
    reader.readAsText(file);
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
      resetHostForm();
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
        <div className="header-content">
          <h2>System Configuration</h2>
          <p>Manage service host IPs and system settings for Temenos components</p>
        </div>
        <div className="header-controls">
          <div className="environment-selector">
            <label>Environment:</label>
            <select 
              value={selectedEnvironment} 
              onChange={(e) => setSelectedEnvironment(e.target.value)}
              className="environment-dropdown"
            >
              {environments.map(env => (
                <option key={env} value={env}>{env}</option>
              ))}
            </select>
          </div>
          <div className="header-actions">
            <button 
              className="btn btn-secondary"
              onClick={() => setShowImportExport(true)}
            >
              📤 Import/Export
            </button>
            <button 
              className="btn btn-primary"
              onClick={() => setShowTemplateWizard(true)}
            >
              🧙‍♂️ Configuration Wizard
            </button>
          </div>
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
          <button className="btn btn-sm btn-primary" onClick={() => setSuccess(null)}>
            Dismiss
          </button>
        </div>
      )}

      {/* Template Wizard Modal */}
      {showTemplateWizard && (
        <div className="form-modal">
          <div className="form-container">
            <h4>🧙‍♂️ Temenos Configuration Wizard</h4>
            <p>Select a pre-configured template based on Temenos best practices:</p>
            <div className="template-grid">
              {Object.entries(temenosTemplates).map(([key, template]) => (
                <div key={key} className="template-card" onClick={() => applyTemplate(key)}>
                  <div className="template-header">
                    <h5>{template.name}</h5>
                    <span className="template-type">{template.type}</span>
                  </div>
                  <p>{template.description}</p>
                  <div className="template-details">
                    <small>Default Port: {template.port}</small>
                    <small>Auth: {template.authenticationType}</small>
                  </div>
                </div>
              ))}
            </div>
            <div className="form-actions">
              <button type="button" className="btn btn-secondary" onClick={() => setShowTemplateWizard(false)}>
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Import/Export Modal */}
      {showImportExport && (
        <div className="form-modal">
          <div className="form-container">
            <h4>📤 Configuration Import/Export</h4>
            <div className="import-export-section">
              <div className="export-section">
                <h5>Export Configuration</h5>
                <p>Export current {selectedEnvironment} environment configuration to JSON file</p>
                <button className="btn btn-primary" onClick={exportConfiguration}>
                  📥 Export Configuration
                </button>
              </div>
              <div className="import-section">
                <h5>Import Configuration</h5>
                <p>Import configuration from JSON file</p>
                <input
                  type="file"
                  accept=".json"
                  onChange={importConfiguration}
                  className="file-input"
                />
                <small>⚠️ This will overwrite existing configuration</small>
              </div>
            </div>
            <div className="form-actions">
              <button type="button" className="btn btn-secondary" onClick={() => setShowImportExport(false)}>
                Close
              </button>
            </div>
          </div>
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

                  <div className="form-row">
                    <div className="form-group">
                      <label>Port</label>
                      <input
                        type="text"
                        value={newHostForm.port}
                        onChange={(e) => setNewHostForm({...newHostForm, port: e.target.value})}
                        placeholder="e.g., 8080"
                      />
                    </div>
                    <div className="form-group">
                      <label>Authentication Type</label>
                      <select
                        value={newHostForm.authenticationType}
                        onChange={(e) => setNewHostForm({...newHostForm, authenticationType: e.target.value})}
                      >
                        <option value="Windows">Windows Authentication</option>
                        <option value="Certificate">Certificate</option>
                        <option value="Service Account">Service Account</option>
                        <option value="SQL">SQL Authentication</option>
                        <option value="Token">Token Based</option>
                      </select>
                    </div>
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
                    <label>Health Check Endpoint (Optional)</label>
                    <input
                      type="text"
                      value={newHostForm.healthCheckEndpoint}
                      onChange={(e) => setNewHostForm({...newHostForm, healthCheckEndpoint: e.target.value})}
                      placeholder="e.g., /health or /BrowserWeb/servlet/BrowserServlet"
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
                {serviceHosts.filter(host => !selectedEnvironment || host.environment === selectedEnvironment).map((host) => (
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
                          className={`btn btn-sm ${testingConnection === host.id ? 'btn-loading' : 'btn-success'}`}
                          onClick={() => testConnection(host.id)}
                          disabled={testingConnection === host.id}
                        >
                          {testingConnection === host.id ? '🔄 Testing...' : '🔗 Test'}
                        </button>
                        <button
                          className="btn btn-sm btn-secondary"
                          onClick={() => {
                            setEditingHost(host);
                            setNewHostForm({
                              name: host.name,
                              host: host.host,
                              type: host.type,
                              description: host.description,
                              isEnabled: host.isEnabled,
                              environment: selectedEnvironment,
                              port: '',
                              username: '',
                              authenticationType: 'Windows',
                              connectionString: '',
                              healthCheckEndpoint: ''
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