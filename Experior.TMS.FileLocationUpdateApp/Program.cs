using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Timers;
using Experior.TMS.FileLocationUpdateApp.Configuration;
using Experior.TMS.FileLocationUpdateApp.DataUpdate;
using Experior.TMS.FileLocationUpdateApp.Entities;
using NLog;
using ConfigurationManager = Experior.TMS.FileLocationUpdateApp.Configuration.ConfigurationManager;

namespace Experior.TMS.FileLocationUpdateApp
{
    internal class Program
    {
        private static Logger _logger;

        public static void Main(string[] args)
        {
            _logger = LogManager.GetCurrentClassLogger();
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            Worker worker = new Worker();
            worker.Start();
            Console.WriteLine("Press any key to exit.");

            Console.ReadKey();
            worker.Dispose();
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Error(e.ExceptionObject as Exception, "Unhandled exception occured: ");
            LogManager.Flush();
        }
   
    }
}