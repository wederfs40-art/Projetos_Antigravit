@echo off
title Abrindo Simulador IPSC na Unity (Modo Estavel DirectX 11)
echo ====================================================
echo  Limpando processos e travas do Unity...
echo ====================================================

taskkill /F /IM UnityCrashHandler64.exe >nul 2>&1
del /F /Q "%~dp0Temp\UnityLockfile" >nul 2>&1

echo ====================================================
echo  Iniciando Unity com DirectX 11 Estavel (-force-d3d11)...
echo ====================================================

start "" "C:\Program Files\Unity\Hub\Editor\6000.6.10f1\Editor\Unity.exe" -projectpath "%~dp0." -force-d3d11

echo Unity iniciada em modo estavel! Voce ja pode fechar esta janela.
exit
