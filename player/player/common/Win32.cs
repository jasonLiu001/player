using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace player
{
    public class Win32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SendMessageW")]
        public static extern int SendMessageW2([System.Runtime.InteropServices.InAttribute()] System.IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);


        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        [DllImport("User32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string strclass, string FrmText);
    }
}