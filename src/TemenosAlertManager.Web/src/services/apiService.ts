import axios, { AxiosInstance, AxiosResponse } from 'axios';
import { 
  ApiResponse, 
  ServiceStatus, 
  ServiceAction, 
  SODRequest, 
  EODRequest, 
  OperationResult,
  DashboardData,
  Alert,
  SystemMetrics
} from '../types';

class ApiService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: '/api',
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor for auth
    this.api.interceptors.request.use(
      (config) => {
        // Add auth token if available
        const token = localStorage.getItem('authToken');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor for error handling
    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          // Handle authentication error
          localStorage.removeItem('authToken');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  // Generic HTTP methods
  async get<T = any>(url: string): Promise<T> {
    const response = await this.api.get(url);
    return response.data;
  }

  async post<T = any>(url: string, data?: any): Promise<T> {
    const response = await this.api.post(url, data);
    return response.data;
  }

  async put<T = any>(url: string, data?: any): Promise<T> {
    const response = await this.api.put(url, data);
    return response.data;
  }

  async delete<T = any>(url: string): Promise<T> {
    const response = await this.api.delete(url);
    return response.data;
  }

  // Health and Monitoring
  async getHealth(): Promise<any> {
    const response = await this.api.get('/health');
    return response.data;
  }

  async getSystemHealth(): Promise<any> {
    const response = await this.api.get('/monitoring/system-health');
    return response.data;
  }

  async getDashboard(): Promise<DashboardData> {
    const response = await this.api.get('/monitoring/dashboard');
    return response.data;
  }

  // Service Management
  async getServices(): Promise<ServiceStatus[]> {
    const response = await this.api.get('/services');
    return response.data;
  }

  async getServiceStatus(): Promise<ServiceStatus[]> {
    const response = await this.api.get('/services/status');
    return response.data;
  }

  async performServiceAction(action: ServiceAction): Promise<any> {
    const response = await this.api.post(`/services/${action.serviceId}/${action.action}`);
    return response.data;
  }

  // SOD/EOD Operations
  async startSOD(request: SODRequest): Promise<OperationResult> {
    const response = await this.api.post('/temenos/operations/sod', request);
    return response.data;
  }

  async startEOD(request: EODRequest): Promise<OperationResult> {
    const response = await this.api.post('/temenos/operations/eod', request);
    return response.data;
  }

  async getOperationStatus(operationId: string): Promise<OperationResult> {
    const response = await this.api.get(`/temenos/operations/${operationId}/status`);
    return response.data;
  }

  async cancelOperation(operationId: string): Promise<any> {
    const response = await this.api.post(`/temenos/operations/${operationId}/cancel`);
    return response.data;
  }

  async getActiveOperations(): Promise<OperationResult[]> {
    const response = await this.api.get('/temenos/operations/active');
    return response.data;
  }

  // Alerts
  async getAlerts(page = 1, pageSize = 50): Promise<Alert[]> {
    const response = await this.api.get(`/alerts?page=${page}&pageSize=${pageSize}`);
    return response.data;
  }

  async acknowledgeAlert(alertId: number, notes?: string): Promise<any> {
    const response = await this.api.post(`/alerts/${alertId}/acknowledge`, { notes });
    return response.data;
  }

  async resolveAlert(alertId: number, notes?: string): Promise<any> {
    const response = await this.api.post(`/alerts/${alertId}/resolve`, { notes });
    return response.data;
  }

  // Performance and Metrics
  async getPerformanceBaselines(): Promise<any> {
    const response = await this.api.get('/performance/baselines');
    return response.data;
  }

  async getPerformanceTrends(): Promise<any> {
    const response = await this.api.get('/performance/trends');
    return response.data;
  }

  // Reports
  async generateReport(reportConfig: any): Promise<any> {
    const response = await this.api.post('/reports/generate', reportConfig);
    return response.data;
  }

  async getReports(): Promise<any> {
    const response = await this.api.get('/reports');
    return response.data;
  }

  async downloadReport(reportId: string): Promise<Blob> {
    const response = await this.api.get(`/reports/${reportId}/download`, {
      responseType: 'blob'
    });
    return response.data;
  }
}

export const apiService = new ApiService();
export default apiService;