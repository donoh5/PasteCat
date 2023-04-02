using System;
using System.Runtime.InteropServices;

namespace PasteIt
{
    public class KeySimulator
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        private const byte KEYEVENTF_EXTENDEDKEY = 0x01;
        private const byte KEYEVENTF_KEYUP = 0x02;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;

        public static void SimulatePaste()
        {
            // Press the 'Ctrl' key
            keybd_event(VK_CONTROL, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);

            // Press the 'V' key
            keybd_event(VK_V, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);

            // Release the 'V' key
            keybd_event(VK_V, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, IntPtr.Zero);

            // Release the 'Ctrl' key
            keybd_event(VK_CONTROL, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, IntPtr.Zero);
        }
    }
}
