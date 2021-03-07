#include <Windows.h>
#include <iostream>

#pragma data_seg(".dll")

#pragma comment(linker, "/SECTION:.dll,RWS")
#pragma data_seg()

HANDLE pipe = NULL;
HHOOK hook = NULL;
HINSTANCE dll = NULL;

void Log(const char* message)
{
    FILE* file = NULL;

    freopen_s(&file, "logs.txt", "a", stdout);

    if (file)
    {
        std::cout << message << std::endl;
        fclose(file);
    }

}
struct Request
{
    int code;
    WPARAM wParam;
    LPARAM lParam;
};

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        dll = module;
    }

    return TRUE;
}

LRESULT CALLBACK HookProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code >= 0)
    {
        if (pipe != NULL && pipe != INVALID_HANDLE_VALUE)
        {
            auto requestSize = 0ul;
            auto request = Request { code, wParam, lParam };

            // Send hook data
            if (WriteFile(pipe, &request, sizeof(request), &requestSize, NULL))
            {
                auto responseSize = 0ul;
                auto response = 0l;

                // Await server reply
                if (ReadFile(pipe, &response, sizeof(response), &responseSize, NULL))
                {
                    return response;
                }
                else
                {
                    char buffer[32];
                    _itoa_s(GetLastError(), buffer, 10);
                    Log("Read");
                    Log(buffer);
                }
            }
            else
            {
                char buffer[32];
                _itoa_s(GetLastError(), buffer, 10);
                Log("Write");
                Log(buffer);
            }
        }
    }

    // Fallback if server can not dispatch the request
    return CallNextHookEx(hook, code, wParam, lParam);
}

extern "C" __declspec(dllexport)
HHOOK Install(int idHook, HWND window, const char* pipeName)
{
    if (hook == NULL && pipe == NULL)
    {
        SetLastError(0ul);

        pipe = CreateFileA(pipeName, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);

        // If no pipe is available, wait 3 seconds to retry
        if (GetLastError() == ERROR_PIPE_BUSY)
        {
            if (!WaitNamedPipeA(pipeName, 3000))
            {
                pipe = NULL;
                return NULL;
            }
        }

        // Install hook if connected
        if (pipe != NULL && pipe != INVALID_HANDLE_VALUE)
        {
            auto targetProcessId = 0ul;
            auto targetProcessThreadId = GetWindowThreadProcessId(window, &targetProcessId);

            hook = SetWindowsHookExA(idHook, HookProc, dll, targetProcessThreadId);
        }
    }

    return hook;
}

extern "C" __declspec(dllexport)
bool Uninstall()
{
    if (hook != NULL && UnhookWindowsHookEx(hook))
    {
        hook = NULL;

        if (pipe != NULL && CloseHandle(pipe))
        {
            pipe = NULL;
        }
    }

    return pipe == NULL && hook == NULL;
}