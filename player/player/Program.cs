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
        /// <summary>
        /// 开始投注时间
        /// </summary>
        static string startTime = ConfigurationManager.AppSettings["startTime"].ToString();
        /// <summary>
        /// 结束投注时间
        /// </summary>
        static string endTime = ConfigurationManager.AppSettings["endTime"].ToString();
        /// <summary>
        /// 自动投注node程序所在路径
        /// </summary>
        static string nodeAppPath = ConfigurationManager.AppSettings["nodeAppPath"].ToString();
        /// <summary>
        /// 元角分模式：  元：1,  角：10，  分：100，  厘：1000
        /// </summary>
        static int awardModel = Convert.ToInt32(ConfigurationManager.AppSettings["awardModel"]);
        /// <summary>
        /// 当前账户最大值 单位：元  值0:表示不限制
        /// </summary>
        static int maxAccountReached = Convert.ToInt32(ConfigurationManager.AppSettings["maxAccountReached"]);
        /// <summary>
        /// 当前账户最小值 单元：元  值0:表示不限制
        /// </summary>
        static int maxLoseAccountReached = Convert.ToInt32(ConfigurationManager.AppSettings["maxLoseAccountReached"]);

        /// <summary>
        /// 计时器
        /// </summary>
        static System.Timers.Timer aTimer = new System.Timers.Timer();

        /// <summary>
        /// 上次的计划文字
        /// </summary>
        static string lastPlanText = string.Empty;

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
            DateTime stopTime1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt32(startTime.Split(':')[0]), Convert.ToInt32(startTime.Split(':')[1]), 0);
            DateTime stopTimer2 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt32(endTime.Split(':')[0]), Convert.ToInt32(endTime.Split(':')[1]), 0);
            //超出投注时间不执行投注
            if (DateTime.Now < stopTime1) return;
            if (DateTime.Now > stopTimer2)
            {
                //关机
                Process.Start("shutdown.exe", "-s -t 30");
                return;
            }

            try
            {
                //首次
                if (string.IsNullOrEmpty(lastPlanText))
                {
                    var titleText = GetWindowCaptionTitle();
                    if (string.IsNullOrEmpty(titleText)) return;

                    //保存最新的计划文本
                    lastPlanText = titleText;
                    //执行投注                
                    StartInvest();
                    return;
                }

                var newTitleText = GetWindowCaptionTitle();
                if (newTitleText == lastPlanText || string.IsNullOrEmpty(newTitleText)) return; //计划未更新

                //保存最新的计划文本
                lastPlanText = newTitleText;
                //执行投注
                StartInvest();
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
                Console.WriteLine(ex.Message);
            }

        }

        /// <summary>
        /// 获取窗口标题文字
        /// </summary>
        /// <returns></returns>
        static string GetWindowCaptionTitle()
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
        /// 获取计划字符串
        /// </summary>
        /// <returns>
        ///返回结果： 095-096期 后一/个位→13569← 计划进行期数：[1]
        /// </returns>
        static string GetPlanText()
        {
            var txt = string.Empty;
            var titleText = GetWindowCaptionTitle();
            var txtArray = titleText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < txtArray.Length; i++)
            {
                var itemText = txtArray[i];
                if (itemText.Contains("计划进行"))
                {
                    //上期中的时候才跟    
                    if (i > 0 && txtArray[i - 1].Contains("中"))
                    {
                        txt = itemText;
                        break;
                    }
                }

            }
            return txt;
        }

        /// <summary>
        /// 获取投注号码
        /// </summary>
        /// <returns>
        ///返回结果用逗号分隔： 3,4,6
        /// </returns>
        static string GetInvestNumbers()
        {
            var investNumber = string.Empty;
            var planText = GetPlanText();
            if (string.IsNullOrEmpty(planText)) return investNumber;

            //投注号码
            var number = planText.Substring(planText.IndexOf('→') + 1, planText.IndexOf('←') - planText.IndexOf('→') - 1);
            //当前计划期数 期数为1投注
            var count = planText.Substring(planText.IndexOf('[') + 1, planText.IndexOf(']') - planText.IndexOf('[') - 1);
            if (count != "1") return investNumber;

            var stringBuilder = new StringBuilder();
            for (var i = 0; i < number.Length; i++)
            {
                stringBuilder.Append(number[i]);
                stringBuilder.Append(",");
            }
            stringBuilder.Remove(stringBuilder.ToString().Length - 1, 1);
            investNumber = stringBuilder.ToString();
            return investNumber;
        }

        /// <summary>
        /// 执行投注
        /// </summary>
        static void StartInvest()
        {
            var investNumbers = GetInvestNumbers();
            if (string.IsNullOrEmpty(investNumbers)) return;
            Process.Start("cmd.exe", $"/C cd {nodeAppPath} && node CommandApp.js -n {investNumbers} -a {awardModel} -m {maxAccountReached} -l {maxLoseAccountReached}");
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        static void WriteLog(string msg)
        {
            var logFile = System.Environment.CurrentDirectory + "\\error.log";
            if (!File.Exists(logFile))
            {
                FileStream fs1 = new FileStream(logFile, FileMode.Create, FileAccess.Write);//创建写入文件 
                StreamWriter sw = new StreamWriter(fs1);
                sw.WriteLine(msg);//开始写入值
                sw.Close();
                fs1.Close();
            }
            else
            {
                FileStream fs = new FileStream(logFile, FileMode.Open, FileAccess.Write);
                StreamWriter sr = new StreamWriter(fs);
                sr.WriteLine(msg);//开始写入值
                sr.Close();
                fs.Close();
            }
        }

        static void Main(string[] args)
        {
            //设置计时器
            SetTimer();
            Console.ReadLine();
        }
    }
}
