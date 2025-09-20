#pragma once
#ifndef LUA_ENGINE_H
#define LUA_ENGINE_H

#include <string>
#include <vector>
#include <queue>
#include <thread>
#include <mutex>
#include <atomic>

// Lua state types forward declaration
struct lua_State;

class LuaEngine
{
public:
    LuaEngine();
    ~LuaEngine();

    // Initialization
    bool Initialize();
    bool IsInitialized() const;

    // Script execution
    void Execute(const std::string& code);
    void ExecuteFile(const std::string& filePath);
    
    // Queue management
    void AddToQueue(const std::string& code);
    void ProcessQueue();
    void ClearQueue();
    size_t GetQueueSize() const;

    // Roblox-specific functions
    bool HookRobloxLua();
    bool UnhookRobloxLua();
    
    // Utility functions
    void SetPrintCallback(void(*callback)(const char*));
    void SetErrorCallback(void(*callback)(const char*));

    // Memory safety
    void SetMemorySafety(bool enabled);
    bool GetMemorySafety() const;

private:
    // Lua state management
    lua_State* GetRobloxLuaState();
    bool CreateLuaState();
    void DestroyLuaState();

    // Internal execution
    bool ExecuteInternal(const std::string& code);
    void SafeExecute(const std::string& code);

    // Hook functions
    static int HookedPrint(lua_State* L);
    static int HookedLoadString(lua_State* L);
    static int HookedRequire(lua_State* L);

    // Thread safety
    mutable std::mutex m_queueMutex;
    mutable std::mutex m_stateMutex;

    // Execution queue
    std::queue<std::string> m_executionQueue;
    
    // Lua state
    lua_State* m_luaState;
    lua_State* m_robloxState;
    
    // Hooks
    void* m_originalPrint;
    void* m_originalLoadString;
    void* m_originalRequire;
    
    // Callbacks
    void(*m_printCallback)(const char*);
    void(*m_errorCallback)(const char*);
    
    // Flags
    std::atomic<bool> m_initialized;
    std::atomic<bool> m_hooked;
    std::atomic<bool> m_memorySafety;
    std::atomic<bool> m_running;

    // Thread for queue processing
    std::thread m_queueThread;

    // Roblox function addresses (these would be found through pattern scanning)
    uintptr_t m_robloxPrintAddr;
    uintptr_t m_robloxLoadStringAddr;
    uintptr_t m_robloxRequireAddr;
    uintptr_t m_robloxLuaStateAddr;
};

// C interface for external calls
extern "C" {
    __declspec(dllexport) LuaEngine* CreateLuaEngine();
    __declspec(dllexport) void DestroyLuaEngine(LuaEngine* engine);
    __declspec(dllexport) void ExecuteLuaCode(LuaEngine* engine, const char* code);
    __declspec(dllexport) bool IsLuaEngineReady(LuaEngine* engine);
    __declspec(dllexport) void ClearExecutionQueue(LuaEngine* engine);
}

#endif // LUA_ENGINE_H
