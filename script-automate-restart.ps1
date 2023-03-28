$programPath = "C:\Program Files (x86)\Irium Software\CRON SyncJiraAzure\CronSynchroJiraAzure.exe"
$logFile = "C:\Program Files (x86)\Irium Software\CRON SyncJiraAzure\program-restart-log.txt"

# Vérifie si le programme est en cours d'exécution
if (Get-Process -Name "CronSynchroJiraAzure" -ErrorAction SilentlyContinue) {
    # Le programme est en cours d'exécution, donc on écrit la date, l'heure et l'état dans le fichier log
    $status = "running"
    $date = Get-Date
    $logLine = "$date : Program is $status`n"
    Add-Content -Path $logFile -Value $logLine
}
else {
    # Le programme n'est pas en cours d'exécution, donc on le relance et on écrit la date, l'heure et l'état dans le fichier log
    $status = "stopped"
    Start-Process -FilePath $programPath
    $date = Get-Date
    $logLine = "$date : Program was $status, restarted`n"
    Add-Content -Path $logFile -Value $logLine
}