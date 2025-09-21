import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Dashboard from './pages/Dashboard';
import ServiceManagement from './pages/ServiceManagement';
import SODEODOperations from './pages/SODEODOperations';
import Monitoring from './pages/Monitoring';
import Reports from './pages/Reports';
import Administration from './pages/Administration';
import './App.css';

const App: React.FC = () => {
  return (
    <Router>
      <div className="app">
        <Layout>
          <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/services" element={<ServiceManagement />} />
            <Route path="/operations" element={<SODEODOperations />} />
            <Route path="/monitoring" element={<Monitoring />} />
            <Route path="/reports" element={<Reports />} />
            <Route path="/administration/*" element={<Administration />} />
            {/* Legacy route for backward compatibility */}
            <Route path="/user-management" element={<Navigate to="/administration/user-management" replace />} />
          </Routes>
        </Layout>
      </div>
    </Router>
  );
};

export default App;