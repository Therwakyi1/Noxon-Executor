@echo off
chcp 65001 >nul
echo [Noxon Executor] Lua 5.4.4 File Copier
echo =====================================
echo.

if not exist "lua-5.4.4\src\lua.h" (
    echo ERROR: Lua 5.4.4 source not found!
    echo.
    echo Make sure you:
    echo 1. Downloaded from: https://www.lua.org/ftp/lua-5.4.4.tar.gz
    echo 2. Extracted to: NoxonExecutor\lua-5.4.4\
    echo 3. The path exists: NoxonExecutor\lua-5.4.4\src\lua.h
    echo.
    echo Current directory: %CD%
    if exist "lua-5.4.4" (
        echo lua-5.4.4 exists: YES
        dir "lua-5.4.4" /b
    ) else (
        echo lua-5.4.4 exists: NO
    )
    echo.
    pause
    exit /b 1
)

echo Found Lua 5.4.4 source files
echo.

if not exist "lib\lua" (
    echo Creating lib\lua directory...
    mkdir "lib\lua"
)

echo Cleaning destination directory...
del "lib\lua\*.*" /q 2>nul

echo Copying header files...
copy "lua-5.4.4\src\*.h" "lib\lua\" >nul
if errorlevel 1 (
    echo Failed to copy header files!
    pause
    exit /b 1
)

echo Copying source files...
copy "lua-5.4.4\src\*.c" "lib\lua\" >nul
if errorlevel 1 (
    echo Failed to copy source files!
    pause
    exit /b 1
)

echo Removing unnecessary files...
del "lib\lua\lua.c" 2>nul
del "lib\lua\luac.c" 2>nul

echo.
echo Verification:
echo    Source: lua-5.4.4\src\
echo    Destination: lib\lua\
echo.

echo Files copied:
dir "lib\lua" /b

echo.
echo File counts:
echo    Headers (.h): 
for /f %%i in ('dir "lib\lua\*.h" 2^>nul ^| find "File(s)"') do echo        %%i
echo    Sources (.c): 
for /f %%i in ('dir "lib\lua\*.c" 2^>nul ^| find "File(s)"') do echo        %%i

echo.
echo IMPORTANT: Now update your API.vcxproj to include these files!
echo    Add all .h and .c files from lib\lua\ to your project
echo.
echo Lua setup complete! Now run Build.bat
echo.
pause
