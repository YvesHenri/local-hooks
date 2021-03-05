#include "main.h"

void log(const char* message)
{
    FILE* file = NULL;

    freopen_s(&file, "logs.txt", "a", stdout);

    if (file)
    {
        std::cout << message << std::endl;
        fclose(file);
    }
}

HHOOK Install(int idHook, HWND window, HOOKPROC hookCallback)
{
    auto processId = 0ul;
    auto threadId = GetWindowThreadProcessId(window, &processId);

    _hookCallback = hookCallback;
    _hookInstance = SetWindowsHookExA(idHook, HookProc, _moduleHandle, threadId);

    return _hookInstance;
}

LRESULT CALLBACK HookProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code > 0)
    {
        if (wParam == WM_MBUTTONUP)
        {
            if (_hookCallback)
            {
                log("callback ok");

                try
                {
                    _hookCallback(code, wParam, lParam);
                }
                catch (...)
                {
                    log("error");
                }
            }
            else
                log("callback invalid");
        }
    }

    return CallNextHookEx(_hookInstance, code, wParam, lParam);
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            _moduleHandle = hModule;
            break;
        case DLL_PROCESS_DETACH:
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
            break;
    }

    return TRUE;
}
