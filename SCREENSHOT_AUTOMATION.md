# Screenshot Automation for Temenos Service Management Platform

This directory contains automated screenshot capture functionality for the Temenos Service Management Platform web interface.

## Overview

The screenshot automation system uses Playwright to systematically navigate through all pages of the Temenos web application and capture full-page screenshots. This provides comprehensive visual documentation of the user interface and all available functionality.

## Prerequisites

1. **Node.js** (v20 or higher)
2. **npm** package manager
3. **Temenos Web Application** running on http://localhost:3000

## Installation

1. Install dependencies in the root project directory:
   ```bash
   npm install
   ```

2. Install web application dependencies:
   ```bash
   npm run install:web
   ```

## Usage

### Quick Start

1. Start the web application:
   ```bash
   npm run start:web
   ```

2. In a new terminal, run the screenshot automation:
   ```bash
   npm run screenshots
   ```

### Manual Steps

If you prefer to run commands manually:

1. Start the development server:
   ```bash
   cd src/TemenosAlertManager.Web
   npm start
   ```

2. Wait for the server to start (usually at http://localhost:3000)

3. Run the automation script:
   ```bash
   node scripts/screenshot-automation.js
   ```

## Output

The automation will:

1. **Create screenshots directory** if it doesn't exist
2. **Navigate to each page** systematically:
   - Dashboard
   - Service Management
   - SOD/EOD Operations
   - Monitoring
   - Reports
   - Administration (System Configuration)
   - Administration (User Management)
3. **Capture full-page screenshots** in PNG format
4. **Generate comprehensive documentation** with descriptions

## Generated Files

```
screenshots/
├── 01-dashboard.png
├── 02-service-management.png
├── 03-sod-eod-operations.png
├── 04-monitoring.png
├── 05-reports.png
├── 06-administration-system-config.png
├── 07-administration-user-management.png
└── README.md (comprehensive documentation)
```

## Features

### Automated Navigation
- Systematic navigation through all application pages
- Proper wait times for page loading and stabilization
- Error handling for navigation issues

### Screenshot Quality
- Full-page screenshots at 1920x1080 resolution
- PNG format for high quality and compatibility
- Consistent naming convention for easy organization

### Documentation Generation
- Automatic creation of comprehensive documentation
- Detailed descriptions for each page and functionality
- Technical information and usage instructions
- Visual reference with embedded screenshots

### Browser Configuration
- Headless browser operation for efficiency
- Optimized viewport settings
- Cross-platform compatibility

## Customization

### Configuration Options

Edit `scripts/screenshot-automation.js` to customize:

```javascript
const CONFIG = {
    baseUrl: 'http://localhost:3000',        // Application URL
    screenshotDir: './screenshots',          // Output directory
    waitTime: 2000,                         // Wait between pages (ms)
    viewportWidth: 1920,                    // Browser width
    viewportHeight: 1080                    // Browser height
};
```

### Adding New Pages

Add new pages to the `PAGES` array in the script:

```javascript
{
    route: '/new-page',
    filename: '08-new-page.png',
    title: 'New Page',
    description: 'Description of the new page functionality'
}
```

## Troubleshooting

### Common Issues

1. **Port 3000 not available**
   - Check if the web application is running
   - Verify the correct port in the CONFIG.baseUrl

2. **Playwright browser not found**
   - Run: `npx playwright install chromium`

3. **Permission errors**
   - Ensure write permissions for the screenshots directory
   - Run with appropriate user permissions

4. **Network timeouts**
   - Increase waitTime in CONFIG
   - Check for API connectivity issues

### Debugging

Enable debugging by modifying the script:

```javascript
// Launch browser with visible interface
browser = await chromium.launch({
    headless: false,  // Change to false
    slowMo: 1000     // Add slow motion
});
```

## Integration

### CI/CD Integration

The automation can be integrated into CI/CD pipelines:

```bash
# Example GitHub Actions step
- name: Generate Screenshots
  run: |
    npm run start:web &
    sleep 30  # Wait for server start
    npm run screenshots
    ls -la screenshots/
```

### Scheduled Documentation

Set up scheduled runs to keep documentation current:

```bash
# Cron job example (daily at 2 AM)
0 2 * * * cd /path/to/project && npm run screenshots
```

## Development

### Script Structure

- `captureScreenshots()` - Main orchestration function
- `capturePageScreenshot()` - Individual page capture
- `generateDocumentation()` - Creates README with descriptions
- `ensureDirectoryExists()` - Directory management utility

### Error Handling

The script includes comprehensive error handling:
- Browser launch failures
- Page navigation errors
- Screenshot capture issues
- File system operations

### Extensibility

The script is designed for easy extension:
- Modular page configuration
- Configurable wait times and viewport
- Customizable documentation templates
- Support for different output formats

## Performance

### Optimization Tips

1. **Headless Mode**: Always use headless mode for production
2. **Parallel Execution**: Consider parallel page capture for large applications
3. **Selective Capture**: Filter pages based on environment or requirements
4. **Resource Management**: Ensure proper browser cleanup

### Metrics

Typical performance for the Temenos platform:
- **Total Pages**: 7
- **Execution Time**: ~30-60 seconds
- **Output Size**: ~500-600 KB total
- **Memory Usage**: ~100-200 MB peak

## Contributing

When contributing to the screenshot automation:

1. **Test thoroughly** with different browser configurations
2. **Update documentation** for any new features or pages
3. **Follow naming conventions** for consistency
4. **Verify cross-platform compatibility**

## License

This automation script is part of the Temenos Service Management Platform and follows the same licensing terms (AGPL-3.0).

## Support

For issues or questions about the screenshot automation:

1. Check the troubleshooting section above
2. Review the generated logs for error details
3. Verify the web application is running correctly
4. Check Playwright documentation for browser-specific issues

## Version History

- **v1.0.0** - Initial implementation with basic page capture
- **v1.1.0** - Added comprehensive documentation generation
- **v1.2.0** - Enhanced error handling and configuration options