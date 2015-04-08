using System;
using System.IO;

namespace Plugin
{
    class DebugLogger
    {
        internal bool DebugLogEnabled;
        internal string TrainPath = InternalFunctions.trainfolder;
        internal string DebugDate;
        internal void LogMessage(string DebugMessage)
        {
            if (!DebugLogEnabled)
            {
                return;
            }
            string FileName = DebugDate + " - debug.log";
            using (StreamWriter sw = File.AppendText(Path.Combine(TrainPath, FileName)))
            {
                sw.WriteLine(DateTime.Now + " : " + DebugMessage);
            }
        }
    }
}
