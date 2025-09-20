#
# Module manifest for module 'TemenosChecks.MQ'
#

@{
    RootModule = 'TemenosChecks.MQ.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'd4e5f6g7-h8i9-0123-defg-h45678901234'
    Author = 'Temenos Alert Manager'
    CompanyName = 'Central Bank'
    Copyright = '(c) 2024 Central Bank. All rights reserved.'
    Description = 'IBM MQ monitoring checks for Temenos environments'
    PowerShellVersion = '7.0'
    RequiredModules = @('TemenosChecks.Common')
    
    FunctionsToExport = @(
        'Test-MqConnectivity',
        'Get-MqQueueDepth',
        'Get-MqChannelStatus',
        'Test-MqRoundTrip',
        'Get-MqQueueManagerStatus',
        'Get-MqDeadLetterQueue',
        'Get-MqPerformanceStats'
    )
    
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
    
    PrivateData = @{
        PSData = @{
            Tags = @('Temenos', 'MQ', 'IBM', 'Monitoring', 'Banking', 'Messaging')
            LicenseUri = ''
            ProjectUri = ''
            IconUri = ''
            ReleaseNotes = 'Initial release of IBM MQ monitoring module'
        }
    }
}