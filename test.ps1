# run tests
# run tests
$output = (.\packages\Fixie.1.0.0.33\lib\net45\fixie.console.exe .\src\GraphQL.Tests\bin\debug\GraphQL.Tests.dll --xUnitXml .\fixie-results.xml) -join "`r`n"

$testResult = $LASTEXITCODE

if ($testResult -eq 0)
{
  Write-Host $output
}

# upload results to AppVeyor
$wc = New-Object 'System.Net.WebClient'
$wc.UploadFile("https://ci.appveyor.com/api/testresults/xunit/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\fixie-results.xml))

if ($testResult -ne 0) {
    throw "{0}." -f ([string] $output)
}
