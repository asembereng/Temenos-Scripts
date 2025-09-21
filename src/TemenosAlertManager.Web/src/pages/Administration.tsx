import React, { useState } from 'react';
import { Link, useLocation, Routes, Route, Navigate } from 'react-router-dom';
import UserManagement from './UserManagement';
import SystemConfiguration from './SystemConfiguration';
import './Administration.css';

const Administration: React.FC = () => {
  const location = useLocation();

  const isActivePath = (path: string) => {
    return location.pathname === path;
  };

  return (
    <div className="administration">
      <div className="page-header">
        <h1>Administration</h1>
        <p>Manage system settings, user accounts, and configurations</p>
      </div>

      <div className="admin-layout">
        <div className="admin-sidebar">
          <nav className="admin-nav">
            <Link
              to="/administration/system-config"
              className={`admin-nav-item ${isActivePath('/administration/system-config') ? 'admin-nav-item-active' : ''}`}
            >
              <span className="admin-nav-icon">🔧</span>
              <span className="admin-nav-label">System Configuration</span>
            </Link>
            <Link
              to="/administration/user-management"
              className={`admin-nav-item ${isActivePath('/administration/user-management') ? 'admin-nav-item-active' : ''}`}
            >
              <span className="admin-nav-icon">👥</span>
              <span className="admin-nav-label">User Management</span>
            </Link>
          </nav>
        </div>

        <div className="admin-content">
          <Routes>
            <Route path="/" element={<Navigate to="/administration/system-config" replace />} />
            <Route path="/system-config" element={<SystemConfiguration />} />
            <Route path="/user-management" element={<UserManagement />} />
          </Routes>
        </div>
      </div>
    </div>
  );
};

export default Administration;