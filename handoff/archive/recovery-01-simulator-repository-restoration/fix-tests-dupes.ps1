$testsDir = 'C:\WerkFlow\Coding\Simulation\Werkflow.OpcUaSimulator.Tests'
$decompileDir = 'C:\WerkFlow\Coding\Simulation\_recovery\tests_debug\Werkflow.OpcUaSimulator.Tests'

Get-ChildItem -Path $testsDir -Filter 'Ap4*.cs' -File | Where-Object {
    $_.Name -notlike 'PhysicalAp4*'
} | ForEach-Object { Remove-Item $_.FullName -Force; Write-Host "Removed $($_.Name)" }

Get-ChildItem -Path $testsDir -Filter 'R4*.cs' -File | ForEach-Object {
    Remove-Item $_.FullName -Force; Write-Host "Removed $($_.Name)"
}

$fts = Join-Path $testsDir 'FaultScenarioTestStack.cs'
if (Test-Path $fts) { Remove-Item $fts -Force; Write-Host 'Removed FaultScenarioTestStack.cs' }

Copy-Item (Join-Path $decompileDir 'R1LongRunReport.cs') $testsDir -Force
Copy-Item (Join-Path $decompileDir 'R1MachineReport.cs') $testsDir -Force
Write-Host 'Restored R1 report types'
