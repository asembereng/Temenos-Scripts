@{
    ModuleVersion = '1.0.0'
    GUID = '12345678-1234-5678-9012-123456789012'
    Author = 'Temenos Alert Manager'
    CompanyName = 'Bank Operations'
    Copyright = '(c) 2024 Bank Operations. All rights reserved.'
    Description = 'PowerShell module for Temenos Start of Day and End of Day operations'
    PowerShellVersion = '5.1'
    
    # Functions to export from this module
    FunctionsToExport = @(
        'Start-TemenosSOD',
        'Start-TemenosEOD',
        'Get-TemenosOperationStatus',
        'Stop-TemenosOperation'
    )
    
    # Cmdlets to export from this module
    CmdletsToExport = @()
    
    # Variables to export from this module
    VariablesToExport = '*'
    
    # Aliases to export from this module
    AliasesToExport = @()
    
    # List of all files packaged with this module
    FileList = @(
        'TemenosChecks.SOD.psm1',
        'TemenosChecks.SOD.psd1'
    )
    
    # Private data to pass to the module specified in RootModule/ModuleToProcess
    PrivateData = @{
        PSData = @{
            Tags = @('Temenos', 'SOD', 'EOD', 'Banking', 'Operations')
            LicenseUri = ''
            ProjectUri = ''
            IconUri = ''
            ReleaseNotes = 'Initial release of Temenos SOD/EOD PowerShell module'
        }
    }
}