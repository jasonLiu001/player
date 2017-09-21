using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Timers;

namespace player
{
    class Program
    {
        static PlanMonitor plan = new PlanMonitor();

        /// <summary>
        /// 计时器定时触发事件
        /// </summary>
        static void TimeEvent(object sender, ElapsedEventArgs e)
        {           
            plan.Start();
        }

        [STAThread]
        static void Main(string[] args)
        {
            var aTimer = new Timer();
            aTimer.Elapsed += new ElapsedEventHandler(TimeEvent);
            aTimer.Interval = 10000;
            aTimer.Enabled = true;
            Console.WriteLine("working...");
            Console.ReadLine();
        }
    }
}
