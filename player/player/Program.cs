using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace player
{
    class Program
    {
        static readonly Robot robot = new Robot();

        /// <summary>
        /// 计时器
        /// </summary>
        static System.Timers.Timer aTimer = new System.Timers.Timer();

        /// <summary>
        /// 设置计时器
        /// </summary>
        static void SetTimer()
        {
            aTimer.Elapsed += new ElapsedEventHandler(TimeEvent);
            // 设置引发时间的时间间隔 此处设置为１秒（１０００毫秒）
            aTimer.Interval = 10000;
            aTimer.Enabled = true;
        }

        /// <summary>
        /// 计时器定时触发事件
        /// </summary>
        static void TimeEvent(object sender, ElapsedEventArgs e)
        {
            robot.Start();
        }

        static void Main(string[] args)
        {
            //设置计时器
            SetTimer();
            Console.ReadLine();
        }
    }
}
