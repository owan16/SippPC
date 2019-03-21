using CrashReporterDotNET;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading; // DispatcherUnhandledExceptionEventArgs

namespace Sipp_PC
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
#if (!DEBUG)

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Application.Current.DispatcherUnhandledException += DispatcherOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
#endif
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            SendReport(unobservedTaskExceptionEventArgs.Exception);
            Environment.Exit(0);
        }

        private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
        {
            SendReport(dispatcherUnhandledExceptionEventArgs.Exception);
            Environment.Exit(0);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            SendReport((Exception)unhandledExceptionEventArgs.ExceptionObject);
            Environment.Exit(0);
        }

        public static void SendReport(Exception exception, string developerMessage = "")
        {
            var reportCrash = new ReportCrash("code@thomasy.tw")
            {
                DeveloperMessage = developerMessage
            };
            reportCrash.DoctorDumpSettings = new DoctorDumpSettings
            {
                ApplicationID = new Guid("a85ec548-daf5-483b-bbad-1f231e156955"),
            };
            reportCrash.Send(exception);
        }
    }
}
