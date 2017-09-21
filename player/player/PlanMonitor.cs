using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace player
{
    /// <summary>
    /// 计划监视实体 监视计划软件中的计划变化
    /// </summary>
    public class PlanMonitor
    {
        /// <summary>
        /// 开始投注时间
        /// </summary>
        private static readonly string startTime = ConfigurationManager.AppSettings["startTime"].ToString();
        /// <summary>
        /// 结束投注时间
        /// </summary>
        private static readonly string endTime = ConfigurationManager.AppSettings["endTime"].ToString();
        /// <summary>
        /// 自动投注node程序所在路径
        /// </summary>
        private static readonly string nodeAppPath = ConfigurationManager.AppSettings["nodeAppPath"].ToString();
        /// <summary>
        /// 起始投注倍数 默认从1倍开始
        /// </summary>
        private static readonly int beginDoubleCount = Convert.ToInt32(ConfigurationManager.AppSettings["beginDoubleCount"]);
        /// <summary>
        /// 元角分模式：  元：1,  角：10，  分：100，  厘：1000
        /// </summary>
        private static readonly int awardModel = Convert.ToInt32(ConfigurationManager.AppSettings["awardModel"]);
        /// <summary>
        /// 当前账户最大值 单位：元  值0:表示不限制
        /// </summary>
        private static readonly int maxAccountReached = Convert.ToInt32(ConfigurationManager.AppSettings["maxAccountReached"]);
        /// <summary>
        /// 当前账户最小值 单元：元  值0:表示不限制
        /// </summary>
        private static readonly int maxLoseAccountReached = Convert.ToInt32(ConfigurationManager.AppSettings["maxLoseAccountReached"]);
        /// <summary>
        /// 调用外部命令的进程
        /// </summary>
        private static Process process = null;
        /// <summary>
        /// 当前计划文字长度
        /// </summary>
        private static int currentPlanTextLenght = 0;
        /// <summary>
        /// 上次的计划文字长度
        /// </summary>
        private static int lastPlanTextLength = 0;

        /// <summary>
        /// 获取完整的计划字符串
        /// </summary>
        private string GetFullPlan()
        {
            var fullPlan = string.Empty;
            IntPtr mainHwnd = Win32.FindWindow("WTWindow", null); //获取窗口句柄
            if (mainHwnd == IntPtr.Zero)
            {
                var msg = "未找到对应的窗口";
                Logger.WriteLog(msg);
                Console.WriteLine(msg);
                return fullPlan;
            }

            //第一个子窗口
            IntPtr firstChildWin = Win32.FindWindowEx(mainHwnd, IntPtr.Zero, "Edit", null);  //第一个子窗口
            //计划窗口 mainHwnd为主窗口 表示在这里面查找 如果替换成子窗口，说明在子窗口中查找
            IntPtr planWin = Win32.FindWindowEx(mainHwnd, firstChildWin, "Edit", null);  //计划窗口
            //存储字符的容量
            var planTextLength = Win32.SendMessageW2(planWin, WMessage.WM_GETTEXTLENGTH, 0, 0);
            //更新当前计划文字长度
            currentPlanTextLenght = planTextLength;

            Byte[] byt = new Byte[planTextLength * 2];
            Win32.SendMessageW2(planWin, WMessage.WM_GETTEXT, planTextLength * 2 + 1, byt);
            fullPlan = Encoding.Unicode.GetString(byt);
            return fullPlan;
        }

        /// <summary>
        /// 获取当前期号对应的计划字符串
        /// </summary>
        /// <returns>
        ///返回结果： 095-096期 后一/个位→13569← 计划进行期数：[1]
        /// </returns>
        private string GetCurrentPeriodPlan()
        {
            var txt = string.Empty;
            var titleText = GetFullPlan();
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
        /// 执行投注
        /// </summary>
        private void ExecuteInvest()
        {
            var investNumbers = GetInvestNumbers();
            if (string.IsNullOrEmpty(investNumbers)) return;
            var periodString = GetFirstPeriodString();
            process = Process.Start("cmd.exe", $"/C cd {nodeAppPath} && node CommandApp.js -n {investNumbers} -a {awardModel} -m {maxAccountReached} -l {maxLoseAccountReached} -d {beginDoubleCount} -p {periodString}");
        }

        /// <summary>
        /// 获取投注号码
        /// </summary>
        /// <returns>
        ///返回结果用逗号分隔： 3,4,6
        /// </returns>
        public string GetInvestNumbers()
        {
            var investNumber = string.Empty;
            var planText = GetCurrentPeriodPlan();
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
        /// 获取计划软件 单条计划的第一个期号值
        /// </summary>
        /// <returns>
        /// 如： 095-096期 后一/个位→13569← 计划进行期数：[1] 
        /// 返回值为 20170901-095
        /// </returns>
        public string GetFirstPeriodString()
        {
            var periodString = string.Empty;
            var planText = GetCurrentPeriodPlan();
            if (string.IsNullOrEmpty(planText)) return periodString;

            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month < 10 ? "0" + DateTime.Now.Month : DateTime.Now.Month.ToString();
            var day = DateTime.Now.Day < 10 ? "0" + DateTime.Now.Day : DateTime.Now.Day.ToString();

            periodString = year + month + day + "-" + planText.Split('-')[0];
            return periodString;
        }

        /// <summary>
        /// 开始计划监视
        /// </summary>
        public void Start()
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
                    var titleText = GetFullPlan();
                    if (string.IsNullOrEmpty(titleText)) return;

                    //更新计划文字长度
                    lastPlanTextLength = currentPlanTextLenght;
                    //执行投注                
                    ExecuteInvest();
                    return;
                }

                var newTitleText = GetFullPlan();
                //计划未更新
                if (string.IsNullOrEmpty(newTitleText) || lastPlanTextLength == currentPlanTextLenght) return;

                //更新计划文字长度
                lastPlanTextLength = currentPlanTextLenght;
                //执行投注
                ExecuteInvest();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex.Message);
                Console.WriteLine(ex.Message);
            }
        }
    }
}
