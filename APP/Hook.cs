using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace APP
{
    public abstract class Hook
    {
        [DllImport("DLL32.dll", EntryPoint = "Install", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        protected static extern IntPtr Install32(int idHook, IntPtr targetWindow, IntPtr serverWindow);

        [DllImport("DLL64.dll", EntryPoint = "Install", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        protected static extern IntPtr Install64(int idHook, IntPtr targetWindow, IntPtr serverWindow);

        [DllImport("DLL32.dll", EntryPoint = "Uninstall", CallingConvention = CallingConvention.Cdecl)]
        protected static extern bool Uninstall32();

        [DllImport("DLL64.dll", EntryPoint = "Uninstall", CallingConvention = CallingConvention.Cdecl)]
        protected static extern bool Uninstall64();

        protected abstract int LocalHookId { get; }
        protected abstract int GlobalHookId { get; }

        protected int id;
        protected IntPtr instance;

        protected readonly HookServer server;
        protected readonly HookCallback callback;

        public bool Installed
        {
            get => instance != IntPtr.Zero;
        }

        public Hook()
        {
            instance = IntPtr.Zero;
            server = new HookServer(Callback);
            callback = new HookCallback(Callback);
        }

        protected abstract IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Only services can install/uninstall hooks.
        /// </summary>
        public class Service<THook> where THook : Hook, new()
        {
            private readonly Dictionary<Process, THook> localHooks;

            public THook Global { get; }

            public Service()
            {
                Global = new THook();
                localHooks = new Dictionary<Process, THook>();
            }

            public THook Install()
            {
                if (!Global.Installed)
                {
                    var processModuleHandle = WinAPI.LoadLibrary("User32");

                    Global.id = Global.GlobalHookId;
                    Global.instance = WinAPI.SetWindowsHookEx(Global.GlobalHookId, Global.callback, processModuleHandle, 0);
                }

                return Global;
            }

            public THook Install(Process process)
            {
                // Get or create hook for corresponding process
                if (!localHooks.TryGetValue(process, out THook hook))
                {
                    EventHandler eventHandler = null;

                    WinAPI.IsWow64Process(process.Handle, out bool isWow64Process);
                    
                    hook = new THook();
                    hook.id = hook.LocalHookId;
                    hook.server.OnDestroyed += eventHandler = (sender, args) =>
                    {
                        lock (hook.server)
                        {
                            Console.WriteLine("Server went down. Uninstalling local hook for process {0}...", process.Id);

                            // Uninstall hook if server crashed or went down for some reason
                            if (Uninstall(process))
                            {
                                hook.server.OnDestroyed -= eventHandler;
                            }
                        }
                    };

                    if (hook.server.Start())
                    {
                        // If OS is not 64 bits, process can't be 64 bits either
                        if (Environment.Is64BitOperatingSystem && !isWow64Process)
                        {
                            hook.instance = Install64(hook.LocalHookId, process.MainWindowHandle, hook.server.Handle);
                        }
                        else
                        {
                            hook.instance = Install32(hook.LocalHookId, process.MainWindowHandle, hook.server.Handle);
                        }
                    }

                    // Cache successfull hooks
                    if (hook.Installed)
                    {
                        localHooks.Add(process, hook);
                    }
                }

                return hook;
            }

            public bool Uninstall()
            {
                return Uninstall(Global);
            }

            public bool Uninstall(Process process)
            {
                return localHooks.TryGetValue(process, out THook hook) && Uninstall(hook, process);
            }

            private bool Uninstall(THook hook)
            {
                return hook.Installed && WinAPI.UnhookWindowsHookEx(hook.instance);
            }

            private bool Uninstall(THook hook, Process process)
            {
                WinAPI.IsWow64Process(process.Handle, out bool isWow64Process);

                hook.server.Stop();

                // If OS is not 64 bits, process can't be 64 bits either
                if (Environment.Is64BitOperatingSystem && !isWow64Process)
                {
                    return Uninstall64() && localHooks.Remove(process);
                }
                else
                {
                    return Uninstall32() && localHooks.Remove(process);
                }
            }

            public bool Destroy()
            {
                var uninstalled = Uninstall(Global);

                foreach (var entry in new Dictionary<Process, THook>(localHooks))
                {
                    uninstalled &= Uninstall(entry.Value, entry.Key);
                }

                return uninstalled;
            }
        }
    }
}
