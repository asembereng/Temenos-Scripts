#
# Module manifest for module 'TemenosChecks.Sql'
#

@{
    RootModule = 'TemenosChecks.Sql.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'b2c3d4e5-f6g7-8901-bcde-f23456789012'
    Author = 'Temenos Alert Manager'
    CompanyName = 'Central Bank'
    Copyright = '(c) 2024 Central Bank. All rights reserved.'
    Description = 'SQL Server monitoring checks for Temenos environments'
    PowerShellVersion = '7.0'
    RequiredModules = @('TemenosChecks.Common', 'SqlServer')
    
    FunctionsToExport = @(
        'Test-SqlServerAvailability',
        'Get-SqlBlockingSessions',
        'Get-SqlLongRunningQueries',
        'Get-SqlTempDbUsage',
        'Get-SqlLogSpaceUsage',
        'Get-SqlAgReplicationLag',
        'Get-SqlFailedJobs',
        'Get-SqlPerformanceCounters',
        'Test-SqlConnectivity',
        'Get-SqlDatabaseFileGrowth'
    )
    
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
    
    PrivateData = @{
        PSData = @{
            Tags = @('Temenos', 'SQL', 'Monitoring', 'Banking', 'Database')
            LicenseUri = ''
            ProjectUri = ''
            IconUri = ''
            ReleaseNotes = 'Initial release of SQL Server monitoring module'
        }
    }
}