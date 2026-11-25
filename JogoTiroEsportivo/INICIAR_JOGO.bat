@echo off
title Abrindo Simulador IPSC na Unity (Unity 6 Oficial Estavel)
echo ====================================================
echo  Limpando processos e travas residuais...
echo ====================================================

taskkill /F /IM UnityCrashHandler64.exe >nul 2>&1
del /F /Q "%~dp0Temp\UnityLockfile" >nul 2>&1

echo ====================================================
echo  Iniciando Unity 6 Estavel (6000.3.29f1) com DirectX 11...
echo ====================================================

start "" "C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe" -projectpath "%~dp0." -force-d3d11

echo Unity iniciada! Voce ja pode fechar esta janela.
exit
