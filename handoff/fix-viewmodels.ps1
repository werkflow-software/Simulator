$vmDir = 'C:\WerkFlow\Coding\Simulation\Werkflow.OpcUaSimulator.App\ViewModels'
Get-ChildItem -Path $vmDir -Filter '*.cs' | ForEach-Object {
    $lines = Get-Content -LiteralPath $_.FullName
    $filtered = $lines | Where-Object { $_ -notmatch '^\s*\[(ObservableProperty|RelayCommand)\]' }
    Set-Content -LiteralPath $_.FullName -Value $filtered -Encoding utf8
    Write-Host "Updated $($_.Name)"
}
