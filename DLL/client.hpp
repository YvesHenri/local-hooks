#pragma once

#include <fileapi.h>

#include "data.hpp"

class HookClient
{
public:
	void log(const char* message)
	{
		FILE* file = NULL;

		freopen_s(&file, "client.txt", "a", stdout);

		if (file)
		{
			std::cout << "[Client] " << message << std::endl;

			fclose(file);
		}
	}

	HookClient() : pipe(INVALID_HANDLE_VALUE)
	{
		log("Constructed!");
	}

	~HookClient()
	{
		log("Destroyed");
	}

	bool connect(const char* pipeName)
	{
		if (pipe == INVALID_HANDLE_VALUE)
		{
			pipe = CreateFileA(pipeName, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);

			if (GetLastError() == ERROR_PIPE_BUSY)
			{
				return WaitNamedPipeA(pipeName, 5000);
			}
		}

		if (connected())
		{
			log("Connected!");
		}

		return pipe != INVALID_HANDLE_VALUE;
	}

	bool disconnect()
	{
		if (pipe != INVALID_HANDLE_VALUE)
		{
			log("Disconnecting...");
			CloseHandle(pipe);
			pipe = INVALID_HANDLE_VALUE;
		}

		return pipe == INVALID_HANDLE_VALUE;
	}

	bool connected()
	{
		return pipe != INVALID_HANDLE_VALUE;
	}

	LRESULT send(int code, WPARAM wParam, LPARAM lParam)
	{
		if (pipe != INVALID_HANDLE_VALUE)
		{
			auto actualRequestSize = 0ul;
			auto request = HookData(code, wParam, lParam);

			if (!WriteFile(pipe, &request, sizeof(request), &actualRequestSize, NULL))
			{
				return -1 * GetLastError();
			}

			auto actualResponseSize = 0ul;
			auto responseBuffer = new char[4];
			
			if (!ReadFile(pipe, responseBuffer, 4, &actualResponseSize, NULL))
			{
				return -1 * GetLastError();
			}

			auto response = 0l;

			std::memcpy(&response, responseBuffer, sizeof(response));

			return response;
		}

		return -1l;
	}

private:
	HANDLE pipe;
};