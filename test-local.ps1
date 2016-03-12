# run tests
$output = (.\packages\Fixie.1.0.0.33\lib\net45\fixie.console.exe .\src\GraphQL.Tests\bin\debug\GraphQL.Tests.dll --xUnitXml .\fixie-results.xml) -join "`r`n"

$testResult = $LASTEXITCODE

if ($testResult -eq 0)
{
  Write-Host $output
}

if ($testResult -ne 0) {
    throw "{0}." -f ([string] $output)
}
