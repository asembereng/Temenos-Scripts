import React, { useState, useEffect } from 'react';
import { apiService } from '../services/apiService';
import './UserManagement.css';

interface AuthConfig {
  id: number;
  adGroupName: string;
  role: 'Viewer' | 'Operator' | 'Admin';
  isEnabled: boolean;
  description?: string;
  createdAt: string;
  updatedAt: string;
}

interface ADConfiguration {
  authenticationType: string;
  domain: string;
  serverAddress: string;
  baseDN: string;
  serviceAccount: string;
  isEnabled: boolean;
  useSSL: boolean;
  port: number;
}

interface ADGroupInfo {
  name: string;
  description: string;
  distinguishedName: string;
}

interface UserRoles {
  userPrincipalName: string;
  displayName: string;
  roles: string[];
  isActive: boolean;
}

const UserManagement: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'groups' | 'config' | 'users'>('groups');
  const [authConfigs, setAuthConfigs] = useState<AuthConfig[]>([]);
  const [adConfig, setADConfig] = useState<ADConfiguration | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchResults, setSearchResults] = useState<ADGroupInfo[]>([]);
  const [userLookup, setUserLookup] = useState<UserRoles | null>(null);

  // Form states
  const [showAddGroupForm, setShowAddGroupForm] = useState(false);
  const [editingConfig, setEditingConfig] = useState<AuthConfig | null>(null);
  const [newGroupForm, setNewGroupForm] = useState({
    adGroupName: '',
    role: 'Viewer' as 'Viewer' | 'Operator' | 'Admin',
    description: '',
    isEnabled: true
  });
  const [searchTerm, setSearchTerm] = useState('');
  const [userSearchTerm, setUserSearchTerm] = useState('');

  useEffect(() => {
    loadAuthConfigs();
    loadADConfiguration();
  }, []);

  const loadAuthConfigs = async () => {
    try {
      setLoading(true);
      const configs = await apiService.get('/api/user-management/auth-configs');
      setAuthConfigs(configs);
    } catch (err: any) {
      setError('Failed to load authentication configurations: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const loadADConfiguration = async () => {
    try {
      const config = await apiService.get('/api/user-management/ad-config');
      setADConfig(config);
    } catch (err: any) {
      setError('Failed to load AD configuration: ' + err.message);
    }
  };

  const handleAddGroup = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setLoading(true);
      await apiService.post('/api/user-management/auth-configs', newGroupForm);
      setNewGroupForm({
        adGroupName: '',
        role: 'Viewer',
        description: '',
        isEnabled: true
      });
      setShowAddGroupForm(false);
      await loadAuthConfigs();
    } catch (err: any) {
      setError('Failed to add group configuration: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateGroup = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingConfig) return;

    try {
      setLoading(true);
      await apiService.put(`/api/user-management/auth-configs/${editingConfig.id}`, {
        adGroupName: editingConfig.adGroupName,
        role: editingConfig.role,
        description: editingConfig.description,
        isEnabled: editingConfig.isEnabled
      });
      setEditingConfig(null);
      await loadAuthConfigs();
    } catch (err: any) {
      setError('Failed to update group configuration: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteGroup = async (id: number) => {
    if (!confirm('Are you sure you want to delete this group configuration?')) return;

    try {
      setLoading(true);
      await apiService.delete(`/api/user-management/auth-configs/${id}`);
      await loadAuthConfigs();
    } catch (err: any) {
      setError('Failed to delete group configuration: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleTestGroup = async (groupName: string) => {
    try {
      setLoading(true);
      const result = await apiService.post('/api/user-management/test-ad-connection', { groupName });
      if (result.isConnectionSuccessful && result.groupExists) {
        alert('✅ Group validation successful! Group exists and is accessible.');
      } else {
        alert(`❌ Group validation failed: ${result.errorMessage || 'Group not found or not accessible'}`);
      }
    } catch (err: any) {
      alert('❌ Group validation failed: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSearchGroups = async () => {
    if (!searchTerm.trim()) return;

    try {
      setLoading(true);
      const results = await apiService.get(`/api/user-management/search-groups?searchTerm=${encodeURIComponent(searchTerm)}`);
      setSearchResults(results);
    } catch (err: any) {
      setError('Failed to search groups: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleUserLookup = async () => {
    if (!userSearchTerm.trim()) return;

    try {
      setLoading(true);
      const result = await apiService.get(`/api/user-management/users/${encodeURIComponent(userSearchTerm)}/roles`);
      setUserLookup(result);
    } catch (err: any) {
      setError('Failed to lookup user roles: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateADConfig = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!adConfig) return;

    try {
      setLoading(true);
      const updatedConfig = await apiService.put('/api/user-management/ad-config', adConfig);
      setADConfig(updatedConfig);
      alert('✅ AD configuration updated successfully!');
    } catch (err: any) {
      setError('Failed to update AD configuration: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const getRoleBadgeClass = (role: string) => {
    switch (role) {
      case 'Admin': return 'role-badge admin';
      case 'Operator': return 'role-badge operator';
      case 'Viewer': return 'role-badge viewer';
      default: return 'role-badge';
    }
  };

  return (
    <div className="user-management">
      <div className="page-header">
        <h1>👥 User Management</h1>
        <p>Configure Active Directory integration and manage user roles</p>
      </div>

      {error && (
        <div className="error-banner">
          <span>{error}</span>
          <button onClick={() => setError(null)}>Dismiss</button>
        </div>
      )}

      <div className="tab-navigation">
        <button 
          className={activeTab === 'groups' ? 'tab-button active' : 'tab-button'}
          onClick={() => setActiveTab('groups')}
        >
          🔐 Group Mappings
        </button>
        <button 
          className={activeTab === 'config' ? 'tab-button active' : 'tab-button'}
          onClick={() => setActiveTab('config')}
        >
          ⚙️ AD Configuration
        </button>
        <button 
          className={activeTab === 'users' ? 'tab-button active' : 'tab-button'}
          onClick={() => setActiveTab('users')}
        >
          👤 User Lookup
        </button>
      </div>

      {activeTab === 'groups' && (
        <div className="tab-content">
          <div className="section-header">
            <h2>Active Directory Group to Role Mappings</h2>
            <button 
              className="btn btn-primary"
              onClick={() => setShowAddGroupForm(true)}
            >
              ➕ Add Group Mapping
            </button>
          </div>

          {showAddGroupForm && (
            <div className="form-modal">
              <div className="modal-content">
                <div className="modal-header">
                  <h3>Add Group Mapping</h3>
                  <button onClick={() => setShowAddGroupForm(false)}>✕</button>
                </div>
                <form onSubmit={handleAddGroup}>
                  <div className="form-group">
                    <label>AD Group Name</label>
                    <input
                      type="text"
                      value={newGroupForm.adGroupName}
                      onChange={(e) => setNewGroupForm({...newGroupForm, adGroupName: e.target.value})}
                      placeholder="Enter AD group name (e.g., TEMENOS_ADMINS)"
                      required
                    />
                  </div>
                  <div className="form-group">
                    <label>Role</label>
                    <select
                      value={newGroupForm.role}
                      onChange={(e) => setNewGroupForm({...newGroupForm, role: e.target.value as any})}
                    >
                      <option value="Viewer">Viewer (Read-only)</option>
                      <option value="Operator">Operator (Operations)</option>
                      <option value="Admin">Admin (Full access)</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label>Description</label>
                    <input
                      type="text"
                      value={newGroupForm.description}
                      onChange={(e) => setNewGroupForm({...newGroupForm, description: e.target.value})}
                      placeholder="Optional description"
                    />
                  </div>
                  <div className="form-group checkbox-group">
                    <label>
                      <input
                        type="checkbox"
                        checked={newGroupForm.isEnabled}
                        onChange={(e) => setNewGroupForm({...newGroupForm, isEnabled: e.target.checked})}
                      />
                      Enabled
                    </label>
                  </div>
                  <div className="form-actions">
                    <button type="button" onClick={() => setShowAddGroupForm(false)}>Cancel</button>
                    <button type="submit" disabled={loading}>
                      {loading ? 'Adding...' : 'Add Group'}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}

          <div className="groups-table">
            <table>
              <thead>
                <tr>
                  <th>AD Group Name</th>
                  <th>Role</th>
                  <th>Status</th>
                  <th>Description</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {authConfigs.map((config) => (
                  <tr key={config.id}>
                    <td>{config.adGroupName}</td>
                    <td>
                      <span className={getRoleBadgeClass(config.role)}>
                        {config.role}
                      </span>
                    </td>
                    <td>
                      <span className={`status-badge ${config.isEnabled ? 'enabled' : 'disabled'}`}>
                        {config.isEnabled ? 'Enabled' : 'Disabled'}
                      </span>
                    </td>
                    <td>{config.description || '-'}</td>
                    <td className="actions">
                      <button 
                        className="btn btn-sm"
                        onClick={() => handleTestGroup(config.adGroupName)}
                        disabled={loading}
                      >
                        🧪 Test
                      </button>
                      <button 
                        className="btn btn-sm"
                        onClick={() => setEditingConfig(config)}
                      >
                        ✏️ Edit
                      </button>
                      <button 
                        className="btn btn-sm btn-danger"
                        onClick={() => handleDeleteGroup(config.id)}
                        disabled={loading}
                      >
                        🗑️ Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {authConfigs.length === 0 && (
              <div className="empty-state">
                <p>No group mappings configured yet.</p>
                <p>Add your first group mapping to get started with user management.</p>
              </div>
            )}
          </div>

          {editingConfig && (
            <div className="form-modal">
              <div className="modal-content">
                <div className="modal-header">
                  <h3>Edit Group Mapping</h3>
                  <button onClick={() => setEditingConfig(null)}>✕</button>
                </div>
                <form onSubmit={handleUpdateGroup}>
                  <div className="form-group">
                    <label>AD Group Name</label>
                    <input
                      type="text"
                      value={editingConfig.adGroupName}
                      onChange={(e) => setEditingConfig({...editingConfig, adGroupName: e.target.value})}
                      required
                    />
                  </div>
                  <div className="form-group">
                    <label>Role</label>
                    <select
                      value={editingConfig.role}
                      onChange={(e) => setEditingConfig({...editingConfig, role: e.target.value as any})}
                    >
                      <option value="Viewer">Viewer (Read-only)</option>
                      <option value="Operator">Operator (Operations)</option>
                      <option value="Admin">Admin (Full access)</option>
                    </select>
                  </div>
                  <div className="form-group">
                    <label>Description</label>
                    <input
                      type="text"
                      value={editingConfig.description || ''}
                      onChange={(e) => setEditingConfig({...editingConfig, description: e.target.value})}
                    />
                  </div>
                  <div className="form-group checkbox-group">
                    <label>
                      <input
                        type="checkbox"
                        checked={editingConfig.isEnabled}
                        onChange={(e) => setEditingConfig({...editingConfig, isEnabled: e.target.checked})}
                      />
                      Enabled
                    </label>
                  </div>
                  <div className="form-actions">
                    <button type="button" onClick={() => setEditingConfig(null)}>Cancel</button>
                    <button type="submit" disabled={loading}>
                      {loading ? 'Updating...' : 'Update Group'}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}
        </div>
      )}

      {activeTab === 'config' && adConfig && (
        <div className="tab-content">
          <div className="section-header">
            <h2>Active Directory Configuration</h2>
            <p>Configure connection settings for Windows AD or Azure AD</p>
          </div>

          <form onSubmit={handleUpdateADConfig} className="ad-config-form">
            <div className="form-group">
              <label>Authentication Type</label>
              <select
                value={adConfig.authenticationType}
                onChange={(e) => setADConfig({...adConfig, authenticationType: e.target.value})}
              >
                <option value="WindowsAuthentication">Windows Authentication</option>
                <option value="AzureAD">Azure Active Directory</option>
              </select>
            </div>

            <div className="form-group">
              <label>Domain</label>
              <input
                type="text"
                value={adConfig.domain}
                onChange={(e) => setADConfig({...adConfig, domain: e.target.value})}
                placeholder="company.local or company.onmicrosoft.com"
              />
            </div>

            <div className="form-group">
              <label>Server Address</label>
              <input
                type="text"
                value={adConfig.serverAddress}
                onChange={(e) => setADConfig({...adConfig, serverAddress: e.target.value})}
                placeholder="dc01.company.local or graph.microsoft.com"
              />
            </div>

            <div className="form-group">
              <label>Base DN</label>
              <input
                type="text"
                value={adConfig.baseDN}
                onChange={(e) => setADConfig({...adConfig, baseDN: e.target.value})}
                placeholder="DC=company,DC=local"
              />
            </div>

            <div className="form-group">
              <label>Service Account</label>
              <input
                type="text"
                value={adConfig.serviceAccount}
                onChange={(e) => setADConfig({...adConfig, serviceAccount: e.target.value})}
                placeholder="serviceaccount@company.local"
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Port</label>
                <input
                  type="number"
                  value={adConfig.port}
                  onChange={(e) => setADConfig({...adConfig, port: parseInt(e.target.value) || 389})}
                  min="1"
                  max="65535"
                />
              </div>
              <div className="form-group checkbox-group">
                <label>
                  <input
                    type="checkbox"
                    checked={adConfig.useSSL}
                    onChange={(e) => setADConfig({...adConfig, useSSL: e.target.checked})}
                  />
                  Use SSL
                </label>
              </div>
              <div className="form-group checkbox-group">
                <label>
                  <input
                    type="checkbox"
                    checked={adConfig.isEnabled}
                    onChange={(e) => setADConfig({...adConfig, isEnabled: e.target.checked})}
                  />
                  Enabled
                </label>
              </div>
            </div>

            <div className="form-actions">
              <button type="submit" disabled={loading}>
                {loading ? 'Updating...' : 'Update Configuration'}
              </button>
            </div>
          </form>
        </div>
      )}

      {activeTab === 'users' && (
        <div className="tab-content">
          <div className="section-header">
            <h2>User Role Lookup</h2>
            <p>Search for users and view their assigned roles</p>
          </div>

          <div className="search-section">
            <div className="search-group">
              <input
                type="text"
                value={userSearchTerm}
                onChange={(e) => setUserSearchTerm(e.target.value)}
                placeholder="Enter user principal name (e.g., john.doe@company.com)"
                onKeyPress={(e) => e.key === 'Enter' && handleUserLookup()}
              />
              <button onClick={handleUserLookup} disabled={loading}>
                {loading ? 'Searching...' : '🔍 Lookup User'}
              </button>
            </div>

            {userLookup && (
              <div className="user-result">
                <h3>User Information</h3>
                <div className="user-details">
                  <div className="detail-row">
                    <strong>User Principal Name:</strong> {userLookup.userPrincipalName}
                  </div>
                  <div className="detail-row">
                    <strong>Display Name:</strong> {userLookup.displayName}
                  </div>
                  <div className="detail-row">
                    <strong>Status:</strong> 
                    <span className={`status-badge ${userLookup.isActive ? 'enabled' : 'disabled'}`}>
                      {userLookup.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </div>
                  <div className="detail-row">
                    <strong>Assigned Roles:</strong>
                    <div className="roles-list">
                      {userLookup.roles.map((role, index) => (
                        <span key={index} className={getRoleBadgeClass(role)}>
                          {role}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>

          <div className="group-search-section">
            <h3>Search AD Groups</h3>
            <div className="search-group">
              <input
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Search for AD groups..."
                onKeyPress={(e) => e.key === 'Enter' && handleSearchGroups()}
              />
              <button onClick={handleSearchGroups} disabled={loading}>
                {loading ? 'Searching...' : '🔍 Search Groups'}
              </button>
            </div>

            {searchResults.length > 0 && (
              <div className="search-results">
                <h4>Search Results</h4>
                <div className="groups-list">
                  {searchResults.map((group, index) => (
                    <div key={index} className="group-item">
                      <div className="group-name">{group.name}</div>
                      <div className="group-description">{group.description || 'No description'}</div>
                      <button 
                        className="btn btn-sm"
                        onClick={() => setNewGroupForm({...newGroupForm, adGroupName: group.name})}
                      >
                        Use in Mapping
                      </button>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default UserManagement;