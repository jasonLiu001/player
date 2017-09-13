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
        /// <summary>
        /// 开始投注时间
        /// </summary>
        static readonly string startTime = ConfigurationManager.AppSettings["startTime"].ToString();
        /// <summary>
        /// 结束投注时间
        /// </summary>
        static readonly string endTime = ConfigurationManager.AppSettings["endTime"].ToString();
        /// <summary>
        /// 自动投注node程序所在路径
        /// </summary>
        static readonly string nodeAppPath = ConfigurationManager.AppSettings["nodeAppPath"].ToString();
        /// <summary>
        /// 起始投注倍数 默认从1倍开始
        /// </summary>
        static readonly int beginDoubleCount = Convert.ToInt32(ConfigurationManager.AppSettings["beginDoubleCount"]);
        /// <summary>
        /// 元角分模式：  元：1,  角：10，  分：100，  厘：1000
        /// </summary>
        static readonly int awardModel = Convert.ToInt32(ConfigurationManager.AppSettings["awardModel"]);
        /// <summary>
        /// 当前账户最大值 单位：元  值0:表示不限制
        /// </summary>
        static readonly int maxAccountReached = Convert.ToInt32(ConfigurationManager.AppSettings["maxAccountReached"]);
        /// <summary>
        /// 当前账户最小值 单元：元  值0:表示不限制
        /// </summary>
        static readonly int maxLoseAccountReached = Convert.ToInt32(ConfigurationManager.AppSettings["maxLoseAccountReached"]);

        /// <summary>
        /// 上次的计划文字长度
        /// </summary>
        static int lastPlanTextLength = 0;

        /// <summary>
        /// 当前计划文字长度
        /// </summary>
        static int currentPlanTextLenght = 0;

        static Process process = null;

        /// <summary>
        /// 计时器定时触发事件
        /// </summary>
        static void TimeEvent(object sender, ElapsedEventArgs e)
        {
            //清除多余进程信息
            if (process != null) process = null;

            DateTime stopTime1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt32(startTime.Split(':')[0]), Convert.ToInt32(startTime.Split(':')[1]), 0);
            DateTime stopTimer2 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt32(endTime.Split(':')[0]), Convert.ToInt32(endTime.Split(':')[1]), 0);
            //超出投注时间不执行投注
            if (DateTime.Now < stopTime1) return;
            if (DateTime.Now > stopTimer2)
            {
                //关机
                process = Process.Start("shutdown.exe", "-s -t 30");
                return;
            }

            try
            {
                //首次
                if (lastPlanTextLength == 0 && currentPlanTextLenght == 0)
                {
                    var titleText = GetWindowCaptionTitle();
                    if (string.IsNullOrEmpty(titleText)) return;

                    //更新计划文字长度
                    lastPlanTextLength = currentPlanTextLenght;
                    //执行投注                
                    StartInvest();
                    WriteLog($"titleText:{titleText}");
                    Console.WriteLine($"titleText:{titleText}");
                    return;
                }

                var newTitleText = GetWindowCaptionTitle();
                //计划未更新
                if (string.IsNullOrEmpty(newTitleText) || lastPlanTextLength == currentPlanTextLenght) return;

                //更新计划文字长度
                lastPlanTextLength = currentPlanTextLenght;
                //执行投注
                StartInvest();
                WriteLog($"newTitleText:{newTitleText}");
                Console.WriteLine($"newTitleText:{newTitleText}");
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
            var captionTitle = string.Empty;
            IntPtr maindHwnd = Win32.FindWindow("WTWindow", null); //获取窗口句柄
            if (maindHwnd == IntPtr.Zero)
            {
                var msg = "未找到对应的窗口";
                WriteLog(msg);
                Console.WriteLine(msg);
                return captionTitle;
            }

            //第一个子窗口
            IntPtr firstChildWin = Win32.FindWindowEx(maindHwnd, IntPtr.Zero, "Edit", null);  //第一个子窗口
            //计划窗口 maindHwnd为主窗口 表示在这里面查找 如果替换成子窗口，说明在子窗口中查找
            IntPtr planWin = Win32.FindWindowEx(maindHwnd, firstChildWin, "Edit", null);  //计划窗口
            //存储字符的容量
            var txtLength = Win32.SendMessageW2(planWin, Constant.WM_GETTEXTLENGTH, 0, 0);
            //更新当前计划文字长度
            currentPlanTextLenght = txtLength;
            WriteLog($"txtLength:{txtLength}");
            Console.WriteLine($"txtLength:{txtLength}");

            Byte[] byt = new Byte[txtLength * 2];
            Win32.SendMessageW2(planWin, Constant.WM_GETTEXT, txtLength * 2 + 1, byt);
            captionTitle = Encoding.Unicode.GetString(byt);
            return captionTitle;
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
            //process = Process.Start("cmd.exe", $"/C cd {nodeAppPath} && node CommandApp.js -n {investNumbers} -a {awardModel} -m {maxAccountReached} -l {maxLoseAccountReached} -d {beginDoubleCount}");
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
