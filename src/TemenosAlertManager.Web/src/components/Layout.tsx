import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import './Layout.css';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const location = useLocation();

  const navigationItems = [
    { path: '/dashboard', label: 'Dashboard', icon: '📊' },
    { path: '/services', label: 'Service Management', icon: '⚙️' },
    { path: '/operations', label: 'SOD/EOD Operations', icon: '🔄' },
    { path: '/monitoring', label: 'Monitoring', icon: '📈' },
    { path: '/reports', label: 'Reports', icon: '📋' },
    { path: '/user-management', label: 'User Management', icon: '👥' },
  ];

  const isActivePath = (path: string) => {
    return location.pathname === path;
  };

  return (
    <div className="layout">
      {/* Header */}
      <header className="header">
        <div className="header-left">
          <button 
            className="sidebar-toggle"
            onClick={() => setSidebarOpen(!sidebarOpen)}
          >
            ☰
          </button>
          <h1 className="header-title">Temenos Service Management Platform</h1>
        </div>
        <div className="header-right">
          <div className="user-info">
            <span className="user-name">Administrator</span>
            <div className="user-avatar">👤</div>
          </div>
        </div>
      </header>

      {/* Sidebar */}
      <aside className={`sidebar ${sidebarOpen ? 'sidebar-open' : 'sidebar-closed'}`}>
        <nav className="sidebar-nav">
          {navigationItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className={`nav-item ${isActivePath(item.path) ? 'nav-item-active' : ''}`}
            >
              <span className="nav-icon">{item.icon}</span>
              {sidebarOpen && <span className="nav-label">{item.label}</span>}
            </Link>
          ))}
        </nav>
        
        <div className="sidebar-footer">
          <div className="external-links">
            <a 
              href="/swagger" 
              target="_blank" 
              rel="noopener noreferrer"
              className="external-link"
              title="Swagger API Documentation"
            >
              <span className="nav-icon">📚</span>
              {sidebarOpen && <span className="nav-label">API Docs</span>}
            </a>
            <a 
              href="/hangfire" 
              target="_blank" 
              rel="noopener noreferrer"
              className="external-link"
              title="Hangfire Dashboard"
            >
              <span className="nav-icon">⚡</span>
              {sidebarOpen && <span className="nav-label">Jobs</span>}
            </a>
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <main className={`main-content ${sidebarOpen ? 'main-content-shifted' : ''}`}>
        <div className="content-wrapper">
          {children}
        </div>
      </main>
    </div>
  );
};

export default Layout;