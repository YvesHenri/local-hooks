namespace APP
{
    public sealed class HookService
    {
        public static Hook.Service<MouseHook> Mouse;
        public static Hook.Service<KeyboardHook> Keyboard;

        static HookService()
        {
            Mouse = new Hook.Service<MouseHook>();
            Keyboard = new Hook.Service<KeyboardHook>();
        }

        public static void Destroy()
        {
            Mouse.Destroy();
            Keyboard.Destroy();
        }
    }
}
