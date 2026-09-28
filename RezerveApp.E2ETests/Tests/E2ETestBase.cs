using Microsoft.Playwright;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace RezerveApp.E2ETests.Tests;

public abstract class E2ETestBase
{
    protected IPlaywright Playwright { get; private set; } = null!;
    protected IBrowser Browser { get; private set; } = null!;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;
    
    private Process? _appProcess;
    protected string AppUrl = "http://localhost:5001";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        var projectPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../RezerveApp"));
        
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --urls \"http://127.0.0.1:5001\"",
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            EnvironmentVariables =
            {
                ["USE_IN_MEMORY_DB"] = "true"
            }
        };

        _appProcess = Process.Start(psi);
        
        // Wait for the server to start
        await Task.Delay(5000); 
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_appProcess != null && !_appProcess.HasExited)
        {
            _appProcess.Kill(true);
            _appProcess.Dispose();
        }
        await Browser.CloseAsync();
        Playwright.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        Context = await Browser.NewContextAsync();
        Page = await Context.NewPageAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await Context.CloseAsync();
    }
}
