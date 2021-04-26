using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmoothDrivingV
{
    public static class Logger
    {
        private static string targetDirectory = Environment.CurrentDirectory + "/Scripts/SmoothDrivingV/Log";
        private static string initDateString = "no-date";
        private static ulong logId = 0;

        public static void InitializeLog()
        {
            DateTime now = DateTime.Now;
            initDateString = now.Year + "-" + now.Month + "-" + now.Day;

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            while (File.Exists(targetDirectory + "/" + initDateString + "," + logId + ".log") && logId < ulong.MaxValue)
            {
                logId++;
            }
        }

        public static void WriteToLog(string message)
        {
            DateTime now = DateTime.Now;
            StreamWriter streamWriter = File.AppendText(targetDirectory + "/" + initDateString + "," + logId + ".log");
            
            streamWriter.WriteLine("[" + now.ToLongTimeString() + "]: " + message);
            
            streamWriter.Close();
            streamWriter.Dispose();
        }
    }
}
