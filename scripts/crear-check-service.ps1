@'
param([string]$Port = "5000")

Write-Host "=== Estado FichaCosto Service ===" -ForegroundColor Cyan

# Estado servicio
$svc = Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue
if ($svc) {
    Write-Host "Servicio: $($svc.Status)" -ForegroundColor $(if($svc.Status -eq "Running"){"Green"}else{"Red"})
} else {
    Write-Host "Servicio: No instalado" -ForegroundColor Red
}

# Proceso
$proc = Get-Process -Name "FichaCosto.Service" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Proceso: PID $($proc.Id), $([math]::Round($proc.WorkingSet64/1MB,2)) MB" -ForegroundColor Green
}

# Endpoint
try {
    $r = Invoke-RestMethod -Uri "http://localhost:$Port/api/health" -TimeoutSec 3
    Write-Host "API: ✅ $($r.status) v$($r.version)" -ForegroundColor Green
} catch {
    Write-Host "API: ❌ No responde" -ForegroundColor Red
}

# Logs
$logPath = "C:\Program Files\FichaCostoService\Logs"
if (Test-Path $logPath) {
    $lastLog = Get-ChildItem $logPath -Filter "*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($lastLog) {
        Write-Host "Último log: $($lastLog.Name) ($(($lastLog.LastWriteTime).ToString('HH:mm:ss')))" -ForegroundColor Gray
    }
}
'@ | Out-File -FilePath "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\scripts\check-service.ps1" -Encoding utf8