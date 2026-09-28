using Microsoft.Playwright;
using OcharoMarketing.Application.Cards;
using OcharoMarketing.Application.Cards.Brokers;
using OcharoMarketing.Infrastructure.Cards.Settings;

namespace OcharoMarketing.Infrastructure.Cards.Brokers;

/// <summary>Integrates Playwright Chromium for offline HTML artwork and PDF rendering.</summary>
/// <remarks>Missing fonts, images, or overflowing essential copy fail the export.</remarks>
public sealed class PlaywrightDocumentRenderingBroker(CardRendererSettings settings) : IDocumentRenderingBroker
{
    public async Task<byte[]> RenderPngAsync(string html, CancellationToken cancellationToken)
    {
        return await RenderAsync(html, false, cancellationToken);
    }

    public async Task<byte[]> RenderPdfAsync(string html, CancellationToken cancellationToken)
    {
        return await RenderAsync(html, true, cancellationToken);
    }

    private async Task<byte[]> RenderAsync(string html, bool pdf, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using IPlaywright playwright = await Playwright.CreateAsync();
        await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            ExecutablePath = settings.ExecutablePath,
            Timeout = settings.TimeoutMilliseconds,
        });
        await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1080, Height = 1350 },
            DeviceScaleFactor = 1,
            JavaScriptEnabled = false,
            ServiceWorkers = ServiceWorkerPolicy.Block,
            Offline = true,
        });
        await context.RouteAsync("**/*", route => route.AbortAsync());
        IPage page = await context.NewPageAsync();
        page.SetDefaultTimeout(settings.TimeoutMilliseconds);
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.Load });
        await page.EvaluateAsync("async () => { await document.fonts.ready; await Promise.all([...document.images].map(i => i.decode())); }");
        bool fontsReady = await page.EvaluateAsync<bool>("() => document.fonts.check('600 72px CardSerif') && document.fonts.check('400 42px CardSans')");
        if (!fontsReady) throw new CardRenderingException("A bundled card font could not be loaded.");
        bool overflowing = await page.EvaluateAsync<bool>("() => [...document.querySelectorAll('[data-fit]')].some(e => e.scrollHeight > e.clientHeight + 1 || e.scrollWidth > e.clientWidth + 1)");
        if (overflowing) throw new CardRenderingException("Card text exceeds its layout. Shorten the copy before exporting.");
        cancellationToken.ThrowIfCancellationRequested();
        if (pdf)
        {
            return await page.PdfAsync(new PagePdfOptions
            {
                Format = "A4",
                Landscape = true,
                PrintBackground = true,
                PreferCSSPageSize = true,
            });
        }
        return await page.Locator("#card").ScreenshotAsync(new LocatorScreenshotOptions
        {
            Type = ScreenshotType.Png,
            Animations = ScreenshotAnimations.Disabled,
        });
    }
}
