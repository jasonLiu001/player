using System;
using System.Timers;

namespace player
{
    class Program
    {
        static readonly Robot robot = new Robot();

        /// <summary>
        /// 计时器定时触发事件
        /// </summary>
        static void TimeEvent(object sender, ElapsedEventArgs e)
        {
            robot.Start();
        }

        [STAThread]
        static void Main(string[] args)
        {
            var aTimer = new Timer();
            aTimer.Elapsed += new ElapsedEventHandler(TimeEvent);
            // 设置引发时间的时间间隔 此处设置为１秒（１０００毫秒）
            aTimer.Interval = 10000;
            aTimer.Enabled = true;
            //设置计时器
            Console.WriteLine("working...");
            Console.ReadLine();
        }
    }
}
