using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CADConverter
{
    /// <summary>
    /// 日志工具
    /// <para>每个程序第一次使用前，请调用<see cref="InitializePath"/>方法初始化日志存储路径，否则多个程序调用时会导致日志文件全部存储在默认(%TEMP%\HLProgramLogs)目录下，造成混乱。</para>
    /// </summary>
    public static class Log
    {
        private static readonly object _lock = new object(); // 锁对象

        /// <summary>
        /// 日志存储路径。由于是静态变量，多个调用者设置不同的路径，都会被最后一个设置的值覆盖。所以存储调用者信息及其对应的日志路径。
        /// </summary>
        private static Dictionary<string, string> LogPath { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 初始化日志存储路径(只需调用一次)
        /// <para>不初始化则使用默认路径：%TEMP%\HLProgramLogs</para>
        /// </summary>
        /// <param name="path">日志存储路径</param>
        public static void InitializePath(string path)
        {
            StackTrace stackTrace = new StackTrace();
            var assemblyName = stackTrace.GetFrame(1).GetMethod().DeclaringType.Assembly.GetName().Name;

            LogPath[assemblyName] = path;
        }

        /// <summary>
        /// 日志默认存储路径
        /// </summary>
        private static string DefaultLogPath => Path.Combine(Path.GetTempPath(), "HLProgramLogs");

        /// <summary>
        /// 日志文件路径
        /// </summary>
        private static string GetLogFileName(string logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath))
                logPath = DefaultLogPath;

            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            return Path.Combine(logPath, $"{DateTime.Now:yyyyMMdd}.log");
        }

        /// <summary>
        /// 保存异常信息
        /// </summary>
        /// <param name="ex">异常信息</param>
        public static async void WriteErrorLogAsync(Exception ex)
        {
            try
            {
                //通过Task传参ex，在Task内部再将参数转化为Exception时，有时会丢失堆栈部分信息（比如异常所在行），因此这里将异常信息直接转化为字符串传参
                string msg = $"{DateTime.Now:HH:mm:ss}{Environment.NewLine}异常来源：{ex.Source}{Environment.NewLine}异常信息：{ex.Message}{Environment.NewLine}堆栈信息：{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}";

                //判断调用方，获取日志存储路径
                string logPath = string.Empty;
                StackFrame[] frames = new StackTrace().GetFrames();
                for (int i = 1; i < frames.Length; i++)
                {
                    var assemblyName = frames[i].GetMethod().DeclaringType.Assembly.GetName().Name;
                    if (LogPath.ContainsKey(assemblyName))
                    {
                        logPath = LogPath[assemblyName];
                        break;
                    }
                }

                string logFileName = GetLogFileName(logPath);

                await Task.Factory.StartNew((obj) =>
                {
                    lock (_lock)
                    {
                        string[] strs = (string[])obj;

                        using (FileStream fs = new FileStream(strs[1], FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            byte[] log = Encoding.Default.GetBytes(strs[0]);
                            fs.Write(log, 0, log.Length);
                        }
                    }
                }, new string[] { msg, logFileName });

            }
            catch (Exception e)
            {
                WriteErrorLogAsync(e);
            }
        }

        /// <summary>
        /// 保存日志信息
        /// </summary>
        /// <param name="msg">日志信息</param>
        public static async void WriteLogAsync(string msg)
        {
            try
            {
                string logPath = string.Empty;
                StackFrame[] frames = new StackTrace().GetFrames();
                for (int i = 1; i < frames.Length; i++)
                {
                    var assemblyName = frames[i].GetMethod().DeclaringType.Assembly.GetName().Name;
                    if (LogPath.ContainsKey(assemblyName))
                    {
                        logPath = LogPath[assemblyName];
                        break;
                    }
                }

                string logFileName = GetLogFileName(logPath);

                await Task.Factory.StartNew((obj) =>
                {
                    lock (_lock)
                    {
                        string[] strs = (string[])obj;

                        using (FileStream fs = new FileStream(strs[1], FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            byte[] log = Encoding.Default.GetBytes($"{DateTime.Now:HH:mm:ss}{Environment.NewLine}{strs[0]}{Environment.NewLine}{Environment.NewLine}");
                            fs.Write(log, 0, log.Length);
                        }
                    }
                }, new string[] { msg, logFileName });
            }
            catch (Exception e)
            {
                WriteErrorLogAsync(e);
            }
        }
    }
}
