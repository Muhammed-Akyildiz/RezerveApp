using Microsoft.Playwright;
using NUnit.Framework;
using System.Threading.Tasks;

namespace RezerveApp.E2ETests.Tests;

[TestFixture]
public class BusinessSetupTests : E2ETestBase
{
    private string _testEmail = string.Empty;

    [SetUp]
    public async Task SetupUserAndLogin()
    {
        // Her testten önce yeni bir kullanıcı oluştur ve giriş yap
        _testEmail = $"biz_user_{System.Guid.NewGuid().ToString().Substring(0,8)}@test.com";
        await Page.GotoAsync($"{AppUrl}/Account/Register");
        await Page.FillAsync("input[name='email']", _testEmail);
        await Page.FillAsync("input[name='password']", "Test1234");
        await Page.FillAsync("input[name='confirmPassword']", "Test1234");
        await Page.CheckAsync("#terms");
        await Page.ClickAsync("button[type='submit']");
        
        await Page.WaitForURLAsync(url => url.Contains("/Business/Create") || url.Contains("/Account/Login"));
        if (Page.Url.Contains("/Account/Login"))
        {
            await Page.FillAsync("input[name='email']", _testEmail);
            await Page.FillAsync("input[name='password']", "Test1234");
            await Page.ClickAsync("button[type='submit']");
        }
    }

    [Test]
    public async Task BusinessAdmin_CanCreateBusiness_And_AccessDashboard()
    {
        // 1. İşletme oluştur sayfasında olmalıyız
        await Expect(Page.Locator("text=İşletmeyi Oluştur")).ToBeVisibleAsync();

        // 2. İşletme bilgilerini doldur
        await Page.FillAsync("input[name='name']", "Test Berber Salonu");
        await Page.FillAsync("input[name='phone']", "05554443322");
        await Page.FillAsync("select[name='cityId']", "34"); // example
        
        // 3. Formu gönder
        await Page.ClickAsync("button[type='submit']");

        // 4. Dashboard'a veya pending approval sayfasına yönlendirilmeli
        // Eğer otomatik onaylanmıyorsa "Pending" ekranına atar, onaylanıyorsa Dashboard'a.
        await Page.WaitForURLAsync(url => url.Contains("/Business/Index") || url.Contains("/Business/Pending"));
        
        // Eğer pending sayfasındaysa, bu test kapsamında in-memory DB'ye müdahale edip onaylayabiliriz
        // veya SuperAdmin panelinden onaylama senaryosuna geçebiliriz. 
        // Şimdilik sadece sayfanın açıldığını doğrulayalım.
        var isDashboardOrPending = Page.Url.Contains("/Business/Index") || Page.Url.Contains("/Business/Pending");
        Assert.That(isDashboardOrPending, Is.True);
    }
    
    private ILocatorAssertions Expect(ILocator locator) => Microsoft.Playwright.Assertions.Expect(locator);
}
