using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PayPage
{
    public static class Log
    {
        private static object sync = new object();
        private static string path = AppDomain.CurrentDomain.BaseDirectory + "Log";

        public static void Write(Exception e)
        {
            Task.Run(()=> {
                lock (sync)
                {
                    try
                    {
                        if (!System.IO.Directory.Exists(path))
                            System.IO.Directory.CreateDirectory(path);

                        using (var sw = new System.IO.StreamWriter(path + "\\log_" + DateTime.Now.ToString("yyyyMMdd_HH") + ".log", true))
                        {
                            sw.WriteLine("\r\n" + DateTime.Now + "  :   " + e.Message);
                            if (e.StackTrace != null && !string.IsNullOrEmpty(e.StackTrace))
                                sw.WriteLine(">  " + e.StackTrace);
                        }
                        
                    }
                    catch { }
                }
            });
        }

    }
}
