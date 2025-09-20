@echo off
echo [Noxon Executor Build System]
echo.

REM Check if Visual Studio is installed
where msbuild >nul 2>&1
if errorlevel 1 (
    echo ERROR: MSBuild not found. Install Visual Studio with C++ support.
    pause
    exit /b 1
)

echo Cleaning previous builds...
if exist "bin" rmdir /s /q "bin"
if exist "Launcher\bin" rmdir /s /q "Launcher\bin"
if exist "Launcher\obj" rmdir /s /q "Launcher\obj"
if exist "Injector\Debug" rmdir /s /q "Injector\Debug"
if exist "Injector\Release" rmdir /s /q "Injector\Release"
if exist "API\Debug" rmdir /s /q "API\Debug"
if exist "API\Release" rmdir /s /q "API\Release"

echo Building Injector (x64 Release)...
msbuild Injector\Injector.vcxproj /p:Configuration=Release /p:Platform=x64 /p:PlatformToolset=v143 /verbosity:minimal
if errorlevel 1 (
    echo ERROR: Failed to build Injector
    pause
    exit /b 1
)

echo Building API (x64 Release)...
msbuild API\API.vcxproj /p:Configuration=Release /p:Platform=x64 /p:PlatformToolset=v143 /verbosity:minimal
if errorlevel 1 (
    echo ERROR: Failed to build API
    pause
    exit /b 1
)

echo Building Launcher (Release)...
msbuild Launcher\Launcher.csproj /p:Configuration=Release /p:Platform=AnyCPU /verbosity:minimal
if errorlevel 1 (
    echo ERROR: Failed to build Launcher
    pause
    exit /b 1
)

echo Creating output directory...
if not exist "bin" mkdir "bin"

echo Copying files...
copy "Launcher\bin\Release\NoxonExecutor.exe" "bin\" >nul
copy "Injector\x64\Release\Injector.dll" "bin\" >nul
copy "API\x64\Release\API.dll" "bin\" >nul

echo Copying dependencies...
REM Copy Detours and other required DLLs if needed

echo.
echo Build completed successfully!
echo Files are in the 'bin' directory.
echo.
pause
