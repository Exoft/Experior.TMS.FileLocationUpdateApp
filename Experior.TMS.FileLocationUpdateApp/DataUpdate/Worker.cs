using System;
using System.Timers;
using Experior.TMS.FileLocationUpdateApp.Configuration;
using NLog;

namespace Experior.TMS.FileLocationUpdateApp.DataUpdate
{
    public class Worker : IDisposable
    {
        private readonly Timer _timer;
        private readonly AppConfiguration _appConfig = ConfigurationManager.Instance.ApplicationConfiguration;
        private readonly Logger _logger;
        private readonly object _syncObject = new Object();
        private bool _isCheckInProgress;
        private readonly DatabaseTools _dataBaseTools;

        public Worker()
        {
            _logger = LogManager.GetCurrentClassLogger();
            _timer = new Timer(_appConfig.Timeout);
            _timer.Elapsed += TimerElapsed;
            _dataBaseTools = new DatabaseTools(_appConfig);
        }

        public void Start()
        {
            _timer.Start();
            PerformCheckForNewRecords();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public bool IsCheckInProgress
        {
            get
            {
                lock (_syncObject)
                {
                    return _isCheckInProgress;
                }
            }
            set
            {
                lock (_syncObject)
                {
                    _isCheckInProgress = value;
                }
            }
        }
        
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (IsCheckInProgress) return;
                IsCheckInProgress = true;
                PerformCheckForNewRecords();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while checking for new records");
                LogManager.Flush();
            }
            finally
            {
                _isCheckInProgress = false;
            }
        }

        private void PerformCheckForNewRecords()
        {
            if (_appConfig.LastProcessedRecordId == 0)
            {
                _logger.Warn("LastProcessedRecordId = 0. Please specify correct Id in app.config");
                return;
            }

            _logger.Info("Check for newly added records started...");

            _dataBaseTools.Process();

            _logger.Info("Check finished.");
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }
        }
    }
}