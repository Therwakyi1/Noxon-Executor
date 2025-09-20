@echo off
echo [Noxon Executor] Copying Lua 5.4.4 files...
echo.

REM Check if source exists
if not exist "lua-5.4.4\src\lua.h" (
    echo ERROR: Lua source not found!
    echo.
    echo 1. Download lua-5.4.4.tar.gz from:
    echo    https://www.lua.org/ftp/lua-5.4.4.tar.gz
    echo.
    echo 2. Extract it to: NoxonExecutor\lua-5.4.4\
    echo    So the path is: NoxonExecutor\lua-5.4.4\src\lua.h
    echo.
    pause
    exit /b 1
)

REM Create destination directory
if not exist "lib\lua" (
    echo Creating lib\lua directory...
    mkdir "lib\lua"
)

REM Copy ALL header files
echo Copying header files...
copy "lua-5.4.4\src\*.h" "lib\lua\" >nul

REM Copy ALL source files  
echo Copying source files...
copy "lua-5.4.4\src\*.c" "lib\lua\" >nul

REM Remove files we don't need for embedding
echo Cleaning up unnecessary files...
del "lib\lua\lua.c" 2>nul
del "lib\lua\luac.c" 2>nul

echo.
echo ✅ Lua files successfully copied to:
echo    NoxonExecutor\lib\lua\
echo.
echo 📁 Files copied:
dir "lib\lua" /b
echo.
pause
