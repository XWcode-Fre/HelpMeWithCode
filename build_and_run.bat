@echo off
echo Сборка и запуск Delta Browser...

REM Проверяем наличие .NET SDK
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo Ошибка: .NET SDK не установлен!
    echo Пожалуйста, установите .NET SDK с https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Восстанавливаем пакеты и собираем проект
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo Ошибка при восстановлении пакетов!
    pause
    exit /b 1
)

dotnet build --configuration Release
if %ERRORLEVEL% neq 0 (
    echo Ошибка при сборке проекта!
    pause
    exit /b 1
)

echo Запуск браузера...
dotnet run --configuration Release
pause 