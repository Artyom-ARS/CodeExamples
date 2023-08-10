@echo off 
set executable=C:\temp\start_program.bat 
set process=HistoryPlatform.exe 
:begin 
tasklist |>nul findstr /b /l /i /c:%process% || start "" "%executable%" 
timeout /t 45 /nobreak >nul 
goto :begin