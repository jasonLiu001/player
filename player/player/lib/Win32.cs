using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace player
{
    public class Win32
    {
        /// <summary>
        /// 查找窗口
        /// </summary>
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// 获取标题
        /// </summary>
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SendMessageW")]
        public static extern int SendMessageW2([System.Runtime.InteropServices.InAttribute()] System.IntPtr hWnd, int Msg, int wParam, Byte[] lParam);

        /// <summary>
        /// 获取标题文字长度
        /// </summary>
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SendMessageW")]
        public static extern int SendMessageW2([System.Runtime.InteropServices.InAttribute()] System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        /// <summary>
        /// 获取文本
        /// </summary>
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        /// <summary>
        /// 查询子窗口
        /// </summary>
        [DllImport("User32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string strclass, string FrmText);
    }
}