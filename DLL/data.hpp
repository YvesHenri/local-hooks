#pragma once

#include <minwindef.h>

struct HookData
{
	int code;
	WPARAM wParam;
	LPARAM lParam;

	HookData(int code, WPARAM wParam, LPARAM lParam) : code(code), wParam(wParam), lParam(lParam) {}
};