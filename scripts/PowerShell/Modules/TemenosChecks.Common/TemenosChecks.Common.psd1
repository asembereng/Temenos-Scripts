#
# Module manifest for module 'TemenosChecks.Common'
#

@{
    RootModule = 'TemenosChecks.Common.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
    Author = 'Temenos Alert Manager'
    CompanyName = 'Central Bank'
    Copyright = '(c) 2024 Central Bank. All rights reserved.'
    Description = 'Common utilities for Temenos monitoring checks'
    PowerShellVersion = '7.0'
    RequiredModules = @()
    
    FunctionsToExport = @(
        'Write-CheckLog',
        'New-CheckResult',
        'Test-ServiceExists',
        'Get-ServiceStatus',
        'Invoke-PowerShellSecurely',
        'ConvertTo-ThresholdObject',
        'Test-NetworkConnectivity',
        'Get-WindowsEventLogs'
    )
    
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
    
    PrivateData = @{
        PSData = @{
            Tags = @('Temenos', 'Monitoring', 'Banking', 'Common')
            LicenseUri = ''
            ProjectUri = ''
            IconUri = ''
            ReleaseNotes = 'Initial release of common monitoring utilities'
        }
    }
}