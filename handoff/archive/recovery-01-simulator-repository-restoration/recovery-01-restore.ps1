# RECOVERY 01 - Restore simulator from Debug build artifacts + existing test sources
$ErrorActionPreference = 'Stop'
$Root = 'C:\WerkFlow\Coding\Simulation'
$BackupRoot = 'C:\WerkFlow\Coding\RecoveryBackup'
$Timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$BackupDir = Join-Path $BackupRoot "Simulation_broken_$Timestamp"

Write-Host "=== 1. Backup defekter Stand nach $BackupDir ==="
New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null
robocopy $Root $BackupDir /E /XD $BackupRoot /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy backup failed with $LASTEXITCODE" }

function Import-IlspyTree {
    param(
        [string]$SourceRoot,
        [string]$DestRoot,
        [string]$Prefix
    )
    if (-not (Test-Path $DestRoot)) { New-Item -ItemType Directory -Force -Path $DestRoot | Out-Null }

    Get-ChildItem -Path $SourceRoot -File -Filter '*.cs' -ErrorAction SilentlyContinue | ForEach-Object {
        if ($_.Name -match '^--z__') { return }
        Copy-Item $_.FullName (Join-Path $DestRoot $_.Name) -Force
    }

    Get-ChildItem -Path $SourceRoot -Directory | Where-Object { $_.Name -like "$Prefix*" } | ForEach-Object {
        $relative = $_.Name.Substring($Prefix.Length).Replace('.', [IO.Path]::DirectorySeparatorChar)
        $targetDir = if ($relative) { Join-Path $DestRoot $relative } else { $DestRoot }
        New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
        Get-ChildItem -Path $_.FullName -File | ForEach-Object {
            Copy-Item $_.FullName (Join-Path $targetDir $_.Name) -Force
        }
    }
}

function Clear-ProjectSource {
    param([string]$ProjectDir, [string[]]$PreserveDirs = @())
    if (-not (Test-Path $ProjectDir)) { return }
    Get-ChildItem -Path $ProjectDir -Recurse -Include '*.cs','*.csproj' -File | Where-Object {
        $rel = $_.FullName.Substring($ProjectDir.Length)
        foreach ($p in $PreserveDirs) {
            if ($rel -like "*\$p\*" -or $rel -like "\$p\*") { return $false }
        }
        return $true
    } | Remove-Item -Force
}

Write-Host "=== 2. Core aus Debug-Decompile ==="
Clear-ProjectSource (Join-Path $Root 'Werkflow.OpcUaSimulator.Core')
Import-IlspyTree -SourceRoot (Join-Path $Root '_recovery\core_debug') -DestRoot (Join-Path $Root 'Werkflow.OpcUaSimulator.Core') -Prefix 'Werkflow.OpcUaSimulator.Core.'

Write-Host "=== 3. OpcUa aus Debug-Decompile ==="
Clear-ProjectSource (Join-Path $Root 'Werkflow.OpcUaSimulator.OpcUa')
Import-IlspyTree -SourceRoot (Join-Path $Root '_recovery\opcua_debug') -DestRoot (Join-Path $Root 'Werkflow.OpcUaSimulator.OpcUa') -Prefix 'Werkflow.OpcUaSimulator.OpcUa.'

Write-Host "=== 4. App aus Debug-Decompile (BAML) ==="
$appDir = Join-Path $Root 'Werkflow.OpcUaSimulator.App'
Clear-ProjectSource $appDir -PreserveDirs @('MachineProfiles', 'FaultScenarios')
$appDebug = Join-Path $Root '_recovery\app_debug'
Import-IlspyTree -SourceRoot $appDebug -DestRoot $appDir -Prefix 'Werkflow.OpcUaSimulator.App.'
# BAML + styles
if (Test-Path (Join-Path $appDebug 'views')) { Copy-Item (Join-Path $appDebug 'views\*') (Join-Path $appDir 'views') -Recurse -Force }
if (Test-Path (Join-Path $appDebug 'styles')) { Copy-Item (Join-Path $appDebug 'styles\*') (Join-Path $appDir 'styles') -Recurse -Force }
foreach ($b in @('mainwindow.baml', 'app.baml')) {
    if (Test-Path (Join-Path $appDebug $b)) { Copy-Item (Join-Path $appDebug $b) $appDir -Force }
}

Write-Host "=== 5. Geschuetzte Dateien aus Backup ==="
$protectedProfiles = Join-Path $BackupDir 'Werkflow.OpcUaSimulator.App\MachineProfiles'
if (Test-Path $protectedProfiles) {
    Copy-Item (Join-Path $protectedProfiles '*') (Join-Path $appDir 'MachineProfiles') -Recurse -Force
}
$protectedFaults = Join-Path $BackupDir 'Werkflow.OpcUaSimulator.App\FaultScenarios'
if (Test-Path $protectedFaults) {
    Copy-Item (Join-Path $protectedFaults '*') (Join-Path $appDir 'FaultScenarios') -Recurse -Force
}

Write-Host "=== 6. Tests-Quellen erhalten ==="
# Tests .cs already in place; only remove obj/bin later

Write-Host "=== 7. Projektdateien schreiben ==="
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Werkflow.OpcUaSimulator.Core</RootNamespace>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.2" />
    <PackageReference Include="System.Text.Json" Version="8.0.5" />
  </ItemGroup>
</Project>
'@ | Set-Content (Join-Path $Root 'Werkflow.OpcUaSimulator.Core\Werkflow.OpcUaSimulator.Core.csproj') -Encoding utf8

@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Werkflow.OpcUaSimulator.OpcUa</RootNamespace>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="OPCFoundation.NetStandard.Opc.Ua.Server" Version="1.5.376.213" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Werkflow.OpcUaSimulator.Core\Werkflow.OpcUaSimulator.Core.csproj" />
  </ItemGroup>
</Project>
'@ | Set-Content (Join-Path $Root 'Werkflow.OpcUaSimulator.OpcUa\Werkflow.OpcUaSimulator.OpcUa.csproj') -Encoding utf8

@'
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <RootNamespace>Werkflow.OpcUaSimulator.App</RootNamespace>
    <AssemblyName>Werkflow OPC UA Simulator</AssemblyName>
    <ApplicationTitle>Werkflow OPC UA Simulator</ApplicationTitle>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Werkflow.OpcUaSimulator.Core\Werkflow.OpcUaSimulator.Core.csproj" />
    <ProjectReference Include="..\Werkflow.OpcUaSimulator.OpcUa\Werkflow.OpcUaSimulator.OpcUa.csproj" />
  </ItemGroup>
  <ItemGroup>
    <None Include="MachineProfiles\**\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Link>MachineProfiles\%(RecursiveDir)%(Filename)%(Extension)</Link>
    </None>
    <None Include="FaultScenarios\**\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Link>FaultScenarios\%(RecursiveDir)%(Filename)%(Extension)</Link>
    </None>
    <EmbeddedResource Include="views\*.baml" />
    <EmbeddedResource Include="styles\*.baml" />
    <EmbeddedResource Include="mainwindow.baml" />
    <EmbeddedResource Include="app.baml" />
  </ItemGroup>
</Project>
'@ | Set-Content (Join-Path $appDir 'Werkflow.OpcUaSimulator.App.csproj') -Encoding utf8

@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <RootNamespace>Werkflow.OpcUaSimulator.Tests</RootNamespace>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="OPCFoundation.NetStandard.Opc.Ua.Client" Version="1.5.376.213" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Werkflow.OpcUaSimulator.Core\Werkflow.OpcUaSimulator.Core.csproj" />
    <ProjectReference Include="..\Werkflow.OpcUaSimulator.OpcUa\Werkflow.OpcUaSimulator.OpcUa.csproj" />
  </ItemGroup>
</Project>
'@ | Set-Content (Join-Path $Root 'Werkflow.OpcUaSimulator.Tests\Werkflow.OpcUaSimulator.Tests.csproj') -Encoding utf8

@'
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Werkflow.OpcUaSimulator.App", "Werkflow.OpcUaSimulator.App\Werkflow.OpcUaSimulator.App.csproj", "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Werkflow.OpcUaSimulator.Core", "Werkflow.OpcUaSimulator.Core\Werkflow.OpcUaSimulator.Core.csproj", "{B2C3D4E5-F6A7-8901-BCDE-F12345678901}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Werkflow.OpcUaSimulator.OpcUa", "Werkflow.OpcUaSimulator.OpcUa\Werkflow.OpcUaSimulator.OpcUa.csproj", "{C3D4E5F6-A7B8-9012-CDEF-123456789012}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Werkflow.OpcUaSimulator.Tests", "Werkflow.OpcUaSimulator.Tests\Werkflow.OpcUaSimulator.Tests.csproj", "{D4E5F6A7-B8C9-0123-DEF0-234567890123}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Release|Any CPU.Build.0 = Release|Any CPU
		{B2C3D4E5-F6A7-8901-BCDE-F12345678901}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{B2C3D4E5-F6A7-8901-BCDE-F12345678901}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{B2C3D4E5-F6A7-8901-BCDE-F12345678901}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{B2C3D4E5-F6A7-8901-BCDE-F12345678901}.Release|Any CPU.Build.0 = Release|Any CPU
		{C3D4E5F6-A7B8-9012-CDEF-123456789012}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C3D4E5F6-A7B8-9012-CDEF-123456789012}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C3D4E5F6-A7B8-9012-CDEF-123456789012}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C3D4E5F6-A7B8-9012-CDEF-123456789012}.Release|Any CPU.Build.0 = Release|Any CPU
		{D4E5F6A7-B8C9-0123-DEF0-234567890123}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{D4E5F6A7-B8C9-0123-DEF0-234567890123}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{D4E5F6A7-B8C9-0123-DEF0-234567890123}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{D4E5F6A7-B8C9-0123-DEF0-234567890123}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
'@ | Set-Content (Join-Path $Root 'Werkflow.OpcUaSimulator.sln') -Encoding utf8

@'
.vs/
**/bin/
**/obj/
publish/
_recovery/
_zip_extract_*/
*.user
*.suo
RecoveryBackup/
'@ | Set-Content (Join-Path $Root '.gitignore') -Encoding utf8

Write-Host "=== 8. Build-Artefakte bereinigen ==="
Get-ChildItem -Path $Root -Recurse -Directory -Filter bin -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
Get-ChildItem -Path $Root -Recurse -Directory -Filter obj -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force

Write-Host "Recovery script completed. Backup: $BackupDir"
