#include "Injector.h"
#include <Windows.h>
#include <TlHelp32.h>
#include <iostream>
#include <string>

// Global variables for error tracking
std::string g_lastError;

// Utility function to set error message
void SetLastError(const std::string& error) {
    g_lastError = error;
    std::cerr << "[INJECTOR ERROR] " << error << std::endl;
}

// Basic injection using LoadLibrary
bool InjectLoadLibrary(int processId, const char* dllPath) {
    HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processId);
    if (!hProcess) {
        SetLastError("Failed to open process: " + std::to_string(GetLastError()));
        return false;
    }

    // Allocate memory in the target process for the DLL path
    LPVOID pDllPath = VirtualAllocEx(hProcess, NULL, strlen(dllPath) + 1, 
                                    MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    if (!pDllPath) {
        SetLastError("Failed to allocate memory: " + std::to_string(GetLastError()));
        CloseHandle(hProcess);
        return false;
    }

    // Write the DLL path to the allocated memory
    if (!WriteProcessMemory(hProcess, pDllPath, dllPath, strlen(dllPath) + 1, NULL)) {
        SetLastError("Failed to write process memory: " + std::to_string(GetLastError()));
        VirtualFreeEx(hProcess, pDllPath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }

    // Get address of LoadLibraryA in kernel32.dll
    LPTHREAD_START_ROUTINE pLoadLibrary = (LPTHREAD_START_ROUTINE)
        GetProcAddress(GetModuleHandleA("kernel32.dll"), "LoadLibraryA");
    
    if (!pLoadLibrary) {
        SetLastError("Failed to get LoadLibraryA address: " + std::to_string(GetLastError()));
        VirtualFreeEx(hProcess, pDllPath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }

    // Create remote thread that calls LoadLibraryA with our DLL path
    HANDLE hThread = CreateRemoteThread(hProcess, NULL, 0, pLoadLibrary, pDllPath, 0, NULL);
    if (!hThread) {
        SetLastError("Failed to create remote thread: " + std::to_string(GetLastError()));
        VirtualFreeEx(hProcess, pDllPath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }

    // Wait for the thread to finish
    WaitForSingleObject(hThread, INFINITE);

    // Cleanup
    VirtualFreeEx(hProcess, pDllPath, 0, MEM_RELEASE);
    CloseHandle(hThread);
    CloseHandle(hProcess);

    return true;
}

// Manual mapping injection (more advanced, harder to detect)
bool ManualMapInject(int processId, const char* dllPath) {
    // This is where you'd implement manual DLL mapping
    // It's complex as fuck and requires parsing PE headers
    // For now, we'll just use LoadLibrary
    SetLastError("Manual mapping not implemented yet");
    return false;
}

// Main injection function
extern "C" INJECTOR_API bool STDCALL Inject(int processId, const char* dllPath) {
    if (processId <= 0 || !dllPath) {
        SetLastError("Invalid parameters");
        return false;
    }

    // Check if the process exists
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, processId);
    if (!hProcess) {
        SetLastError("Process not found: " + std::to_string(processId));
        return false;
    }
    CloseHandle(hProcess);

    // Try to inject using LoadLibrary method
    return InjectLoadLibrary(processId, dllPath);
}

// Advanced injection with configuration
extern "C" INJECTOR_API bool STDCALL AdvancedInject(int processId, const char* dllPath, InjectorConfig config) {
    switch (config.method) {
        case INJECT_METHOD_LOADLIBRARY:
            return InjectLoadLibrary(processId, dllPath);
        case INJECT_METHOD_MANUALMAP:
            return ManualMapInject(processId, dllPath);
        case INJECT_METHOD_THREADHIJACK:
            SetLastError("Thread hijack injection not implemented");
            return false;
        default:
            SetLastError("Unknown injection method");
            return false;
    }
}

// Utility functions
extern "C" INJECTOR_API const char* STDCALL GetLastErrorMsg() {
    return g_lastError.c_str();
}

extern "C" INJECTOR_API bool STDCALL IsProcessRunning(int processId) {
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, processId);
    if (hProcess) {
        CloseHandle(hProcess);
        return true;
    }
    return false;
}

extern "C" INJECTOR_API int STDCALL FindProcessByName(const char* processName) {
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) {
        return -1;
    }

    PROCESSENTRY32 pe;
    pe.dwSize = sizeof(PROCESSENTRY32);

    if (!Process32First(hSnapshot, &pe)) {
        CloseHandle(hSnapshot);
        return -1;
    }

    do {
        if (_stricmp(pe.szExeFile, processName) == 0) {
            CloseHandle(hSnapshot);
            return pe.th32ProcessID;
        }
    } while (Process32Next(hSnapshot, &pe));

    CloseHandle(hSnapshot);
    return -1;
}
