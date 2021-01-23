using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDLibCore
{
    public class Helper : IDisposable
    {
        public int APIID { get; set; }
        public string APIHASH { get; set; }
        public string debugproxy { get; set; }
        public enums.DebugLevel debuglevel { get; set; }
        public int timeout { get; set; }

        public Helper()
        {
            debugproxy = "127.0.0.1:7890";
            timeout = 25000;
            debuglevel = enums.DebugLevel.LogOnly;
        }

        public void addlog(string data)
        {
            if (debuglevel != enums.DebugLevel.None)
            {
                Console.WriteLine("[TDlibCore] - {0}", data);
            }
        }

        public void Dispose()
        {
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}