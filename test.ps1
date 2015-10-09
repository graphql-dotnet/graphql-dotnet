# run tests
.\packages\Fixie.1.0.0.29\lib\net45\fixie.console.exe .\src\GraphQL.Tests\bin\debug\GraphQL.Tests.dll --xUnitXml .\fixie-results.xml

# upload results to AppVeyor
$wc = New-Object 'System.Net.WebClient'
$wc.UploadFile("https://ci.appveyor.com/api/testresults/xunit/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\fixie-results.xml))
