using System;
using System.IO;

namespace Activity_Finder.Services
{
    public static class AppLogger
    {
        private static readonly object LockObject = new object();
        private static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_errors.log");

        public static void Log(Exception ex)
        {
            try
            {
                lock (LockObject)
                {
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
                }
            }
            catch { }
        }
    }
}
