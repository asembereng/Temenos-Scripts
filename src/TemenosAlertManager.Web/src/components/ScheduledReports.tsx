import React, { useState, useEffect } from 'react';
import apiService from '../services/apiService';

interface ScheduledReport {
  id: string;
  name: string;
  reportType: string;
  schedule: string;
  format: string;
  recipients: string[];
  isActive: boolean;
  lastRun?: string;
  nextRun: string;
}

interface ScheduledReportsProps {
  onScheduleReport: () => void;
}

const ScheduledReports: React.FC<ScheduledReportsProps> = ({ onScheduleReport }) => {
  const [scheduledReports, setScheduledReports] = useState<ScheduledReport[]>([]);
  const [loading, setLoading] = useState(true);
  const [showScheduleForm, setShowScheduleForm] = useState(false);
  const [scheduleForm, setScheduleForm] = useState({
    name: '',
    reportType: 'operations',
    schedule: 'daily',
    format: 'pdf',
    recipients: '',
    isActive: true
  });

  useEffect(() => {
    loadScheduledReports();
  }, []);

  const loadScheduledReports = async () => {
    try {
      // Mock data for demonstration
      const mockScheduledReports: ScheduledReport[] = [
        {
          id: '1',
          name: 'Daily Operations Summary',
          reportType: 'Operations Summary',
          schedule: 'Daily at 6:00 AM',
          format: 'PDF',
          recipients: ['admin@company.com', 'ops@company.com'],
          isActive: true,
          lastRun: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
          nextRun: new Date(Date.now() + 6 * 60 * 60 * 1000).toISOString()
        },
        {
          id: '2',
          name: 'Weekly Performance Report',
          reportType: 'Performance Analytics',
          schedule: 'Weekly on Monday',
          format: 'Excel',
          recipients: ['manager@company.com'],
          isActive: true,
          lastRun: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
          nextRun: new Date(Date.now() + 2 * 24 * 60 * 60 * 1000).toISOString()
        },
        {
          id: '3',
          name: 'Monthly Compliance Report',
          reportType: 'Compliance Report',
          schedule: 'Monthly on 1st',
          format: 'PDF',
          recipients: ['compliance@company.com', 'audit@company.com'],
          isActive: false,
          nextRun: new Date(Date.now() + 15 * 24 * 60 * 60 * 1000).toISOString()
        }
      ];
      
      setScheduledReports(mockScheduledReports);
    } catch (error) {
      console.error('Failed to load scheduled reports:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleScheduleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      // In real implementation, this would call the API
      const newSchedule: ScheduledReport = {
        id: Date.now().toString(),
        name: scheduleForm.name,
        reportType: scheduleForm.reportType,
        schedule: scheduleForm.schedule,
        format: scheduleForm.format.toUpperCase(),
        recipients: scheduleForm.recipients.split(',').map(email => email.trim()),
        isActive: scheduleForm.isActive,
        nextRun: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
      };

      setScheduledReports(prev => [...prev, newSchedule]);
      setShowScheduleForm(false);
      setScheduleForm({
        name: '',
        reportType: 'operations',
        schedule: 'daily',
        format: 'pdf',
        recipients: '',
        isActive: true
      });
    } catch (error) {
      console.error('Failed to schedule report:', error);
    }
  };

  const toggleReportStatus = async (reportId: string) => {
    setScheduledReports(prev =>
      prev.map(report =>
        report.id === reportId
          ? { ...report, isActive: !report.isActive }
          : report
      )
    );
  };

  const deleteScheduledReport = async (reportId: string) => {
    if (window.confirm('Are you sure you want to delete this scheduled report?')) {
      setScheduledReports(prev => prev.filter(report => report.id !== reportId));
    }
  };

  if (loading) {
    return <div className="loading">Loading scheduled reports...</div>;
  }

  return (
    <div className="scheduled-reports-manager">
      <div className="scheduled-reports-header">
        <h3>Automated Report Scheduling</h3>
        <button 
          className="btn btn-primary"
          onClick={() => setShowScheduleForm(true)}
        >
          📅 Schedule New Report
        </button>
      </div>

      {showScheduleForm && (
        <div className="schedule-form-overlay">
          <div className="schedule-form">
            <div className="form-header">
              <h4>Schedule New Report</h4>
              <button 
                className="close-btn"
                onClick={() => setShowScheduleForm(false)}
              >
                ✕
              </button>
            </div>
            
            <form onSubmit={handleScheduleSubmit}>
              <div className="form-group">
                <label>Report Name</label>
                <input
                  type="text"
                  value={scheduleForm.name}
                  onChange={(e) => setScheduleForm(prev => ({ ...prev, name: e.target.value }))}
                  className="form-control"
                  required
                  placeholder="Enter report name"
                />
              </div>

              <div className="grid grid-2">
                <div className="form-group">
                  <label>Report Type</label>
                  <select
                    value={scheduleForm.reportType}
                    onChange={(e) => setScheduleForm(prev => ({ ...prev, reportType: e.target.value }))}
                    className="form-control"
                  >
                    <option value="operations">Operations Summary</option>
                    <option value="performance">Performance Analytics</option>
                    <option value="compliance">Compliance Report</option>
                  </select>
                </div>

                <div className="form-group">
                  <label>Format</label>
                  <select
                    value={scheduleForm.format}
                    onChange={(e) => setScheduleForm(prev => ({ ...prev, format: e.target.value }))}
                    className="form-control"
                  >
                    <option value="pdf">PDF</option>
                    <option value="excel">Excel</option>
                    <option value="csv">CSV</option>
                  </select>
                </div>
              </div>

              <div className="form-group">
                <label>Schedule</label>
                <select
                  value={scheduleForm.schedule}
                  onChange={(e) => setScheduleForm(prev => ({ ...prev, schedule: e.target.value }))}
                  className="form-control"
                >
                  <option value="daily">Daily at 6:00 AM</option>
                  <option value="weekly">Weekly on Monday</option>
                  <option value="monthly">Monthly on 1st</option>
                  <option value="quarterly">Quarterly</option>
                </select>
              </div>

              <div className="form-group">
                <label>Recipients (comma-separated emails)</label>
                <input
                  type="text"
                  value={scheduleForm.recipients}
                  onChange={(e) => setScheduleForm(prev => ({ ...prev, recipients: e.target.value }))}
                  className="form-control"
                  placeholder="admin@company.com, ops@company.com"
                  required
                />
              </div>

              <div className="form-actions">
                <button type="button" className="btn btn-secondary" onClick={() => setShowScheduleForm(false)}>
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary">
                  Schedule Report
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div className="scheduled-reports-list">
        {scheduledReports.length === 0 ? (
          <div className="no-schedules">
            <h4>📅 No Scheduled Reports</h4>
            <p>Create your first automated report schedule to get started.</p>
          </div>
        ) : (
          <div className="schedules-grid">
            {scheduledReports.map((report) => (
              <div key={report.id} className="schedule-card">
                <div className="schedule-header">
                  <h4>{report.name}</h4>
                  <div className="schedule-status">
                    <label className="toggle-switch">
                      <input
                        type="checkbox"
                        checked={report.isActive}
                        onChange={() => toggleReportStatus(report.id)}
                      />
                      <span className="slider"></span>
                    </label>
                  </div>
                </div>

                <div className="schedule-details">
                  <div className="detail-item">
                    <span className="label">Type:</span>
                    <span className="value">{report.reportType}</span>
                  </div>
                  <div className="detail-item">
                    <span className="label">Schedule:</span>
                    <span className="value">{report.schedule}</span>
                  </div>
                  <div className="detail-item">
                    <span className="label">Format:</span>
                    <span className="value">{report.format}</span>
                  </div>
                  <div className="detail-item">
                    <span className="label">Recipients:</span>
                    <span className="value">{report.recipients.length} recipients</span>
                  </div>
                  {report.lastRun && (
                    <div className="detail-item">
                      <span className="label">Last Run:</span>
                      <span className="value">{new Date(report.lastRun).toLocaleDateString()}</span>
                    </div>
                  )}
                  <div className="detail-item">
                    <span className="label">Next Run:</span>
                    <span className="value">{new Date(report.nextRun).toLocaleDateString()}</span>
                  </div>
                </div>

                <div className="schedule-actions">
                  <button className="btn btn-sm btn-outline">Edit</button>
                  <button 
                    className="btn btn-sm btn-danger"
                    onClick={() => deleteScheduledReport(report.id)}
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default ScheduledReports;