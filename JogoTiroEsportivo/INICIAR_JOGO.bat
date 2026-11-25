@echo off
title Abrindo Simulador IPSC na Unity
echo ====================================================
echo  Iniciando Unity com DirectX 11 Estavel...
echo ====================================================

taskkill /F /IM UnityCrashHandler64.exe >nul 2>&1
del /F /Q "D:\weder\IAs\Antigravit\Projetos_Antigravit\JogoTiroEsportivo\Temp\UnityLockfile" >nul 2>&1

start "" "C:\Program Files\Unity\Hub\Editor\6000.6.10f1\Editor\Unity.exe" -projectpath "D:\weder\IAs\Antigravit\Projetos_Antigravit\JogoTiroEsportivo" -force-d3d11

echo Unity iniciada! Voce ja pode fechar esta janela preta.
exit
