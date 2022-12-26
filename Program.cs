using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using System.Runtime.InteropServices;

namespace LocSel;

internal static class Program
{
    // Needed to hide window
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
    private static void Main()
    {
        // Create chrome service
        var chromeService = ChromeDriverService.CreateDefaultService();
        chromeService.HideCommandPromptWindow = true;

        // Create Chrome driver options
        var options = new ChromeOptions();
        
        // Grant access to geolocation
        options.AddUserProfilePreference("profile.default_content_setting_values.geolocation", 1);
        
        // Workaround for starting minimized
        options.AddArgument("--window-position=-32000,-32000");
        
        // Get chrome processes before starting webdriver
        var pids = Process
            .GetProcessesByName("chrome")
            .Select(x => x.Id)
            .ToList();
        
        // Create Chrome driver instance
        WebDriver chromeDriver = new ChromeDriver(chromeService, options);

        // Get new chrome processes 
        var processes  = Process
            .GetProcessesByName("chrome")
            .Where(x => !pids.Contains(x.Id))
            .ToList();
        
        // Hide new chrome processes windows
        processes.ForEach(x => ShowWindow(x.MainWindowHandle, 0));
        
        // Navigate to microsoft.com
        chromeDriver.Navigate().GoToUrl("https://www.microsoft.com/");
        
        // Get users geolocation
        var geolocationJson = (string)chromeDriver.ExecuteAsyncScript("var callback = arguments[arguments.length - 1];navigator.geolocation.getCurrentPosition((loc)=>{loc = loc.coords;callback(JSON.stringify({accuracy: loc.accuracy,latitude: loc.latitude,longitude: loc.longitude}));});");
        
        // Close chrome driver
        chromeDriver.Quit();

        // Kill new processes 
        processes.ForEach(x => x.Kill());
        
        // Kill chromedriver processes
        Process
            .GetProcessesByName("chromedriver")
            .ToList()
            .ForEach(x => x.Kill());
        
        // Write geolocation to desktop
        File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\geolocation.json", geolocationJson);
    }
}