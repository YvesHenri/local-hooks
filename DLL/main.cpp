#include <Windows.h>
#include <string>
#include <iostream>

#include "client.hpp"

#pragma data_seg(".dll")
// 
#pragma comment(linker, "/SECTION:.dll,RWS")
#pragma data_seg()

HHOOK _hookHandle = NULL;
HINSTANCE _moduleHandle = NULL;
HookClient _hookClient;

void Log(const char* message)
{
    FILE* file = NULL;

    freopen_s(&file, "x.txt", "a", stdout);

    if (file)
    {
        std::cout << message << std::endl;

        fclose(file);
    }
}

LRESULT CALLBACK HookProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code > 0)
    {
        if (wParam == WM_RBUTTONUP)
        {
            Log("Right up");
        }
        if (wParam == WM_MBUTTONUP)
        {
            Log("Middle up");
        }
        if (wParam == WM_LBUTTONUP)
        {
            Log("Left up");
        }

        _hookClient.log("Logging message from HookProc");

        if (_hookClient.connected())
        {
            Log("Sending hook data...");
            _hookClient.send(code, wParam, lParam);
        }
        else
            Log("Can not send hook data. Client is disconnected.");
    }

    return CallNextHookEx(_hookHandle, code, wParam, lParam);
}

extern "C" __declspec(dllexport) 
HHOOK Install(int idHook, HWND window, const char* pipeServerName)
{
    if (_hookHandle == NULL && !_hookClient.connected())
    {
        auto targetProcessId = 0ul;
        auto targetProcessThreadId = GetWindowThreadProcessId(window, &targetProcessId);

        if (_hookClient.connect(pipeServerName))
        {
            _hookHandle = SetWindowsHookExA(idHook, HookProc, _moduleHandle, targetProcessThreadId);
        }
    }

    return _hookHandle;
}

extern "C" __declspec(dllexport)
bool Uninstall()
{
    if (_hookClient.connected())
    {
        if (_hookClient.disconnect())
        {
            if (_hookHandle)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = NULL;
            }
        }
    }    

    return _hookHandle == NULL;
}

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