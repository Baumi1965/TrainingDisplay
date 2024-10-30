using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.MariaDB;
using Serilog.Sinks.MariaDB.Extensions;

namespace TrainingDisplay;

sealed class Program
{
    public static string? ConnectionString;
    public static int Location;
    public static string? LogLevel;
    public static string? AssemblyName;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            var settings = GetConfig();
            ConnectionString = settings.GetConnectionString("trainingsql");
            Location = Convert.ToInt32(settings["Spielstaette"]);
            LogLevel = settings["LogLevel"];
            
            SelfLog.Enable(msg => Debug.WriteLine(msg));

            var propertiesToColumn = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                ["Exception"] = "Exception",
                ["Level"] = "LogLevel",
                ["Message"] = "LogMessage",
                ["MessageTemplate"] = "LogMessageTemplate",
                ["Properties"]= "Properties",
                ["Action"] = "Message",
                ["Timestamp"] = "Timestamp",
                ["ExtendedInfo"] = "ExtendedInfo",
                ["Hostname"] = "Hostname",
                ["User"] = "User",
                ["Application"] = "Application",
                ["Class"] = "Class",
                ["Method"] = "Method",
                ["Location"] = "Location",
                ["Value1"] = "Value1",
                ["Value2"] = "Value2",
                ["Value3"] = "Value3",
                ["Value4"] = "Value4",
                ["Value5"] = "Value5",
            };

            MariaDBSinkOptions sinkOpions = new MariaDBSinkOptions()
            {
                PropertiesToColumnsMapping = propertiesToColumn,
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.Debug()
                .WriteTo.MariaDB(
                    connectionString: ConnectionString?.Replace("XpoProvider=MySql;", ""),
                    tableName: "logging",
                    autoCreateTable: true,
                    useBulkInsert: false,
                    options: sinkOpions)
                .CreateLogger();

            AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            
            Log.Information("{@Hostname} {@Application} {@User} {@Action} {@ExtendedInfo} {@Class} {@Method} {@Location} {@Value1} {@Value2} {@Value3} {@Value4} {@Value5}",
                Environment.MachineName, AssemblyName, Environment.UserName, "Application started", "", "Program",
                "Main", Location.ToString(), "", "", "", "", "");
           
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e,
                "{@Hostname} {@Application} {@User} {@Action} {@ExtendedInfo} {@Class} {@Method} {@Location} {@Value1} {@Value2} {@Value3} {@Value4} {@Value5}",
                Environment.MachineName, AssemblyName, Environment.UserName, e.Message, e.StackTrace, "Program",
                "Main", Location.ToString(), "", "", "", "", "");
        }
        finally
        {
            Log.Information("{@Hostname} {@Application} {@User} {@Action} {@ExtendedInfo} {@Class} {@Method} {@Location} {@Value1} {@Value2} {@Value3} {@Value4} {@Value5}",
                Environment.MachineName, AssemblyName, Environment.UserName, "Application closed", "", "Program",
                "Main", Location.ToString(), "", "", "", "", ""); 
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static IConfiguration GetConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        
        return builder.Build();
    }
}