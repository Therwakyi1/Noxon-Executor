#pragma once
#ifndef INJECTOR_H
#define INJECTOR_H

// Define the DLL export/import macros for Windows
#ifdef _WIN32
    #ifdef INJECTOR_EXPORTS
        #define INJECTOR_API __declspec(dllexport)
    #else
        #define INJECTOR_API __declspec(dllimport)
    #endif
#else
    #define INJECTOR_API
#endif

// Define calling convention
#define STDCALL __stdcall

// External "C" to prevent C++ name mangling
extern "C" {
    // Main injection function - this is what your C# code will call
    INJECTOR_API bool STDCALL Inject(int processId, const char* dllPath);
    
    // Utility functions
    INJECTOR_API const char* STDCALL GetLastErrorMsg();
    INJECTOR_API bool STDCALL IsProcessRunning(int processId);
    INJECTOR_API int STDCALL FindProcessByName(const char* processName);
}

// Injection methods enum
enum InjectionMethod {
    INJECT_METHOD_LOADLIBRARY = 0,
    INJECT_METHOD_MANUALMAP,
    INJECT_METHOD_THREADHIJACK
};

// Configuration structure
struct InjectorConfig {
    InjectionMethod method;
    bool stealthMode;
    bool bypassAntiCheat;
    unsigned int timeoutMs;
};

// Advanced injection function with configuration
INJECTOR_API bool STDCALL AdvancedInject(int processId, const char* dllPath, InjectorConfig config);

#endif // INJECTOR_H
