#include <Windows.h>
#include <iostream>

#pragma data_seg(".dll")
HWND host = NULL;
#pragma comment(linker, "/SECTION:.dll,RWS")
#pragma data_seg()

HHOOK hook = NULL;
HINSTANCE dll = NULL;

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        dll = module;
    }

    return TRUE;
}

LRESULT CALLBACK CallWndProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code == HC_ACTION)
    {
        COPYDATASTRUCT data {};

        data.dwData = code;
        data.cbData = sizeof(CWPSTRUCT);
        data.lpData = reinterpret_cast<CWPSTRUCT*>(lParam);

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

LRESULT CALLBACK CallWndProcRet(int code, WPARAM wParam, LPARAM lParam)
{
    if (code == HC_ACTION)
    {
        COPYDATASTRUCT data {};

        data.dwData = code;
        data.cbData = sizeof(CWPRETSTRUCT);
        data.lpData = reinterpret_cast<CWPRETSTRUCT*>(lParam);

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

LRESULT CALLBACK CBTProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code >= 0)
    {
        COPYDATASTRUCT data {};

        data.dwData = code;
        data.cbData = sizeof(code);

        // https://docs.microsoft.com/en-us/previous-versions/windows/desktop/legacy/ms644977(v=vs.85)
        switch (code)
        {
            case HCBT_ACTIVATE:
                data.cbData = sizeof(CBTACTIVATESTRUCT);
                data.lpData = reinterpret_cast<CBTACTIVATESTRUCT*>(lParam);
                break;
            case HCBT_CLICKSKIPPED:
                data.cbData = sizeof(MOUSEHOOKSTRUCT);
                data.lpData = reinterpret_cast<MOUSEHOOKSTRUCT*>(lParam);
                break;
            case HCBT_CREATEWND:
                data.cbData = sizeof(CBT_CREATEWND);
                data.lpData = reinterpret_cast<CBT_CREATEWND*>(lParam);
                break;
            case HCBT_MOVESIZE:
                data.cbData = sizeof(RECT);
                data.lpData = reinterpret_cast<RECT*>(lParam);
                break;
        }

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

LRESULT CALLBACK DebugProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code == HC_ACTION)
    {
        COPYDATASTRUCT data {};

        data.dwData = code;
        data.cbData = sizeof(DEBUGHOOKINFO);
        data.lpData = reinterpret_cast<DEBUGHOOKINFO*>(lParam);

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

LRESULT CALLBACK ForegroundIdleProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code == HC_ACTION)
    {
        COPYDATASTRUCT data {};

        data.dwData = code;
        data.cbData = sizeof(code);

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

LRESULT CALLBACK JournalPlaybackProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code >= 0)
    {
        COPYDATASTRUCT data {};

        data.dwData = code;
        data.cbData = sizeof(code);

        if (code == HC_GETNEXT)
        {
            data.cbData = sizeof(EVENTMSG);
            data.lpData = reinterpret_cast<EVENTMSG*>(lParam);
        }

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

LRESULT CALLBACK JournalRecordProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code >= 0)
    {
        COPYDATASTRUCT data {};

        data.dwData = code;
        data.cbData = sizeof(EVENTMSG);
        data.lpData = reinterpret_cast<EVENTMSG*>(lParam);

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

LRESULT CALLBACK KeyboardProc(int code, WPARAM wParam, LPARAM lParam)
{
    struct KBDHOOKSTRUCT
    {
        DWORD vkCode;
        DWORD scanCode;
        DWORD keyCount;
        bool extended;
        bool alt;
        bool wasDown;
        bool transition;
    };

    if (code >= 0)
    {
        KBDHOOKSTRUCT kb {};
        COPYDATASTRUCT data {};

        kb.vkCode = wParam;
        kb.keyCount = lParam & 0xff;
        kb.scanCode = (lParam >> 16) & 0xff;
        kb.extended = (lParam & (1 << 24)) == 1;
        kb.alt = (lParam & (1 << 29)) == 1;
        kb.wasDown = (lParam & (1 << 30)) == 1;
        kb.transition = (lParam & (1 << 31)) == 1;

        data.dwData = code;
        data.cbData = sizeof(KBDHOOKSTRUCT);
        data.lpData = reinterpret_cast<KBDHOOKSTRUCT*>(lParam);

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

LRESULT CALLBACK MouseProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code >= 0)
    {
        COPYDATASTRUCT data {};
        MOUSEHOOKSTRUCT* mouse = reinterpret_cast<MOUSEHOOKSTRUCT*>(lParam);

        RECT rect {};
        POINT normalizedPoint = mouse->pt;

        GetWindowRect(mouse->hwnd, &rect);
        ScreenToClient(mouse->hwnd, &normalizedPoint);

        data.dwData = code;
        data.cbData = sizeof(MOUSEHOOKSTRUCT);
        data.lpData = mouse;

        auto x = mouse->pt.x - rect.left;
        auto y = mouse->pt.y - rect.top;

        // Log(x, y, "rect-based");
        // Log(mouse->pt.x, mouse->pt.y, "raw");
        // Log(normalizedPoint.x, normalizedPoint.y, "normalized");

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

LRESULT CALLBACK MessageProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code >= 0)
    {
        COPYDATASTRUCT data {};

        data.dwData = code;
        data.cbData = sizeof(MSG);
        data.lpData = reinterpret_cast<MSG*>(lParam);

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

// https://docs.microsoft.com/en-us/previous-versions/windows/desktop/legacy/ms644991(v=vs.85)
LRESULT CALLBACK ShellProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code >= 0)
    {
        COPYDATASTRUCT data {};

        data.dwData = code;
        data.cbData = sizeof(code);

        if (code == HSHELL_GETMINRECT)
        {
            data.cbData = sizeof(RECT);
            data.lpData = reinterpret_cast<RECT*>(lParam);
        }

        return SendMessageA(host, WM_COPYDATA, wParam, reinterpret_cast<LPARAM>(&data));
    }

    return CallNextHookEx(hook, code, wParam, lParam);
}

extern "C" __declspec(dllexport)
HHOOK Install(int idHook, HWND targetWindow, HWND serverWindow)
{
    if (hook == NULL && host == NULL)
    {
        auto targetProcessId = 0ul;
        auto targetProcessThreadId = GetWindowThreadProcessId(targetWindow, &targetProcessId);

        switch (idHook)
        {
            case WH_CBT:
                hook = SetWindowsHookExA(idHook, CBTProc, dll, targetProcessThreadId);
                break;
            case WH_DEBUG:
                hook = SetWindowsHookExA(idHook, DebugProc, dll, targetProcessThreadId);
                break;
            case WH_CALLWNDPROC:
                hook = SetWindowsHookExA(idHook, CallWndProc, dll, targetProcessThreadId);
                break;
            case WH_CALLWNDPROCRET:
                hook = SetWindowsHookExA(idHook, CallWndProcRet, dll, targetProcessThreadId);
                break;
            case WH_FOREGROUNDIDLE:
                hook = SetWindowsHookExA(idHook, ForegroundIdleProc, dll, targetProcessThreadId);
                break;
            case WH_JOURNALPLAYBACK:
                hook = SetWindowsHookExA(idHook, JournalPlaybackProc, dll, targetProcessThreadId);
                break;
            case WH_JOURNALRECORD:
                hook = SetWindowsHookExA(idHook, JournalRecordProc, dll, targetProcessThreadId);
                break;
            case WH_KEYBOARD:
                hook = SetWindowsHookExA(idHook, KeyboardProc, dll, targetProcessThreadId);
                break;
            case WH_MOUSE:
                hook = SetWindowsHookExA(idHook, MouseProc, dll, targetProcessThreadId);
                break;
            case WH_SHELL:
                hook = SetWindowsHookExA(idHook, ShellProc, dll, targetProcessThreadId);
                break;
            case WH_GETMESSAGE:
                hook = SetWindowsHookExA(idHook, MessageProc, dll, targetProcessThreadId);
                break;
            case WH_MSGFILTER:
                hook = SetWindowsHookExA(idHook, MessageProc, dll, targetProcessThreadId);
                break;
            case WH_SYSMSGFILTER:
                hook = SetWindowsHookExA(idHook, MessageProc, dll, targetProcessThreadId);
                break;
        }

        if (hook != NULL)
        {
            host = serverWindow;
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
        host = NULL;
    }

    return host == NULL && hook == NULL;
}