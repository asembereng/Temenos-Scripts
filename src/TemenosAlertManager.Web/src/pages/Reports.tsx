import React, { useState, useEffect } from 'react';
import apiService from '../services/apiService';
import './Dashboard.css';

interface ReportConfig {
  reportType: 'operations' | 'performance' | 'compliance' | 'custom';
  title: string;
  description: string;
  format: 'pdf' | 'excel' | 'csv';
  environment?: string;
  dateRange: {
    startDate: string;
    endDate: string;
  };
  parameters?: Record<string, any>;
}

interface AvailableReport {
  id: string;
  name: string;
  description: string;
  format: string;
  createdAt: string;
  size?: string;
  status: 'ready' | 'generating' | 'failed';
}

const Reports: React.FC = () => {
  const [availableReports, setAvailableReports] = useState<AvailableReport[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'generate' | 'scheduled' | 'history'>('generate');
  const [reportConfig, setReportConfig] = useState<ReportConfig>({
    reportType: 'operations',
    title: 'Operations Summary Report',
    description: 'Comprehensive operations summary for the selected period',
    format: 'pdf',
    dateRange: {
      startDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      endDate: new Date().toISOString().split('T')[0]
    }
  });
  const [isGenerating, setIsGenerating] = useState(false);

  useEffect(() => {
    loadAvailableReports();
  }, []);

  const loadAvailableReports = async () => {
    try {
      setError(null);
      const reports = await apiService.getReports();
      // Transform API response to match our interface
      const transformedReports = Array.isArray(reports) ? reports.map((report: any, index: number) => ({
        id: report.id || `report-${index}`,
        name: report.name || report.reportType || 'Untitled Report',
        description: report.description || 'No description available',
        format: report.format || 'pdf',
        createdAt: report.createdAt || new Date().toISOString(),
        size: report.size || 'Unknown',
        status: report.status || 'ready'
      })) : [];
      setAvailableReports(transformedReports);
    } catch (err) {
      console.error('Failed to load reports:', err);
      setError('Failed to load available reports.');
      setAvailableReports([]);
    }
  };

  const handleReportGeneration = async () => {
    setIsGenerating(true);
    try {
      setError(null);
      await apiService.generateReport(reportConfig);
      
      // Refresh the reports list
      await loadAvailableReports();
      
      // Reset form or show success message
      alert('Report generation started successfully!');
    } catch (err) {
      console.error('Failed to generate report:', err);
      setError('Failed to start report generation. Please try again.');
    } finally {
      setIsGenerating(false);
    }
  };

  const handleDownloadReport = async (reportId: string) => {
    try {
      const blob = await apiService.downloadReport(reportId);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `report-${reportId}.${reportConfig.format}`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Failed to download report:', err);
      alert('Failed to download report. Please try again.');
    }
  };

  const updateReportConfig = (field: string, value: any) => {
    setReportConfig(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const updateDateRange = (field: 'startDate' | 'endDate', value: string) => {
    setReportConfig(prev => ({
      ...prev,
      dateRange: {
        ...prev.dateRange,
        [field]: value
      }
    }));
  };

  const reportTypes = [
    { 
      value: 'operations', 
      label: 'Operations Summary', 
      description: 'SOD/EOD operations, success rates, and performance metrics' 
    },
    { 
      value: 'performance', 
      label: 'Performance Analytics', 
      description: 'System performance trends, bottlenecks, and optimization insights' 
    },
    { 
      value: 'compliance', 
      label: 'Compliance Report', 
      description: 'Audit trails, security events, and regulatory compliance data' 
    },
    { 
      value: 'custom', 
      label: 'Custom Report', 
      description: 'Build a custom report with specific metrics and filters' 
    }
  ];

  const formatOptions = [
    { value: 'pdf', label: 'PDF Document' },
    { value: 'excel', label: 'Excel Spreadsheet' },
    { value: 'csv', label: 'CSV Data File' }
  ];

  return (
    <div className="reports">
      <div className="page-header">
        <h1>Report Generation</h1>
        <div className="dashboard-actions">
          <button className="btn btn-secondary" onClick={loadAvailableReports}>
            🔄 Refresh
          </button>
        </div>
      </div>

      {error && (
        <div className="error-banner">
          {error}
        </div>
      )}

      {/* Navigation Tabs */}
      <div className="tab-navigation">
        <button 
          className={`tab-button ${activeTab === 'generate' ? 'active' : ''}`}
          onClick={() => setActiveTab('generate')}
        >
          Generate Report
        </button>
        <button 
          className={`tab-button ${activeTab === 'scheduled' ? 'active' : ''}`}
          onClick={() => setActiveTab('scheduled')}
        >
          Scheduled Reports
        </button>
        <button 
          className={`tab-button ${activeTab === 'history' ? 'active' : ''}`}
          onClick={() => setActiveTab('history')}
        >
          Report History
        </button>
      </div>

      {/* Generate Report Tab */}
      {activeTab === 'generate' && (
        <div className="reports-section">
          <h2>Generate New Report</h2>
          <div className="report-form">
            <div className="grid grid-2">
              {/* Report Type Selection */}
              <div className="form-group">
                <label htmlFor="reportType">Report Type</label>
                <select 
                  id="reportType"
                  value={reportConfig.reportType}
                  onChange={(e) => updateReportConfig('reportType', e.target.value)}
                  className="form-control"
                >
                  {reportTypes.map(type => (
                    <option key={type.value} value={type.value}>
                      {type.label}
                    </option>
                  ))}
                </select>
                <div className="form-help">
                  {reportTypes.find(t => t.value === reportConfig.reportType)?.description}
                </div>
              </div>

              {/* Export Format */}
              <div className="form-group">
                <label htmlFor="format">Export Format</label>
                <select 
                  id="format"
                  value={reportConfig.format}
                  onChange={(e) => updateReportConfig('format', e.target.value)}
                  className="form-control"
                >
                  {formatOptions.map(format => (
                    <option key={format.value} value={format.value}>
                      {format.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="grid grid-2">
              {/* Date Range */}
              <div className="form-group">
                <label htmlFor="startDate">Start Date</label>
                <input 
                  type="date"
                  id="startDate"
                  value={reportConfig.dateRange.startDate}
                  onChange={(e) => updateDateRange('startDate', e.target.value)}
                  className="form-control"
                />
              </div>

              <div className="form-group">
                <label htmlFor="endDate">End Date</label>
                <input 
                  type="date"
                  id="endDate"
                  value={reportConfig.dateRange.endDate}
                  onChange={(e) => updateDateRange('endDate', e.target.value)}
                  className="form-control"
                />
              </div>
            </div>

            {/* Title and Description */}
            <div className="form-group">
              <label htmlFor="title">Report Title</label>
              <input 
                type="text"
                id="title"
                value={reportConfig.title}
                onChange={(e) => updateReportConfig('title', e.target.value)}
                className="form-control"
                placeholder="Enter report title"
              />
            </div>

            <div className="form-group">
              <label htmlFor="description">Description</label>
              <textarea 
                id="description"
                value={reportConfig.description}
                onChange={(e) => updateReportConfig('description', e.target.value)}
                className="form-control"
                rows={3}
                placeholder="Enter report description"
              />
            </div>

            {/* Environment Filter (optional) */}
            <div className="form-group">
              <label htmlFor="environment">Environment (Optional)</label>
              <select 
                id="environment"
                value={reportConfig.environment || ''}
                onChange={(e) => updateReportConfig('environment', e.target.value || undefined)}
                className="form-control"
              >
                <option value="">All Environments</option>
                <option value="production">Production</option>
                <option value="staging">Staging</option>
                <option value="testing">Testing</option>
                <option value="development">Development</option>
              </select>
            </div>

            {/* Generate Button */}
            <div className="form-actions">
              <button 
                className="btn btn-primary"
                onClick={handleReportGeneration}
                disabled={isGenerating}
              >
                {isGenerating ? '⏳ Generating...' : '📄 Generate Report'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Scheduled Reports Tab */}
      {activeTab === 'scheduled' && (
        <div className="reports-section">
          <h2>Scheduled Reports</h2>
          <div className="scheduled-reports">
            <div className="placeholder-content">
              <h3>📅 Automated Report Scheduling</h3>
              <p>Set up automated report generation with customizable schedules.</p>
              <div className="feature-list">
                <div className="feature-item">
                  <strong>Daily/Weekly/Monthly Reports:</strong> Configure regular report generation
                </div>
                <div className="feature-item">
                  <strong>Email Distribution:</strong> Automatically send reports to stakeholders
                </div>
                <div className="feature-item">
                  <strong>Custom Templates:</strong> Create reusable report templates
                </div>
                <div className="feature-item">
                  <strong>Conditional Triggers:</strong> Generate reports based on system events
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Report History Tab */}
      {activeTab === 'history' && (
        <div className="reports-section">
          <h2>Report History</h2>
          <div className="reports-list">
            {loading ? (
              <div className="loading">Loading reports...</div>
            ) : availableReports.length === 0 ? (
              <div className="no-reports">
                <h3>📋 No Reports Available</h3>
                <p>Generate your first report using the "Generate Report" tab.</p>
              </div>
            ) : (
              <div className="reports-grid">
                {availableReports.map((report) => (
                  <div key={report.id} className="report-item">
                    <div className="report-header">
                      <h4>{report.name}</h4>
                      <span className={`report-status ${report.status}`}>
                        {report.status}
                      </span>
                    </div>
                    <p className="report-description">{report.description}</p>
                    <div className="report-meta">
                      <span>Format: {report.format.toUpperCase()}</span>
                      <span>Created: {new Date(report.createdAt).toLocaleDateString()}</span>
                      {report.size && <span>Size: {report.size}</span>}
                    </div>
                    <div className="report-actions">
                      {report.status === 'ready' && (
                        <button 
                          className="btn btn-sm btn-primary"
                          onClick={() => handleDownloadReport(report.id)}
                        >
                          📥 Download
                        </button>
                      )}
                      {report.status === 'generating' && (
                        <span className="generating-indicator">⏳ Generating...</span>
                      )}
                      {report.status === 'failed' && (
                        <span className="failed-indicator">❌ Failed</span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default Reports;