$processName = "SmallBusinessPOS.Web"
$processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
if ($processes) {
    $processes | Stop-Process -Force
    Write-Host "Proceso detenido: $processName"
} else {
    Write-Host "No hay procesos activos de $processName"
}
