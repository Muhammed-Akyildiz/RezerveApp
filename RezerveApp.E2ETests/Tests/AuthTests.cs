using Microsoft.Playwright;
using NUnit.Framework;
using System.Threading.Tasks;

namespace RezerveApp.E2ETests.Tests;

[TestFixture]
public class AuthTests : E2ETestBase
{
    [Test]
    public async Task LandingPage_ShouldLoadSuccessfully()
    {
        await Page.GotoAsync(AppUrl);
        
        // Ana sayfada "RezerveApp" veya benzeri bir metnin olduğunu doğrula
        var isTitleCorrect = await Page.TitleAsync();
        Assert.That(isTitleCorrect, Does.Contain("RezerveApp").Or.Contain("Ana Sayfa"));

        // Randevu Al, Hemen Başla vb. butonlar görünür olmalı
        var heroCta = Page.Locator("text=Ücretsiz Başla").First;
        await Expect(heroCta).ToBeVisibleAsync();
    }

    [Test]
    public async Task Register_And_Login_Flow_ShouldWork()
    {
        // 1. Kayıt sayfasına git
        await Page.GotoAsync($"{AppUrl}/Account/Register");
        
        // 2. Formu doldur
        var email = $"test_user_{System.Guid.NewGuid().ToString().Substring(0,8)}@test.com";
        await Page.FillAsync("input[name='email']", email);
        await Page.FillAsync("input[name='password']", "Test1234");
        await Page.FillAsync("input[name='confirmPassword']", "Test1234");
        await Page.CheckAsync("#terms");
        
        // 3. Submit
        await Page.ClickAsync("button[type='submit']");
        
        // 4. Doğrulama başarılıysa muhtemelen işletme oluşturma sayfasına (/Business/Create) 
        // veya Login sayfasına yönlendirir (otomatik login olursa dashboard/create).
        // Kod yapısına göre Register sonrası otomatik login yapıp Business/Create'e atıyordu.
        await Page.WaitForURLAsync(url => url.Contains("/Business/Create") || url.Contains("/Account/Login"));
        
        // Eğer login'e attıysa login olalım
        if (Page.Url.Contains("/Account/Login"))
        {
            await Page.FillAsync("input[name='email']", email);
            await Page.FillAsync("input[name='password']", "Test1234");
            await Page.ClickAsync("button[type='submit']");
        }
        
        // Giriş yaptıktan sonra /Business/Create sayfasında olmalı
        await Expect(Page.Locator("text=İşletmeyi Oluştur")).ToBeVisibleAsync();
    }
    
    // Playwright NUnit'de Expect kullanmak için yardımcı metod (LocatorAssertions)
    private ILocatorAssertions Expect(ILocator locator) => Microsoft.Playwright.Assertions.Expect(locator);
}
