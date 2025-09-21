@{
    ModuleVersion = '1.0.0'
    GUID = '87654321-4321-8765-2109-876543210987'
    Author = 'Temenos Alert Manager'
    CompanyName = 'Bank Operations'
    Copyright = '(c) 2024 Bank Operations. All rights reserved.'
    Description = 'PowerShell module for Temenos service management operations'
    PowerShellVersion = '5.1'
    
    # Functions to export from this module
    FunctionsToExport = @(
        'Start-TemenosService',
        'Stop-TemenosService',
        'Restart-TemenosService',
        'Test-TemenosServiceHealth'
    )
    
    # Cmdlets to export from this module
    CmdletsToExport = @()
    
    # Variables to export from this module
    VariablesToExport = '*'
    
    # Aliases to export from this module
    AliasesToExport = @()
    
    # List of all files packaged with this module
    FileList = @(
        'TemenosChecks.ServiceManagement.psm1',
        'TemenosChecks.ServiceManagement.psd1'
    )
    
    # Private data to pass to the module specified in RootModule/ModuleToProcess
    PrivateData = @{
        PSData = @{
            Tags = @('Temenos', 'ServiceManagement', 'Banking', 'Operations')
            LicenseUri = ''
            ProjectUri = ''
            IconUri = ''
            ReleaseNotes = 'Initial release of Temenos Service Management PowerShell module'
        }
    }
}