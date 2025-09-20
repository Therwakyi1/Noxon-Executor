#include "Injector.h"
#include <Windows.h>
#include <TlHelp32.h>
#include <iostream>
#include <string>
#include <vector>

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
    // Read the DLL file into memory
    HANDLE hFile = CreateFileA(dllPath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile == INVALID_HANDLE_VALUE) {
        SetLastError("Failed to open DLL file: " + std::to_string(GetLastError()));
        return false;
    }

    DWORD fileSize = GetFileSize(hFile, NULL);
    if (fileSize == INVALID_FILE_SIZE) {
        SetLastError("Failed to get file size: " + std::to_string(GetLastError()));
        CloseHandle(hFile);
        return false;
    }

    BYTE* pDllData = new BYTE[fileSize];
    DWORD bytesRead;
    if (!ReadFile(hFile, pDllData, fileSize, &bytesRead, NULL)) {
        SetLastError("Failed to read DLL file: " + std::to_string(GetLastError()));
        delete[] pDllData;
        CloseHandle(hFile);
        return false;
    }
    CloseHandle(hFile);

    // Basic manual mapping implementation
    // This is a simplified version - real manual mapping is much more complex

    HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processId);
    if (!hProcess) {
        SetLastError("Failed to open process: " + std::to_string(GetLastError()));
        delete[] pDllData;
        return false;
    }

    // TODO: Parse PE headers, allocate memory, copy sections, apply relocations, etc.
    // This is where the actual manual mapping would happen

    delete[] pDllData;
    CloseHandle(hProcess);

    SetLastError("Manual mapping not fully implemented yet");
    return false;
}

// Thread hijack injection
bool ThreadHijackInject(int processId, const char* dllPath) {
    // Find a thread to hijack
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) {
        SetLastError("Failed to create thread snapshot: " + std::to_string(GetLastError()));
        return false;
    }

    THREADENTRY32 te;
    te.dwSize = sizeof(THREADENTRY32);

    DWORD targetThreadId = 0;
    if (Thread32First(hSnapshot, &te)) {
        do {
            if (te.th32OwnerProcessID == processId) {
                targetThreadId = te.th32ThreadID;
                break;
            }
        } while (Thread32Next(hSnapshot, &te));
    }
    CloseHandle(hSnapshot);

    if (targetThreadId == 0) {
        SetLastError("No threads found in target process");
        return false;
    }

    // TODO: Implement thread hijacking
    // This involves suspending the thread, modifying its context to execute your code,
    // then restoring it

    SetLastError("Thread hijack injection not implemented yet");
    return false;
}

// Check if Hyperion is running
bool IsHyperionRunning() {
    // Check for Hyperion driver presence
    HANDLE hDevice = CreateFileA("\\\\.\\HyperionDriver", 
                                GENERIC_READ, 
                                FILE_SHARE_READ | FILE_SHARE_WRITE, 
                                NULL, 
                                OPEN_EXISTING, 
                                FILE_ATTRIBUTE_NORMAL, 
                                NULL);
    
    if (hDevice != INVALID_HANDLE_VALUE) {
        CloseHandle(hDevice);
        return true;
    }
    
    // Check for Roblox processes (indicating Hyperion might be active)
    const char* robloxProcesses[] = {
        "RobloxPlayerBeta.exe",
        "Windows10Universal.exe",
        "RobloxWebHelper.exe"
    };
    
    for (const char* procName : robloxProcesses) {
        if (FindProcessByName(procName) != -1) {
            return true;
        }
    }
    
    return false;
}

// Hyperion-aware injection
bool HyperionAwareInject(int processId, const char* dllPath) {
    // 1. Check if Hyperion is present
    if (IsHyperionRunning()) {
        std::cout << "[INJECTOR] Hyperion detected - using manual mapping\n";
        return ManualMapInject(processId, dllPath);
    }
    
    // 2. Fall back to normal injection
    std::cout << "[INJECTOR] Hyperion not detected - using LoadLibrary\n";
    return InjectLoadLibrary(processId, dllPath);
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

    // Use Hyperion-aware injection
    return HyperionAwareInject(processId, dllPath);
}

// Stealth injection function
extern "C" INJECTOR_API bool STDCALL InjectStealth(int processId, const char* dllPath) {
    return HyperionAwareInject(processId, dllPath);
}

// Advanced injection with configuration
extern "C" INJECTOR_API bool STDCALL AdvancedInject(int processId, const char* dllPath, InjectorConfig config) {
    switch (config.method) {
        case INJECT_METHOD_LOADLIBRARY:
            return InjectLoadLibrary(processId, dllPath);
        case INJECT_METHOD_MANUALMAP:
            return ManualMapInject(processId, dllPath);
        case INJECT_METHOD_THREADHIJACK:
            return ThreadHijackInject(processId, dllPath);
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
        DWORD exitCode;
        if (GetExitCodeProcess(hProcess, &exitCode) && exitCode == STILL_ACTIVE) {
            CloseHandle(hProcess);
            return true;
        }
        CloseHandle(hProcess);
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

// Get process name from ID
extern "C" INJECTOR_API bool STDCALL GetProcessName(int processId, char* buffer, int bufferSize) {
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) {
        return false;
    }

    PROCESSENTRY32 pe;
    pe.dwSize = sizeof(PROCESSENTRY32);

    if (!Process32First(hSnapshot, &pe)) {
        CloseHandle(hSnapshot);
        return false;
    }

    do {
        if (pe.th32ProcessID == processId) {
            strncpy_s(buffer, bufferSize, pe.szExeFile, _TRUNCATE);
            CloseHandle(hSnapshot);
            return true;
        }
    } while (Process32Next(hSnapshot, &pe));

    CloseHandle(hSnapshot);
    return false;
}

// Get list of all running processes
extern "C" INJECTOR_API int STDCALL GetProcessList(int* processIds, char** processNames, int maxCount) {
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) {
        return 0;
    }

    PROCESSENTRY32 pe;
    pe.dwSize = sizeof(PROCESSENTRY32);

    if (!Process32First(hSnapshot, &pe)) {
        CloseHandle(hSnapshot);
        return 0;
    }

    int count = 0;
    do {
        if (count < maxCount) {
            processIds[count] = pe.th32ProcessID;
            processNames[count] = _strdup(pe.szExeFile); // Caller must free this memory
            count++;
        }
    } while (Process32Next(hSnapshot, &pe) && count < maxCount);

    CloseHandle(hSnapshot);
    return count;
}
