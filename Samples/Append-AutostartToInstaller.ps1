# Helper script invoked by MSBuild post-packaging target to bundle Register-AutostartTask.ps1 into published install scripts.
param(
    [string]$PublishDir = "",
    [string]$ScriptDir = ""
)

Write-Host "Append-AutostartToInstaller: PublishDir = '$PublishDir'"
Write-Host "Append-AutostartToInstaller: ScriptDir = '$ScriptDir'"

if (-not $PublishDir -or -not (Test-Path $PublishDir)) {
    Write-Host "Publish directory '$PublishDir' not found."
    exit 0
}

$AutostartScript = Join-Path $ScriptDir "Register-AutostartTask.ps1"
if (-not (Test-Path $AutostartScript)) {
    Write-Warning "Autostart script '$AutostartScript' not found."
    exit 0
}

$scripts = Get-ChildItem -Path $PublishDir -Filter "*.ps1" -Recurse | Where-Object { 
    $_.Name -ne 'Register-AutostartTask.ps1' -and 
    $_.Name -ne 'Append-AutostartToInstaller.ps1' -and 
    $_.DirectoryName -notmatch 'TelemetryDependencies' 
}

Write-Host "Found $($scripts.Count) scripts to modify."

foreach ($script in $scripts) {
    Write-Host "Processing script: $($script.FullName)"
    Copy-Item -Path $AutostartScript -Destination $script.DirectoryName -Force
    $content = Get-Content $script.FullName -Raw
    if ($content -notmatch 'Register-AutostartTask.ps1') {
        $cmd = "`r`n# Register Task Scheduler Autostart`r`nWrite-Host 'Running Task Scheduler Autostart Setup...' -ForegroundColor Cyan`r`n& `"`$PSScriptRoot\Register-AutostartTask.ps1`"`r`n"
        $sigIdx = $content.IndexOf('# SIG # Begin signature block')
        if ($sigIdx -ge 0) {
            $newContent = $content.Insert($sigIdx, $cmd)
            [System.IO.File]::WriteAllText($script.FullName, $newContent)
        } else {
            Add-Content -Path $script.FullName -Value $cmd
        }
        Write-Host "Successfully bundled Register-AutostartTask.ps1 into $($script.Name)" -ForegroundColor Green
    } else {
        Write-Host "Already contains Register-AutostartTask.ps1" -ForegroundColor Yellow
    }
}
