using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PasswordInputProtection
{
    class KeyboardHook
    {
        public event KeyPressEventHandler KeyPressEvent;
        public event KeyEventHandler KeyDownEvent;
        public event KeyEventHandler KeyUpEvent;

        [StructLayout(LayoutKind.Sequential)]
        private class KeyboardLowLevelHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        private delegate int LowLevelKeyboardProc(int nCode, int wParam, IntPtr lParam);

        private LowLevelKeyboardProc KeyboardHookProcedure;

        private int hKeyboardHookId = 0;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_SYSKEYUP = 0x105;

        private const byte VK_CAPITAL = 0x14;
        private const byte VK_LALT = 0xA4;
        private const byte VK_LCONTROL = 0xA2;
        private const byte VK_LSHIFT = 0xA0;
        private const byte VK_NUMLOCK = 0x90;
        private const byte VK_RALT = 0xA5;
        private const byte VK_RCONTROL = 0x3;
        private const byte VK_RSHIFT = 0xA1;
        private const byte VK_SHIFT = 0x10;

        public void Start()
        {
            if (hKeyboardHookId == 0)
            {
                KeyboardHookProcedure = new LowLevelKeyboardProc(KeyboardHookProc);
                hKeyboardHookId =
                    SetWindowsHookEx(
                        WH_KEYBOARD_LL,
                        KeyboardHookProcedure,
                        GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName),
                        0);

                if (hKeyboardHookId == 0)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Stop();
                }
            }
        }

        public void Stop()
        {
            if (hKeyboardHookId != 0)
            {
                int retKeyboard = UnhookWindowsHookEx(hKeyboardHookId);
                hKeyboardHookId = 0;

                if (retKeyboard == 0)
                {
                    
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                }
            }
        }

        private int KeyboardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            bool handled = false;

            if ((nCode >= 0) && (KeyDownEvent != null || KeyUpEvent != null || KeyPressEvent != null))
            {
                KeyboardLowLevelHookStruct keyboardLowLevelHookStruct = new KeyboardLowLevelHookStruct();
                Marshal.PtrToStructure(lParam, keyboardLowLevelHookStruct);

                bool isDownControl = ((GetKeyState(VK_LCONTROL) & 0x80) != 0) || ((GetKeyState(VK_RCONTROL) & 0x80) != 0);
                bool isDownShift = ((GetKeyState(VK_LSHIFT) & 0x80) != 0) || ((GetKeyState(VK_RSHIFT) & 0x80) != 0);
                bool isDownAlt = ((GetKeyState(VK_LALT) & 0x80) != 0) || ((GetKeyState(VK_RALT) & 0x80) != 0);
                bool isDownCapslock = (GetKeyState(VK_CAPITAL) != 0);

                KeyEventArgs e = new KeyEventArgs((Keys)(keyboardLowLevelHookStruct.vkCode |
                                                            (isDownControl ? (int)Keys.Control : 0) |
                                                            (isDownShift ? (int)Keys.Shift : 0) |
                                                            (isDownAlt ? (int)Keys.Alt : 0)));

                if ((KeyDownEvent != null) && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
                {
                    KeyDownEvent(this, e);
                    handled = e.Handled;
                }
                else if ((KeyUpEvent != null) && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
                {
                    KeyUpEvent(this, e);
                    handled = e.Handled;
                }
                if ((KeyPressEvent != null) && (wParam == WM_KEYDOWN) && !handled && !e.SuppressKeyPress)
                {
                    byte[] keyState = new byte[256];
                    GetKeyboardState(keyState);

                    byte[] inBuffer = new byte[2];
                    if (ToAscii(keyboardLowLevelHookStruct.vkCode,
                        keyboardLowLevelHookStruct.scanCode,
                        keyState,
                        inBuffer,
                        keyboardLowLevelHookStruct.flags) == 1)
                    {
                        char key = (char)inBuffer[0];
                        if ((isDownCapslock ^ isDownShift) && Char.IsLetter(key))
                        {
                            key = Char.ToUpperInvariant(key);
                        }
                        KeyPressEventArgs kea = new KeyPressEventArgs(key);
                        KeyPressEvent(this, kea);
                        handled = kea.Handled;
                    }
                }
            }

            if (handled)
            {
                return 1;
            }
            else
            {
                return CallNextHookEx(hKeyboardHookId, nCode, wParam, lParam);
            }

        }

        ~KeyboardHook()
        {
            Stop();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            int dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int CallNextHookEx(
            int idHook,
            int nCode,
            int wParam,
            IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int ToAscii(
            int uVirtKey,
            int uScanCode,
            byte[] lpbKeyState,
            byte[] lpwTransKey,
            int fuState);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern short GetKeyState(int vKey);
    }
}
