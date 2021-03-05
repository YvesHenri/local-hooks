using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace APP
{
    public abstract class Hook
    {
        [DllImport("DLL32.dll", EntryPoint = "Install", CallingConvention = CallingConvention.Cdecl)]
        protected static extern IntPtr Install32(int idHook, IntPtr windowHandle, IntPtr callbackPointer);

        [DllImport("DLL64.dll", EntryPoint = "Install", CallingConvention = CallingConvention.Cdecl)]
        protected static extern IntPtr Install64(int idHook, IntPtr windowHandle, IntPtr callbackPointer);

        /*
        [DllImport("DLL32.dll", EntryPoint = "Uninstall", CallingConvention = CallingConvention.Cdecl)]
        protected static extern bool Uninstall32(IntPtr hookHandle);

        [DllImport("DLL64.dll", EntryPoint = "Uninstall", CallingConvention = CallingConvention.Cdecl)]
        protected static extern bool Uninstall64(IntPtr hookHandle);
        */

        protected abstract int LocalHookId { get; }
        protected abstract int GlobalHookId { get; }

        protected IntPtr instance;
        protected readonly HookCallback handler;
        protected readonly GCHandle garbageCollectorHandle;

        public bool Installed
        {
            get => instance != IntPtr.Zero;
        }

        public Hook()
        {
            instance = IntPtr.Zero;
            handler = new HookCallback(Callback);
            garbageCollectorHandle = GCHandle.Alloc(handler);
        }

        ~Hook()
        {
            garbageCollectorHandle.Free();
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
                if (!Global.Installed && !Install(Global))
                {
                    Console.WriteLine($"Failed to install {Global.GetType().Name} locally.");
                    // throw new Exception($"Failed to install {Global.GetType().Name} globally.");
                }

                return Global;
            }

            public THook Install(Process process)
            {
                // Get or create hook for corresponding process
                if (!localHooks.TryGetValue(process, out THook hook))
                {
                    hook = new THook();
                    
                    if (Install(hook, process))
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

            private bool Install(THook hook)
            {
                Debug.WriteLine($"Hook.Service<{typeof(THook).Name}>::Install");

                if (!hook.Installed)
                {
                    Debug.WriteLine($"Hook.Service<{typeof(THook).Name}>::Install - Installing");

                    using (Process process = Process.GetCurrentProcess())
                    {
                        using (ProcessModule processModule = process.MainModule)
                        {
                            var processModuleHandle = WinAPI.LoadLibrary("User32"); // WinAPI.GetModuleHandle(processModule.ModuleName);

                            hook.instance = WinAPI.SetWindowsHookEx(hook.GlobalHookId, hook.handler, processModuleHandle, 0);

                            Debug.WriteLine($"Hook.Service<{typeof(THook).Name}>::Install - Installed");
                        }
                    }
                }

                return hook.Installed;
            }

            private bool Install(THook hook, Process process)
            {
                Debug.WriteLine($"Hook.Service<{typeof(THook).Name}>::Install(Process)");

                if (!hook.Installed)
                {
                    Debug.WriteLine($"Hook.Service<{typeof(THook).Name}>::Install(Process) - Installing");
                    WinAPI.IsWow64Process(process.Handle, out bool isWow64Process);

                    IntPtr callbackPointer = Marshal.GetFunctionPointerForDelegate(hook.handler);

                    // If OS is not 64 bits, process can't be 64 bits either
                    if (Environment.Is64BitOperatingSystem && !isWow64Process)
                    {
                        hook.instance = Install64(hook.LocalHookId, process.MainWindowHandle, callbackPointer);
                        Debug.WriteLine($"Hook.Service<{typeof(THook).Name}>::Install(Process) - Installed 64 bits");
                    }
                    else
                    {
                        hook.instance = Install32(hook.LocalHookId, process.MainWindowHandle, callbackPointer);
                        Debug.WriteLine($"Hook.Service<{typeof(THook).Name}>::Install(Process) - Installed 32 bits");
                    }
                }

                return hook.Installed;
            }

            private bool Uninstall(THook hook)
            {
                return hook.Installed && WinAPI.UnhookWindowsHookEx(hook.instance);
            }

            private bool Uninstall(THook hook, Process process)
            {
                return Uninstall(hook) && localHooks.Remove(process);
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
