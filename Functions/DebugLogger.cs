using System;
using System.IO;

namespace Plugin
{
    class DebugLogger
    {
        internal bool DebugLogEnabled;
        internal string DebugDate;
        internal void LogMessage(string DebugMessage)
        {
            if (!DebugLogEnabled)
            {
                return;
            }
            try
            {
                string FileName = DebugDate + " - debug.log";
                using (StreamWriter sw = File.AppendText(Path.Combine(Plugin.TrainFolder, FileName)))
                {
                    sw.WriteLine(DateTime.Now + " : " + DebugMessage);
                }
            }
            catch
            {
                
            }
        }
    }
}
