using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;



public class SystemInfoService : ServiceBase
{
    private Timer timer = null;

    public SystemInfoService()
    {
        this.ServiceName = "SystemInfoService";
    }

    protected override void OnStart(string[] args)
    {
        timer = new Timer();
        timer.Interval = 60000; // 60 seconds
        timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
        timer.Start();
    }

    protected override void OnStop()
    {
        timer.Stop();
        timer = null;
    }

    private void OnTimer(object sender, ElapsedEventArgs args)
    {
        // Log disk usage
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                string logMessage = $"Drive {drive.Name}: Available Space: {drive.TotalFreeSpace / (1024 * 1024 * 1024)} GB, Total Size: {drive.TotalSize / (1024 * 1024 * 1024)} GB";
                Log(logMessage);
            }
        }

        // Log network interface status
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            string logMessage = $"NIC {nic.Name}: Status: {nic.OperationalStatus}";
            Log(logMessage);
        }

        // Log OS version
        string osVersion = $"OS Version: {Environment.OSVersion.VersionString}";
        Log(osVersion);

        // Log build version
        string osBuildVersion = $"OS Build Version: {Environment.OSVersion.Version}";
        Log(osBuildVersion);

        // Log installed programs (Requires administrative privileges)
        string installedPrograms = "Installed Programs:";
        Log(installedPrograms);
        try
        {
            string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registry_key))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using (Microsoft.Win32.RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        string programName = subkey.GetValue("DisplayName") as string;
                        if (!string.IsNullOrEmpty(programName))
                        {
                            Log($"    {programName}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Error retrieving installed programs: {ex.Message}");
        }
    }

        private void Log(string message)
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SystemInfoLog.txt");
        using (StreamWriter sw = new StreamWriter(filePath, true))
        {
            sw.WriteLine($"{DateTime.Now}: {message}");
        }
    }

    public static void Main()
    {
        ServiceBase.Run(new SystemInfoService());
    }
}
