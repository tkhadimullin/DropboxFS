using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using Dokan;
using DropNet;
using DropboxFS.DropboxBackend;
using DropboxFS.Framework;
using NLog;

namespace DropboxFS
{
    [ExcludeFromCodeCoverage]
    [WindowsService("DropboxFSService",
        DisplayName = "DropboxFSService",
        Description = "Represents a Dropbox account as a local drive",
        EventLogSource = "DropboxFSService",
        StartMode = ServiceStartMode.Automatic,
        CanPauseAndContinue = false)]
    public class MainService : IWindowsService
    {
        private readonly Logger _log;
        private Thread _dokanThread;
        private ApplicationConfig _config;

        public MainService()
        {
            _log = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }

        /// <summary>
        /// This method is called when the service gets a request to start.
        /// </summary>
        /// <param name="args">Any command line arguments</param>
        public void OnStart(string[] args)
        {
            _log.Trace("Service start");
                     
            List<DropNetClient> dropNetClients;
            do
            {
                _log.Trace("Reading configuration...");
                Thread.Sleep(15000);
                //initialize from config including list of client accounts            
                _config = new ApplicationConfig();
                dropNetClients = (from ClientAccount account in _config.ClientAccounts select new DropNetClient(_config.ApplicationKey, _config.ApplicationSecret, account.Key, account.Secret)).ToList();    
            } while (dropNetClients.Count < 1);
            _dokanThread = new Thread(() => DokanNet.DokanMain(new DokanOptions { 
                                                                    DebugMode = false,
                                                                    MountPoint = string.Format("{0}:\\", _config.DriveLetter), 
                                                                    ThreadCount = 0, 
                                                                    UseKeepAlive = true, 
                                                                    RemovableDrive = true, 
                                                                    NetworkDrive = true},
                                                               new DropboxImplementation(dropNetClients)));
            _dokanThread.Start();
        }

        /// <summary>
        /// This method is called when the service gets a request to stop.
        /// </summary>
        public void OnStop()
        {
            _log.Trace("Service stop");
            DokanNet.DokanUnmount(_config.DriveLetter);
            _dokanThread.Join();
        }

        /// <summary>
        /// This method is called when a service gets a request to pause, 
        /// but not stop completely.
        /// </summary>
        public void OnPause()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This method is called when a service gets a request to resume 
        /// after a pause is issued.
        /// </summary>
        public void OnContinue()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This method is called when the machine the service is running on
        /// is being shutdown.
        /// </summary>
        public void OnShutdown()
        {
            _log.Trace("Service shutdown");
            DokanNet.DokanUnmount(_config.DriveLetter);
            _dokanThread.Join();
        }

        /// <summary>
        /// This method is called when a custom command is issued to the service.
        /// </summary>
        /// <param name="command">The command identifier to execute.</param >
        public void OnCustomCommand(int command)
        {
            _log.Trace("Service custom command");
        }
    }
}
