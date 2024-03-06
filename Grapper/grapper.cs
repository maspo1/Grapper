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
using MongoDB.Bson;
using MongoDB.Driver;




public class SystemInfoService : ServiceBase
{
    private Timer timer = null;
    private MongoClient mongoClient;
    private IMongoDatabase database;
    private IMongoCollection<BsonDocument> collection;

    public SystemInfoService()
    {
        this.ServiceName = "SystemInfoService";
        // MongoDB 연결 초기화
        mongoClient = new MongoClient("mongodb://localhost:27017/"); // MongoDB 연결 문자열
        database = mongoClient.GetDatabase("local"); // 데이터베이스 이름
        collection = database.GetCollection<BsonDocument>("lovvv"); // 컬렉션 이름
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
        // BSON 도큐먼트 생성
        var systemInfo = new BsonDocument
        {
            { "timestamp", DateTime.UtcNow }
        };


        // Log disk usage
        var drivesArray = new BsonArray();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                
                var driveInfo = new BsonDocument
                {
                    { "Drive", drive.Name},
                    { "Available Space", drive.TotalFreeSpace/(1024*1024*1024)},
                    { "Total Size", drive.TotalSize / (1024 * 1024 * 1024)}
                };
                drivesArray.Add(driveInfo);

            }
        }
        systemInfo.Add("드라이브정보", drivesArray);

        // MongoDB에 문서 저장
        collection.InsertOne(systemInfo);
        //string logMessage = $"Drive {drive.Name}: Available Space: {drive.TotalFreeSpace / (1024 * 1024 * 1024)} GB, Total Size: {drive.TotalSize / (1024 * 1024 * 1024)} GB";
        //Log(logMessage);

        // Log network interface status
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            string logMessage = $"NIC {nic.Name}: Status: {nic.OperationalStatus}";
            Log(logMessage);
        }

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

        // OS 정보수집

        try
        {
            string registry_key = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registry_key))
            {
                if (key != null) 
                {
                    var productName = key.GetValue("ProductName")?.ToString();
                    var releaseId = key.GetValue("ReleaseId")?.ToString();
                    var displayName = key.GetValue("DisplayVersion")?.ToString();
                    string logMessage = $"OS Name: {productName}, Version: {displayName}, Release ID: {releaseId}";
                    Log(logMessage);
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
