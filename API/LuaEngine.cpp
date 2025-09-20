#include "LuaEngine.h"
#include <Windows.h>
#include <iostream>
#include <fstream>
#include <chrono>
#include <thread>
#include <Psapi.h>

// Lua includes
extern "C" {
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
}

// Memory scanning utility
uintptr_t PatternScan(const char* module, const char* pattern, const char* mask) {
    MODULEINFO moduleInfo = { 0 };
    HMODULE hModule = GetModuleHandleA(module);
    
    if (!hModule)
        return 0;
        
    GetModuleInformation(GetCurrentProcess(), hModule, &moduleInfo, sizeof(MODULEINFO));
    
    uintptr_t base = (uintptr_t)moduleInfo.lpBaseOfDll;
    uintptr_t size = moduleInfo.SizeOfImage;
    
    uintptr_t patternLength = strlen(mask);
    
    for (uintptr_t i = 0; i < size - patternLength; i++) {
        bool found = true;
        
        for (uintptr_t j = 0; j < patternLength; j++) {
            if (mask[j] != '?' && pattern[j] != *(char*)(base + i + j)) {
                found = false;
                break;
            }
        }
        
        if (found) {
            return base + i;
        }
    }
    
    return 0;
}

// LuaEngine implementation
LuaEngine::LuaEngine() 
    : m_luaState(nullptr), 
      m_robloxState(nullptr),
      m_originalPrint(nullptr),
      m_originalLoadString(nullptr),
      m_originalRequire(nullptr),
      m_printCallback(nullptr),
      m_errorCallback(nullptr),
      m_initialized(false),
      m_hooked(false),
      m_memorySafety(true),
      m_running(false),
      m_robloxPrintAddr(0),
      m_robloxLoadStringAddr(0),
      m_robloxRequireAddr(0),
      m_robloxLuaStateAddr(0)
{
    m_queueThread = std::thread(&LuaEngine::ProcessQueue, this);
}

LuaEngine::~LuaEngine() {
    m_running = false;
    if (m_queueThread.joinable()) {
        m_queueThread.join();
    }
    
    UnhookRobloxLua();
    DestroyLuaState();
}

bool LuaEngine::Initialize() {
    if (m_initialized) return true;

    std::cout << "[LuaEngine] Initializing..." << std::endl;

    // Find Roblox Lua functions using pattern scanning
    m_robloxPrintAddr = PatternScan(nullptr, "\x55\x8B\xEC\x6A\xFF", "xxxxx"); // Example pattern
    m_robloxLoadStringAddr = PatternScan(nullptr, "\x8B\x4D\x08\x85\xC9", "xxxxx");
    m_robloxRequireAddr = PatternScan(nullptr, "\x83\xEC\x08\x53\x56\x57", "xxxxxx");
    
    if (!m_robloxPrintAddr || !m_robloxLoadStringAddr || !m_robloxRequireAddr) {
        std::cout << "[LuaEngine] Failed to find Roblox functions!" << std::endl;
        return false;
    }

    std::cout << "[LuaEngine] Found Roblox functions:" << std::endl;
    std::cout << "  Print: 0x" << std::hex << m_robloxPrintAddr << std::endl;
    std::cout << "  LoadString: 0x" << std::hex << m_robloxLoadStringAddr << std::endl;
    std::cout << "  Require: 0x" << std::hex << m_robloxRequireAddr << std::endl;

    // Create our own Lua state for safe execution
    if (!CreateLuaState()) {
        std::cout << "[LuaEngine] Failed to create Lua state!" << std::endl;
        return false;
    }

    m_initialized = true;
    std::cout << "[LuaEngine] Initialized successfully!" << std::endl;
    
    return true;
}

bool LuaEngine::CreateLuaState() {
    m_luaState = luaL_newstate();
    if (!m_luaState) {
        return false;
    }

    // Open standard libraries
    luaL_openlibs(m_luaState);
    
    return true;
}

void LuaEngine::DestroyLuaState() {
    if (m_luaState) {
        lua_close(m_luaState);
        m_luaState = nullptr;
    }
}

bool LuaEngine::HookRobloxLua() {
    if (m_hooked) return true;

    std::cout << "[LuaEngine] Hooking not available - Detours not installed" << std::endl;
    return false; // Detours not available
}

bool LuaEngine::UnhookRobloxLua() {
    if (!m_hooked) return true;

    std::cout << "[LuaEngine] No hooks to remove - Detours not installed" << std::endl;
    return true;
}

void LuaEngine::Execute(const std::string& code) {
    if (!m_initialized) {
        std::cout << "[LuaEngine] Not initialized!" << std::endl;
        return;
    }

    if (m_memorySafety) {
        SafeExecute(code);
    } else {
        ExecuteInternal(code);
    }
}

void LuaEngine::ExecuteFile(const std::string& filePath) {
    std::ifstream file(filePath);
    if (!file.is_open()) {
        std::cout << "[LuaEngine] Failed to open file: " << filePath << std::endl;
        return;
    }

    std::string code((std::istreambuf_iterator<char>(file)), 
                     std::istreambuf_iterator<char>());
    
    Execute(code);
}

bool LuaEngine::ExecuteInternal(const std::string& code) {
    if (!m_luaState) return false;

    int result = luaL_loadstring(m_luaState, code.c_str());
    if (result != LUA_OK) {
        const char* error = lua_tostring(m_luaState, -1);
        std::cout << "[LuaEngine] Load error: " << error << std::endl;
        if (m_errorCallback) m_errorCallback(error);
        lua_pop(m_luaState, 1);
        return false;
    }

    result = lua_pcall(m_luaState, 0, LUA_MULTRET, 0);
    if (result != LUA_OK) {
        const char* error = lua_tostring(m_luaState, -1);
        std::cout << "[LuaEngine] Execution error: " << error << std::endl;
        if (m_errorCallback) m_errorCallback(error);
        lua_pop(m_luaState, 1);
        return false;
    }

    return true;
}

void LuaEngine::SafeExecute(const std::string& code) {
    __try {
        ExecuteInternal(code);
    }
    __except (EXCEPTION_EXECUTE_HANDLER) {
        std::cout << "[LuaEngine] Memory access violation prevented!" << std::endl;
        if (m_errorCallback) m_errorCallback("Memory access violation - bad script?");
    }
}

void LuaEngine::AddToQueue(const std::string& code) {
    std::lock_guard<std::mutex> lock(m_queueMutex);
    m_executionQueue.push(code);
}

void LuaEngine::ProcessQueue() {
    m_running = true;
    
    while (m_running) {
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
        
        std::lock_guard<std::mutex> lock(m_queueMutex);
        while (!m_executionQueue.empty()) {
            std::string code = m_executionQueue.front();
            m_executionQueue.pop();
            
            Execute(code);
        }
    }
}

void LuaEngine::ClearQueue() {
    std::lock_guard<std::mutex> lock(m_queueMutex);
    while (!m_executionQueue.empty()) {
        m_executionQueue.pop();
    }
}

size_t LuaEngine::GetQueueSize() const {
    std::lock_guard<std::mutex> lock(m_queueMutex);
    return m_executionQueue.size();
}

// Hooked functions (stubs - won't work without Detours)
int LuaEngine::HookedPrint(lua_State* L) {
    std::cout << "[LuaEngine] Hooks not available - Detours not installed" << std::endl;
    return 0;
}

int LuaEngine::HookedLoadString(lua_State* L) {
    std::cout << "[LuaEngine] Hooks not available - Detours not installed" << std::endl;
    return 0;
}

int LuaEngine::HookedRequire(lua_State* L) {
    std::cout << "[LuaEngine] Hooks not available - Detours not installed" << std::endl;
    return 0;
}

// C interface functions
extern "C" {
    __declspec(dllexport) LuaEngine* CreateLuaEngine() {
        return new LuaEngine();
    }
    
    __declspec(dllexport) void DestroyLuaEngine(LuaEngine* engine) {
        delete engine;
    }
    
    __declspec(dllexport) void ExecuteLuaCode(LuaEngine* engine, const char* code) {
        if (engine) engine->Execute(code);
    }
    
    __declspec(dllexport) bool IsLuaEngineReady(LuaEngine* engine) {
        return engine && engine->IsInitialized();
    }
    
    __declspec(dllexport) void ClearExecutionQueue(LuaEngine* engine) {
        if (engine) engine->ClearQueue();
    }
}
