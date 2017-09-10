using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace player
{
    class Program
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public static string filePath = ConfigurationManager.AppSettings["filePath"].ToString();

        /// <summary>
        /// 计时器
        /// </summary>
        private static System.Timers.Timer aTimer = new System.Timers.Timer();

        /// <summary>
        /// 上次的计划文字
        /// </summary>
        private static string lastPlanText = string.Empty;

        static void Main(string[] args)
        {
            //设置计时器
            //SetTimer();
            GetPlanText();
            Console.ReadLine();
        }

        /// <summary>
        /// 获取计划字符串
        /// </summary>
        /// <returns></returns>
        private static string GetPlanText()
        {
            var txt = string.Empty;
            var titleText = GetWindowCaptionTitle();
            var txtArray = titleText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < txtArray.Length; i++)
            {
                var itemText = txtArray[i];
                if (itemText.Contains("计划进行"))
                {
                    txt = itemText;
                    break;
                }
            }
            return txt;
        }

        /// <summary>
        /// 获取窗口标题文字
        /// </summary>
        /// <returns></returns>
        private static string GetWindowCaptionTitle()
        {
            IntPtr maindHwnd = Win32.FindWindow("WTWindow", null); //获取窗口句柄
            if (maindHwnd == IntPtr.Zero)
            {
                Console.WriteLine("未找到对应的窗口");
                return string.Empty;
            }

            //第一个子窗口
            IntPtr firstChildWin = Win32.FindWindowEx(maindHwnd, IntPtr.Zero, "Edit", null);  //第一个子窗口
            //计划窗口 maindHwnd为主窗口 表示在这里面查找 如果替换成子窗口，说明在子窗口中查找
            IntPtr planWin = Win32.FindWindowEx(maindHwnd, firstChildWin, "Edit", null);  //计划窗口
            int maxLength = 1000000;

            IntPtr buffer = Marshal.AllocHGlobal((maxLength + 1) * 2);
            Win32.SendMessageW2(planWin, Constant.WM_GETTEXT, (uint)maxLength, buffer);
            string windowCaptionTitle = Marshal.PtrToStringUni(buffer);
            return windowCaptionTitle;
        }


        /// <summary>
        /// 设置计时器
        /// </summary>
        private static void SetTimer()
        {
            aTimer.Elapsed += new ElapsedEventHandler(TimeEvent);
            // 设置引发时间的时间间隔 此处设置为１秒（１０００毫秒）
            aTimer.Interval = 10000;
            aTimer.Enabled = true;
        }

        /// <summary>
        /// 计时器定时触发事件
        /// </summary>
        private static void TimeEvent(object sender, ElapsedEventArgs e)
        {
            //首次
            if (string.IsNullOrEmpty(lastPlanText))
            {
                var titleText = GetWindowCaptionTitle();
                //保存文本
                lastPlanText = titleText;
                //保存到文件
                Console.WriteLine(titleText);
                return;
            }

            var newTitleText = GetWindowCaptionTitle();
            if (newTitleText == lastPlanText) return; //计划未更新

            //保存新计划文本
            lastPlanText = newTitleText;
            //保存到文件
            Console.WriteLine(newTitleText);
        }
    }
}
