@echo off
cd /d "%~dp0"
if not exist PromptStudio.exe powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
start "" "%~dp0PromptStudio.exe"
