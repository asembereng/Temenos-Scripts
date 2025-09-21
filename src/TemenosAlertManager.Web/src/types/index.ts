// API Response Types
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

// Service Management Types
export interface ServiceStatus {
  id: number;
  name: string;
  host: string;
  type: string;
  status: 'Healthy' | 'Warning' | 'Critical' | 'Unknown';
  lastChecked: string;
  canStart: boolean;
  canStop: boolean;
  canRestart: boolean;
}

export interface ServiceAction {
  serviceId: number;
  action: 'start' | 'stop' | 'restart' | 'healthcheck';
}

// SOD/EOD Types
export interface SODRequest {
  environment: string;
  servicesFilter: string[];
  dryRun: boolean;
  forceExecution: boolean;
  comments?: string;
}

export interface EODRequest {
  environment: string;
  servicesFilter: string[];
  dryRun: boolean;
  forceExecution: boolean;
  cutoffTime?: string;
  comments?: string;
}

export interface OperationResult {
  operationId: string;
  status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';
  progressPercentage: number;
  currentStep: string;
  startTime: string;
  estimatedDuration?: string;
  steps: OperationStep[];
}

export interface OperationStep {
  name: string;
  status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Skipped';
  startTime?: string;
  endTime?: string;
  details?: string;
  errorMessage?: string;
}

// Monitoring Types
export interface HealthSummary {
  domain: string;
  overallStatus: 'Success' | 'Warning' | 'Critical' | 'Error';
  activeAlerts: number;
  criticalAlerts: number;
  warningAlerts: number;
  lastChecked: string;
  metrics: Record<string, any>;
}

export interface Alert {
  id: number;
  title: string;
  description: string;
  severity: 'Info' | 'Warning' | 'Critical';
  state: 'Active' | 'Acknowledged' | 'Resolved' | 'Suppressed';
  domain: string;
  source: string;
  metricValue?: string;
  threshold?: string;
  createdAt: string;
  acknowledgedAt?: string;
  acknowledgedBy?: string;
  resolvedAt?: string;
  resolvedBy?: string;
  notes?: string;
}

export interface DashboardData {
  domainSummaries: HealthSummary[];
  recentAlerts: Alert[];
  systemMetrics: SystemMetrics;
  activeOperations: OperationResult[];
}

export interface SystemMetrics {
  cpuUtilization: number;
  memoryUtilization: number;
  diskUtilization: number;
  networkUtilization: number;
  activeConnections: number;
  responseTime: number;
  throughput: number;
  errorRate: number;
  timestamp: string;
}

// User and Authentication Types
export interface User {
  id: string;
  username: string;
  displayName: string;
  email: string;
  role: 'Viewer' | 'Operator' | 'Admin';
  permissions: string[];
}

// Navigation Types
export interface NavigationItem {
  label: string;
  path: string;
  icon: string;
  requiredRole?: string[];
  children?: NavigationItem[];
}