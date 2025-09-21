/**
 * Temenos Service Management Platform - Screenshot Automation Script
 * 
 * This script uses Playwright to automatically navigate through all pages 
 * of the Temenos Service Management Platform and capture full-page screenshots.
 * 
 * Requirements:
 * - Node.js and npm installed
 * - Playwright package installed: npm install playwright
 * - Temenos web application running on http://localhost:3000
 * 
 * Usage:
 * node scripts/screenshot-automation.js
 */

const { chromium } = require('playwright');
const fs = require('fs').promises;
const path = require('path');

// Configuration
const CONFIG = {
    baseUrl: 'http://localhost:3000',
    screenshotDir: path.join(__dirname, '..', 'screenshots'),
    waitTime: 2000, // Wait time between navigations
    viewportWidth: 1920,
    viewportHeight: 1080
};

// Pages to capture with their routes and descriptions
const PAGES = [
    {
        route: '/dashboard',
        filename: '01-dashboard.png',
        title: 'Dashboard',
        description: 'Main dashboard showing system overview and key metrics'
    },
    {
        route: '/services',
        filename: '02-service-management.png',
        title: 'Service Management',
        description: 'Service management interface for monitoring and controlling Temenos services'
    },
    {
        route: '/operations',
        filename: '03-sod-eod-operations.png',
        title: 'SOD/EOD Operations',
        description: 'Start of Day and End of Day operations management with comprehensive configuration forms'
    },
    {
        route: '/monitoring',
        filename: '04-monitoring.png',
        title: 'Monitoring',
        description: 'Real-time monitoring dashboard for system health and performance metrics'
    },
    {
        route: '/reports',
        filename: '05-reports.png',
        title: 'Reports',
        description: 'Report generation interface for various system and compliance reports'
    },
    {
        route: '/administration/system-config',
        filename: '06-administration-system-config.png',
        title: 'Administration - System Configuration',
        description: 'System configuration interface for managing service hosts and system settings'
    },
    {
        route: '/administration/user-management',
        filename: '07-administration-user-management.png',
        title: 'Administration - User Management',
        description: 'User management interface for Active Directory integration and role mappings'
    }
];

/**
 * Main function to capture screenshots of all pages
 */
async function captureScreenshots() {
    let browser;
    
    try {
        console.log('🚀 Starting Temenos Platform Screenshot Automation...');
        
        // Ensure screenshots directory exists
        await ensureDirectoryExists(CONFIG.screenshotDir);
        
        // Launch browser
        browser = await chromium.launch({
            headless: true,
            args: ['--no-sandbox', '--disable-dev-shm-usage']
        });
        
        const context = await browser.newContext({
            viewport: { 
                width: CONFIG.viewportWidth, 
                height: CONFIG.viewportHeight 
            }
        });
        
        const page = await context.newPage();
        
        // Capture screenshots for each page
        for (const pageInfo of PAGES) {
            await capturePageScreenshot(page, pageInfo);
        }
        
        console.log('✅ All screenshots captured successfully!');
        console.log(`📁 Screenshots saved to: ${CONFIG.screenshotDir}`);
        
        // Generate documentation
        await generateDocumentation();
        
    } catch (error) {
        console.error('❌ Error during screenshot capture:', error);
        throw error;
    } finally {
        if (browser) {
            await browser.close();
        }
    }
}

/**
 * Capture screenshot for a specific page
 */
async function capturePageScreenshot(page, pageInfo) {
    try {
        console.log(`📸 Capturing ${pageInfo.title} (${pageInfo.route})`);
        
        const fullUrl = `${CONFIG.baseUrl}${pageInfo.route}`;
        await page.goto(fullUrl, { waitUntil: 'networkidle' });
        
        // Wait for the page to stabilize
        await page.waitForTimeout(CONFIG.waitTime);
        
        // Take full-page screenshot
        const screenshotPath = path.join(CONFIG.screenshotDir, pageInfo.filename);
        await page.screenshot({
            path: screenshotPath,
            fullPage: true,
            type: 'png'
        });
        
        console.log(`✅ Screenshot saved: ${pageInfo.filename}`);
        
    } catch (error) {
        console.error(`❌ Failed to capture ${pageInfo.title}:`, error);
        throw error;
    }
}

/**
 * Ensure directory exists, create if it doesn't
 */
async function ensureDirectoryExists(dirPath) {
    try {
        await fs.access(dirPath);
    } catch {
        await fs.mkdir(dirPath, { recursive: true });
        console.log(`📁 Created directory: ${dirPath}`);
    }
}

/**
 * Generate markdown documentation with screenshot descriptions
 */
async function generateDocumentation() {
    const timestamp = new Date().toISOString();
    const docContent = `# Temenos Service Management Platform - Screenshots

Generated on: ${timestamp}

This document contains screenshots of all pages in the Temenos Service Management Platform web interface.

## Overview

The Temenos Service Management Platform is a comprehensive enterprise-grade solution for managing Temenos banking environments. This screenshot collection provides a visual reference for all major interface components and functionality.

## Application Screenshots

${PAGES.map((page, index) => `
### ${index + 1}. ${page.title}

**File:** \`${page.filename}\`  
**Route:** \`${page.route}\`  
**Description:** ${page.description}

![${page.title}](${page.filename})

---
`).join('')}

## Technical Information

- **Total Pages Captured:** ${PAGES.length}
- **Screenshot Format:** PNG
- **Resolution:** ${CONFIG.viewportWidth}x${CONFIG.viewportHeight}
- **Capture Method:** Full-page screenshots using Playwright
- **Base URL:** ${CONFIG.baseUrl}

## Application Features Demonstrated

### Dashboard
- System overview and status indicators
- Key performance metrics display
- Navigation sidebar with all available modules

### Service Management
- Service monitoring and status tracking
- Service filtering and categorization
- Real-time service health indicators

### SOD/EOD Operations
- Comprehensive operation configuration forms
- Environment selection (DEV, TEST, UAT, PROD)
- Service filtering options
- Dry run and force execution controls
- Operation progress tracking

### Monitoring
- Real-time monitoring capabilities
- System health dashboards
- Performance metrics visualization

### Reports
- Multi-format report generation capabilities
- Scheduled automated reports
- Custom report templates
- Compliance reporting features

### Administration
- **System Configuration:** Service host management, system settings
- **User Management:** Active Directory integration, role mappings

## Notes

- Screenshots were captured with the development server running
- Some API calls may show 504 Gateway Timeout errors as expected when backend services are not running
- All UI components and layouts are fully functional and responsive
- The interface uses modern React components with TypeScript

## Automation Script

This documentation was generated automatically using the Playwright automation script located at:
\`scripts/screenshot-automation.js\`

To regenerate screenshots:
1. Start the development server: \`npm start\` (in src/TemenosAlertManager.Web)
2. Run the automation script: \`node scripts/screenshot-automation.js\`
`;

    const docPath = path.join(CONFIG.screenshotDir, 'README.md');
    await fs.writeFile(docPath, docContent);
    console.log(`📝 Documentation generated: ${docPath}`);
}

// Run the script if called directly
if (require.main === module) {
    captureScreenshots()
        .then(() => {
            console.log('🎉 Screenshot automation completed successfully!');
            process.exit(0);
        })
        .catch((error) => {
            console.error('💥 Screenshot automation failed:', error);
            process.exit(1);
        });
}

module.exports = { captureScreenshots, PAGES, CONFIG };