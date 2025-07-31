@echo off

start cmd /k "cd Registry && dotnet run --launch-profile https"
timeout /t 5 /nobreak
start cmd /k "cd server1 && dotnet run"
start cmd /k "cd Server2 && dotnet run"
start cmd /k "cd Server3 && dotnet run"
start cmd /k "cd Orchestrator && dotnet run"
timeout /t 5 /nobreak
start cmd /k "cd Client && dotnet run"