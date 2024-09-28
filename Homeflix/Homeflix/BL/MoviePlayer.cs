using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homeflix.BL
{
    public class MoviePlayer
    {
        private Process vlcProcess;
        private string vlcPlayerPath = "";        
        public MoviePlayer(string vlcPlayerPath) 
        {
            this.vlcPlayerPath = vlcPlayerPath;
        }

        public void Play(string videoPath, int startTimeSeconds)
        {
            string arguments = $"\"{videoPath}\" --start-time={startTimeSeconds}";
            vlcProcess = Process.Start(vlcPlayerPath, arguments);
        }

        public void Stop()
        {
            if (!vlcProcess.HasExited)
            {
                vlcProcess.Kill();
            }
        }
    }
}
