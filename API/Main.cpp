#include <Windows.h>
#include <iostream>
#include <thread>
#include <chrono>
#include "LuaEngine.h"

// Global variables
HANDLE g_hThread = NULL;
bool g_bRunning = true;
LuaEngine* g_pLuaEngine = nullptr;

// Forward declarations
void MainThread();
void SetupHooks();
void CleanupHooks();

// DLL entry point
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        {
            // Disable thread notifications for performance
            DisableThreadLibraryCalls(hModule);
            
            // Create our main thread
            g_hThread = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)MainThread, hModule, 0, NULL);
            if (g_hThread) {
                CloseHandle(g_hThread);
            }
        }
        break;
        
    case DLL_PROCESS_DETACH:
        {
            // Cleanup
            g_bRunning = false;
            if (g_pLuaEngine) {
                delete g_pLuaEngine;
                g_pLuaEngine = nullptr;
            }
            CleanupHooks();
        }
        break;
    }
    return TRUE;
}

// Main thread function
void MainThread()
{
    // Wait for Roblox to fully initialize
    std::this_thread::sleep_for(std::chrono::seconds(3));
    
    // Initialize console for debugging
    AllocConsole();
    FILE* f;
    freopen_s(&f, "CONOUT$", "w", stdout);
    std::cout << "[Noxon API] Injected successfully!" << std::endl;
    std::cout << "[Noxon API] Setting up Lua environment..." << std::endl;
    
    try {
        // Create Lua engine
        g_pLuaEngine = new LuaEngine();
        
        // Setup function hooks
        SetupHooks();
        
        std::cout << "[Noxon API] Ready for execution!" << std::endl;
        
        // Main loop
        while (g_bRunning) {
            // Process execution queue
            if (g_pLuaEngine) {
                g_pLuaEngine->ProcessQueue();
            }
            
            // Sleep to prevent CPU hogging
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
        }
    }
    catch (const std::exception& e) {
        std::cout << "[Noxon API] Exception: " << e.what() << std::endl;
    }
    
    // Cleanup console
    if (f) fclose(f);
    FreeConsole();
}

// Placeholder for hook setup
void SetupHooks()
{
    std::cout << "[Noxon API] Setting up function hooks..." << std::endl;
    
    // This is where you'd hook into:
    // 1. Roblox's Lua state creation
    // 2. Script execution functions
    // 3. Game-specific functions
    
    // For now, we just print a message
    std::cout << "[Noxon API] Hooks installed (placeholder)" << std::endl;
}

// Placeholder for hook cleanup
void CleanupHooks()
{
    std::cout << "[Noxon API] Cleaning up hooks..." << std::endl;
    
    // This is where you'd restore original functions
    // and remove any installed hooks
}

// Function to execute Lua code (called from external sources)
extern "C" __declspec(dllexport) void ExecuteLuaCode(const char* code)
{
    if (g_pLuaEngine && code) {
        g_pLuaEngine->Execute(code);
    }
}

// Function to get Lua engine status
extern "C" __declspec(dllexport) bool IsLuaEngineReady()
{
    return g_pLuaEngine != nullptr;
}

// Function to clear execution queue
extern "C" __declspec(dllexport) void ClearQueue()
{
    if (g_pLuaEngine) {
        g_pLuaEngine->ClearQueue();
    }
}
