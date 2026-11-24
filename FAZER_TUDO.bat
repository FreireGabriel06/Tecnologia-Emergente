@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Sebraelab VR - configurar o projeto e gravar o video do jogo
cd /d "%~dp0"
set "PROJETO=%CD%"

echo.
echo  ============================================================
echo   Sebraelab VR - faz tudo sozinho na Unity:
echo     1. atualiza o projeto (git pull)
echo     2. configura o projeto e cria a cena (sem abrir janela)
echo     3. abre a Unity, joga sozinho e grava o video do jogo
echo   Feche a Unity antes de continuar. Leva de 5 a 20 minutos.
echo  ============================================================
echo.
pause

if not exist "%PROJETO%\ProjectSettings\ProjectVersion.txt" (
  echo Este arquivo precisa ficar na pasta do projeto, a que tem Assets e ProjectSettings.
  pause
  exit /b 1
)
if not exist "%PROJETO%\Gravacoes" mkdir "%PROJETO%\Gravacoes"

echo.
echo [1/3] Atualizando o projeto...
where git >nul 2>nul
if errorlevel 1 (
  echo   Git nao encontrado: seguindo com os arquivos que ja estao na pasta.
) else (
  git pull --ff-only
  if errorlevel 1 echo   Nao deu para atualizar ^(sem internet ou com mudancas locais^): seguindo assim mesmo.
)

echo.
echo [2/3] Procurando a Unity 6...
set "VERSAO="
for /f "tokens=2" %%v in ('findstr /b /c:"m_EditorVersion:" "%PROJETO%\ProjectSettings\ProjectVersion.txt"') do set "VERSAO=%%v"
set "UNITY="
call :procurar "%ProgramFiles%\Unity\Hub\Editor"
rem Outra pasta de instalacao escolhida no Unity Hub, em Preferences, Installs
if not defined UNITY if exist "%APPDATA%\UnityHub\secondaryInstallPath.json" (
  for /f "usebackq delims=" %%p in (`powershell -NoProfile -Command "Get-Content -Raw '%APPDATA%\UnityHub\secondaryInstallPath.json' | ConvertFrom-Json"`) do call :procurar "%%p"
)
if not defined UNITY (
  echo   Nao achei a Unity 6 instalada pelo Unity Hub.
  set /p "UNITY=  Arraste aqui o arquivo Unity.exe e aperte Enter: "
  set "UNITY=!UNITY:"=!"
)
if not exist "%UNITY%" (
  echo   Unity.exe nao encontrado. Instale a Unity 6 pelo Unity Hub e rode de novo.
  pause
  exit /b 1
)
echo   Usando: %UNITY%

echo.
echo [3/3] a) Configurando o projeto (sem janela). Acompanhe em Gravacoes\1_configurar.log
start "" /wait "%UNITY%" -batchmode -projectPath "%PROJETO%" -executeMethod SebraelabAutomatico.Configurar -logFile "%PROJETO%\Gravacoes\1_configurar.log"
set "CODIGO=%errorlevel%"
if not "%CODIGO%"=="0" (
  echo.
  echo   A configuracao nao terminou ^(codigo %CODIGO%^). Veja o fim de Gravacoes\1_configurar.log
  echo   Se o log falar de licenca, abra o Unity Hub, entre na sua conta e rode de novo.
  pause
  exit /b %CODIGO%
)
echo   Projeto configurado.

echo.
echo [3/3] b) Gravando o video do jogo. A Unity vai abrir, jogar sozinha e fechar.
echo        Nao mexa no mouse nem no teclado enquanto ela grava.
if exist "%PROJETO%\Gravacoes\Sebraelab_demo.mp4" del /q "%PROJETO%\Gravacoes\Sebraelab_demo.mp4"
start "" /wait "%UNITY%" -projectPath "%PROJETO%" -executeMethod SebraelabAutomatico.GravarDemonstracao -logFile "%PROJETO%\Gravacoes\2_gravar.log"
set "CODIGO=%errorlevel%"

echo.
if exist "%PROJETO%\Gravacoes\Sebraelab_demo.mp4" (
  echo  Pronto^^! Video do jogo: !PROJETO!\Gravacoes\Sebraelab_demo.mp4
  echo  Os logs ficaram em Gravacoes\1_configurar.log e Gravacoes\2_gravar.log
  explorer /select,"%PROJETO%\Gravacoes\Sebraelab_demo.mp4"
) else (
  echo  O video nao foi gerado ^(codigo %CODIGO%^). Veja o fim de Gravacoes\2_gravar.log
)
echo.
pause
exit /b %CODIGO%

:procurar
rem %1 = pasta com as versoes da Unity (uma subpasta por versao)
if defined UNITY exit /b 0
if not exist "%~1" exit /b 0
if defined VERSAO if exist "%~1\%VERSAO%\Editor\Unity.exe" (
  set "UNITY=%~1\%VERSAO%\Editor\Unity.exe"
  exit /b 0
)
for /d %%d in ("%~1\6000.*") do (
  if exist "%%~d\Editor\Unity.exe" set "UNITY=%%~d\Editor\Unity.exe"
)
exit /b 0
