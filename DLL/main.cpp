#include <Windows.h>
#include <string>

#pragma data_seg(".dll")
HHOOK _hookHandle = NULL;
HOOKPROC _hookCallback = NULL;
HINSTANCE _moduleHandle = NULL;
#pragma comment(linker, "/SECTION:.dll,RWS")
#pragma data_seg()

// This gets executed within the target process memory region
LRESULT CALLBACK HookProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code > 0)
    {
        auto hostProcessFunctionPointerAddress = 0xdeadbeef;

    }

    return CallNextHookEx(_hookHandle, code, wParam, lParam);
}

extern "C" __declspec(dllexport)
HHOOK Install(int idHook, HWND window, HOOKPROC hookCallback)
{
    auto targetProcessId = 0ul;
    auto targetProcessThreadId = GetWindowThreadProcessId(window, &targetProcessId);
    auto hostProcessId = GetCurrentProcessId();
    auto hostProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, FALSE, hostProcessId);

    if (hostProcessHandle)
    {
        // VirtualAllocEx(hostProcessHandle, NULL, sizeof(HOOKPROC), MEM_COMMIT, PAGE_READWRITE);

        _hookCallback = hookCallback;
        _hookHandle = SetWindowsHookExA(idHook, HookProc, _moduleHandle, targetProcessThreadId);

        return _hookHandle;
    }
    else
    {
        return NULL;
    }
}

/*
void CreateIPC()
{
    auto fileMappingHandle = CreateFileMappingA(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, NULL, sizeof(HOOKDATA), "sfm");

    if (fileMappingHandle)
    {
        auto fileMappingData = (HOOKDATA*) MapViewOfFile(fileMappingHandle, FILE_MAP_WRITE | FILE_MAP_READ, NULL, NULL, sizeof(HOOKDATA));
    }
}
*/

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            _moduleHandle = hModule;
            break;
        case DLL_PROCESS_DETACH:
            break;
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
            break;
    }

    return TRUE;
}
