namespace APP
{
    public sealed class HookService
    {
        public static Hook.Service<MouseHook> Mouse = new Hook.Service<MouseHook>();
        public static Hook.Service<KeyboardHook> Keyboard = new Hook.Service<KeyboardHook>();

        public static void Destroy()
        {
            Mouse.Destroy();
            Keyboard.Destroy();
        }
    }
}
