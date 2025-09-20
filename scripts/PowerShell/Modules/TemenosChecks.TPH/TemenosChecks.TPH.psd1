#
# Module manifest for module 'TemenosChecks.TPH'
#

@{
    RootModule = 'TemenosChecks.TPH.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'c3d4e5f6-g7h8-9012-cdef-g34567890123'
    Author = 'Temenos Alert Manager'
    CompanyName = 'Central Bank'
    Copyright = '(c) 2024 Central Bank. All rights reserved.'
    Description = 'TPH (Payment Hub) monitoring checks for Temenos environments'
    PowerShellVersion = '7.0'
    RequiredModules = @('TemenosChecks.Common')
    
    FunctionsToExport = @(
        'Test-TphServices',
        'Get-TphQueueDepth',
        'Test-TphConnectivity',
        'Get-TphTransactionStatus',
        'Get-TphPerformanceMetrics',
        'Test-TphListenerStatus',
        'Get-TphErrorLogs'
    )
    
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
    
    PrivateData = @{
        PSData = @{
            Tags = @('Temenos', 'TPH', 'PaymentHub', 'Monitoring', 'Banking')
            LicenseUri = ''
            ProjectUri = ''
            IconUri = ''
            ReleaseNotes = 'Initial release of TPH monitoring module'
        }
    }
}