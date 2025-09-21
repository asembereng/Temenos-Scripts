# Temenos Service Management Platform - Web Interface

This directory contains the React + TypeScript frontend for the Temenos Service Management Platform.

## 🛠️ Technology Stack

- **React 19** - UI framework
- **TypeScript** - Type safety and enhanced development experience
- **Webpack 5** - Bundling and build system
- **Axios** - HTTP client for API communication
- **React Router** - Client-side routing
- **CSS3** - Styling with custom CSS

## 🏗️ Architecture

### Components Structure
```
src/
├── components/          # Reusable UI components
│   └── Layout.tsx      # Main layout with navigation
├── pages/              # Page components
│   ├── Dashboard.tsx   # System overview dashboard
│   ├── ServiceManagement.tsx  # Service control interface
│   ├── SODEODOperations.tsx   # SOD/EOD operation controls
│   ├── Monitoring.tsx  # Real-time monitoring (placeholder)
│   └── Reports.tsx     # Report generation (placeholder)
├── services/           # API service layer
│   └── apiService.ts   # Centralized API communication
├── types/              # TypeScript type definitions
│   └── index.ts        # Shared interfaces and types
├── App.tsx             # Main application component
└── index.tsx           # Application entry point
```

### Key Features Implemented

#### ✅ Dashboard
- System health overview with status indicators
- Real-time metrics display (CPU, Memory, Disk utilization)
- Recent alerts summary
- Active operations monitoring
- Auto-refresh every 30 seconds

#### ✅ Service Management
- Comprehensive service listing with status
- Service control actions (Start/Stop/Restart/Health Check)
- Filter by service type
- Real-time status updates
- Service statistics overview

#### ✅ SOD/EOD Operations
- Interactive forms for SOD (Start of Day) operations
- Interactive forms for EOD (End of Day) operations
- Environment selection (DEV/TEST/UAT/PROD)
- Service filtering options
- Dry run and force execution options
- Real-time operation progress tracking
- Operation cancellation capability

#### ⏳ Monitoring (Placeholder)
- Framework ready for real-time monitoring features
- Designed for live performance metrics
- Alert monitoring integration planned

#### ⏳ Reports (Placeholder)
- Framework ready for report generation
- Multi-format export planned (PDF, Excel, CSV)
- Custom report templates planned

## 🚀 Getting Started

### Prerequisites
- Node.js 18+ 
- npm 8+

### Development Setup

1. **Install Dependencies**
   ```bash
   cd src/TemenosAlertManager.Web
   npm install
   ```

2. **Start Development Server**
   ```bash
   npm start
   ```
   - Opens on http://localhost:3000
   - Auto-reloads on file changes
   - Proxies API calls to https://localhost:5001

3. **Build for Production**
   ```bash
   npm run build
   ```
   - Creates optimized production build in `dist/` directory

### Integration with .NET API

The frontend is designed to integrate seamlessly with the ASP.NET Core API:

1. **Automated Build & Deploy**
   ```bash
   # From repository root
   ./build-frontend.sh
   ```
   This script:
   - Builds the React application
   - Copies build files to `TemenosAlertManager.Api/wwwroot/`
   - The .NET API serves these files automatically

2. **API Integration**
   - All API calls go through the centralized `apiService`
   - CORS is configured in the .NET API for development
   - Production serves frontend and API from same origin

## 📡 API Integration

### Service Layer
The `apiService.ts` provides a centralized way to communicate with the backend:

```typescript
// Health monitoring
await apiService.getHealth();
await apiService.getDashboard();

// Service management
await apiService.getServices();
await apiService.performServiceAction({ serviceId: 1, action: 'restart' });

// SOD/EOD operations
await apiService.startSOD({ environment: 'DEV', dryRun: true });
await apiService.getOperationStatus('operation-id');
```

### Authentication
- Token-based authentication ready
- Automatic token refresh handling
- Redirect to login on 401 responses

## 🎨 UI/UX Design

### Design Principles
- **Clean & Professional** - Business-focused interface
- **Responsive** - Works on desktop, tablet, and mobile
- **Accessible** - Keyboard navigation and screen reader support
- **Consistent** - Unified color scheme and typography

### Color Scheme
- **Primary**: #007bff (Blue)
- **Success**: #28a745 (Green) 
- **Warning**: #ffc107 (Yellow)
- **Danger**: #dc3545 (Red)
- **Secondary**: #6c757d (Gray)

### Status Indicators
- 🟢 **Healthy/Success** - Green badges and progress bars
- 🟡 **Warning** - Yellow badges for caution states
- 🔴 **Critical/Error** - Red badges for failures
- ⚪ **Unknown** - Gray badges for unknown states

## 🔧 Development Guidelines

### Code Organization
- **Components**: Reusable UI components in `components/`
- **Pages**: Route-specific page components in `pages/`
- **Services**: API and business logic in `services/`
- **Types**: TypeScript definitions in `types/`

### Styling
- Component-specific CSS files alongside components
- Global styles in `App.css`
- CSS custom properties for theming
- Responsive design with CSS Grid and Flexbox

### State Management
- React hooks for component state
- Service layer for API state management
- No external state management library (keeps it simple)

## 🚀 Production Deployment

### Build Process
1. Run `npm run build` to create production build
2. Files are optimized and minified
3. Code splitting separates vendor and application code
4. Source maps generated for debugging

### Performance
- Lazy loading planned for route-based code splitting
- Asset optimization with Webpack
- Gzip compression ready
- CDN-ready static assets

## 🔄 Integration Points

### With .NET API
- **Health**: `/health` and `/api/monitoring/system-health`
- **Services**: `/api/services/*` endpoints
- **Operations**: `/api/temenos/operations/*` endpoints
- **Alerts**: `/api/alerts/*` endpoints

### External Tools
- **Swagger**: Direct links to `/swagger` for API documentation
- **Hangfire**: Direct links to `/hangfire` for job monitoring

## 🐛 Troubleshooting

### Common Issues

1. **API Connection Errors**
   - Ensure .NET API is running on https://localhost:5001
   - Check CORS configuration in Program.cs
   - Verify proxy settings in webpack.config.js

2. **Build Failures**
   - Clear node_modules and run `npm install`
   - Check Node.js version compatibility
   - Verify TypeScript configuration

3. **Runtime Errors**
   - Check browser console for JavaScript errors
   - Verify API endpoints are responding
   - Check network tab for failed requests

### Development Tips
- Use browser dev tools React extension
- Enable source maps for debugging
- Check API responses in Network tab
- Use TypeScript strict mode for better error catching

## 🎯 Future Enhancements

### Planned Features
- **Real-time Updates**: WebSocket/SignalR integration
- **Advanced Monitoring**: Custom dashboards and charts
- **Report Builder**: Interactive report generation
- **User Management**: Role-based access control
- **Themes**: Dark/light mode support
- **Offline Support**: PWA capabilities

### Performance Improvements
- Route-based code splitting
- Virtual scrolling for large lists
- Memoization for expensive computations
- Background data fetching

This web interface provides a comprehensive, user-friendly way to manage Temenos services and operations while maintaining professional enterprise standards.