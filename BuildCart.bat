@echo off
setlocal
title (not)SEGA BIRTH - Factory Assembler

:: --- CONFIGURATION ---
set ASSEMBLER=builder.py
set SOURCE=game.asm
set SPRITE=hero.png
set OUTPUT=game.BGF

echo [SYSTEM] Initializing Factory...

:: Remove old build to ensure fresh compilation
if exist %OUTPUT% del %OUTPUT%

echo [SYSTEM] Compiling %SOURCE% and packing %SPRITE%...

:: Run the Python builder
python %ASSEMBLER% %SOURCE% %SPRITE%

:: Validation
if exist %OUTPUT% (
    echo [SUCCESS] Cartridge %OUTPUT% created. 
    echo [INFO] Ready for deployment to Silicon.
) else (
    echo [ERROR] Assembly failed. Check source files for syntax errors.
    pause
)

endlocal