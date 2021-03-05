#pragma once

#include <Windows.h>
#include <cstdio>
#include <iostream>

#pragma data_seg(".octopus")
HOOKPROC _hookCallback = NULL;
#pragma comment(linker, "/SECTION:.octopus,RWS")
#pragma data_seg()

static HHOOK _hookInstance = NULL;
static HINSTANCE _moduleHandle = NULL;

extern "C" __declspec(dllexport)
HHOOK Install(int idHook, HWND window, HOOKPROC hookCallback);

// HookProc doesn't need to be exported. Does it need to exist at all?
extern "C" __declspec(dllexport)
LRESULT CALLBACK HookProc(int code, WPARAM wparam, LPARAM lparam);