<#
AFTERTIDE: read-only local AI and ComfyUI inventory.
Windows PowerShell 5.1+ / PowerShell 7. No deleting, installing, model loading or API requests.
Run from the repository:
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Audit-LocalAI.ps1
Optional:
  pwsh -File .\scripts\Audit-LocalAI.ps1 -MaxDepth 5 -AdditionalRoots 'E:\','F:\AI'
The report is saved in the user's Documents folder, NOT in the Git repository.
#>
[CmdletBinding()]
param(
    [int]$MaxDepth = 4,
    [string[]]$AdditionalRoots = @()
)

$ErrorActionPreference = 'Continue'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$docs = [Environment]::GetFolderPath('MyDocuments')
if (-not $docs) { $docs = $env:TEMP }
$out = Join-Path $docs ("AFTERTIDE-LocalAI-Audit-" + $stamp)
New-Item -Path $out -ItemType Directory -Force | Out-Null

$disks = @(Get-PSDrive -PSProvider FileSystem | Where-Object { Test-Path $_.Root } | Select-Object -ExpandProperty Root)
$roots = New-Object 'System.Collections.Generic.List[string]'
# Do not scan the whole Windows system volume, only the user's home and common development roots.
$roots.Add($env:USERPROFILE)
foreach ($drive in $disks) {
    if ($drive -ne $env:SystemDrive.TrimEnd('\') + '\') { $roots.Add($drive) }
}
foreach ($r in $AdditionalRoots) { if ($r) { $roots.Add($r) } }
$roots = @($roots | Where-Object { $_ -and (Test-Path $_ -PathType Container) } | Sort-Object -Unique)
$folderRegex = '(?i)^(comfyui.*|local.?ai|ollama|lm.?studio|llama[._-]?cpp|stable-diffusion-webui.*|invokeai|fooocus|automatic1111|forge|models|checkpoints)$'
$skipRegex = '(?i)([\\/])(\.git|node_modules|\.venv|venv|\.cache|__pycache__|windows|program files(?: \(x86\))?)([\\/]|$)'
$matches = New-Object 'System.Collections.Generic.List[object]'

foreach ($root in $roots) {
    Write-Host "Scanning folders under $root (max depth $MaxDepth)..."
    try {
        # Depth is intentionally bounded to avoid a full multi-terabyte recursive scan.
        Get-ChildItem -LiteralPath $root -Directory -Recurse -Depth $MaxDepth -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch $skipRegex -and $_.Name -match $folderRegex } |
            ForEach-Object {
                $matches.Add([pscustomobject]@{
                    Name = $_.Name
                    Path = $_.FullName
                    LastWriteTime = $_.LastWriteTime
                })
            }
    } catch {
        Write-Warning ("Scanning problem in " + $root + ": " + $_.Exception.Message)
    }
}

# Add known stores even if they fall outside the depth/roots.
$known = @(
    (Join-Path $env:USERPROFILE '.ollama\models'),
    (Join-Path $env:USERPROFILE 'ComfyUI'),
    (Join-Path $env:LOCALAPPDATA 'Programs\ComfyUI'),
    (Join-Path $env:APPDATA 'ComfyUI'),
    'D:\ComfyUI', 'D:\ComfyUI_windows_portable', 'D:\LocalAI', 'D:\AI',
    'D:\_gabor-web'
)
foreach ($p in $known) {
    if (Test-Path -LiteralPath $p -PathType Container) {
        $matches.Add([pscustomobject]@{ Name=(Split-Path -Leaf $p); Path=$p; LastWriteTime=(Get-Item -LiteralPath $p).LastWriteTime })
    }
}
$directories = @($matches | Sort-Object Path -Unique)
$directories | Export-Csv (Join-Path $out 'folders.csv') -Encoding UTF8 -NoTypeInformation

# Record selected model weights/checkpoints. Filename+byte-size matches are CANDIDATES, not proven duplicates.
$extensions = @('.safetensors','.ckpt','.pt','.pth','.gguf','.ggml','.onnx','.bin')
$modelRoots = @($directories | Where-Object {
    $_.Name -match '(?i)(comfy|models|checkpoints|local.?ai|ollama|lm.?studio|llama|webui|forge|invoke)'
} | Select-Object -ExpandProperty Path -Unique)
$weights = New-Object 'System.Collections.Generic.List[object]'
$configs = New-Object 'System.Collections.Generic.List[object]'
foreach ($root in $modelRoots) {
    Write-Host "Inspecting model/config file names under $root ..."
    Get-ChildItem -LiteralPath $root -File -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object {
            if ($_.Extension.ToLowerInvariant() -in $extensions) {
                $weights.Add([pscustomobject]@{
                    File = $_.Name; Bytes = $_.Length;
                    GB = [math]::Round($_.Length / 1GB,3);
                    Modified = $_.LastWriteTime; Path=$_.FullName
                })
            } elseif ($_.Name -match '(?i)^(extra_model_paths.*\.ya?ml|config\.json|config\.yaml|model.*\.yaml|workflow.*\.json)$') {
                $configs.Add([pscustomobject]@{ Name=$_.Name; Path=$_.FullName; Modified=$_.LastWriteTime })
            }
        }
}
$weights | Sort-Object Path -Unique | Export-Csv (Join-Path $out 'weights.csv') -NoTypeInformation -Encoding UTF8
$configs | Sort-Object Path -Unique | Export-Csv (Join-Path $out 'configs-workflows.csv') -NoTypeInformation -Encoding UTF8
$weights | Group-Object File,Bytes | Where-Object { $_.Count -gt 1 } |
    ForEach-Object { $_.Group } |
    Sort-Object Path -Unique |
    Export-Csv (Join-Path $out 'potential-duplicates-NOT-CONFIRMED.csv') -NoTypeInformation -Encoding UTF8

# Running programs can indicate the active install. Access to some Process.Path properties may be denied.
Get-Process -ErrorAction SilentlyContinue |
    Where-Object { $_.ProcessName -match '(?i)(comfy|ollama|localai|lmstudio|python|llama|forge|stable)' } |
    ForEach-Object {
        [pscustomobject]@{ Name=$_.ProcessName; PID=$_.Id; Path=$_.Path }
    } | Export-Csv (Join-Path $out 'running-processes.csv') -NoTypeInformation -Encoding UTF8

@"
AFTERTIDE Local AI read-only inventory
Generated: $(Get-Date -Format s)
Roots scanned: $($roots -join ', ')
Depth: $MaxDepth
Folder candidates: $($directories.Count)
Model-like files: $($weights.Count)

IMPORTANT
- No files were changed, moved or deleted.
- The 'duplicates' CSV groups filenames+sizes ONLY. It does not prove equal content.
- Model references inside ComfyUI workflows and external YAML model paths still need manual inspection.
- An old or unused-looking folder is NOT automatically safe to delete.
- Review running processes and current working installations before quarantining anything.
- Do not commit this audit report; it may contain private file paths.
"@ | Set-Content -LiteralPath (Join-Path $out 'README.txt') -Encoding UTF8
Write-Host ""
Write-Host "Read-only audit saved to: $out"
Write-Host ("Found " + $directories.Count + " folder candidates and " + $weights.Count + " model-like files.")
