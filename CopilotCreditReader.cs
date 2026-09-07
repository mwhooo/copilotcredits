using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Playwright;

namespace CopilotCredits;

public sealed record CreditInfo(int Used, int Total);

public sealed class CopilotCreditReader
{
	private const string CopilotFeaturesUrl = "https://github.com/settings/copilot/features";
	private const string CreditPattern = @"(?<used>[\d,]+)\s*/\s*(?<total>[\d,]+)\s+AI credits";
	private const int HeadlessTimeoutMilliseconds = 10_000;
	private const int SignInTimeoutMilliseconds = 300_000;

	private readonly string profileDirectory = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"CopilotCredits",
		"browser-profile");
	private bool chromiumInstallChecked;

	public async Task<CreditInfo> ReadUsedCreditsAsync()
	{
		CreditInfo? credits = await TryReadUsedCreditsAsync(true, HeadlessTimeoutMilliseconds);

		if (credits is not null)
		{
			return credits;
		}

		credits = await TryReadUsedCreditsAsync(false, SignInTimeoutMilliseconds);
		return credits ?? throw new InvalidOperationException("Sign in to GitHub in the opened browser.");
	}

	private async Task<CreditInfo?> TryReadUsedCreditsAsync(bool headless, float timeoutMilliseconds)
	{
		using IPlaywright playwright = await Playwright.CreateAsync();
		await EnsureChromiumInstalledAsync(playwright);
		await using IBrowserContext context = await playwright.Chromium.LaunchPersistentContextAsync(
			profileDirectory,
			new BrowserTypeLaunchPersistentContextOptions { Headless = headless });

		IPage page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
		await page.GotoAsync(CopilotFeaturesUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

		ILocator creditMeter = page.GetByText(new Regex(CreditPattern, RegexOptions.IgnoreCase)).First;

		try
		{
			await creditMeter.WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMilliseconds });
		}
		catch (TimeoutException)
		{
			return null;
		}

		Match creditMatch = Regex.Match(await creditMeter.InnerTextAsync(), CreditPattern, RegexOptions.IgnoreCase);
		if (creditMatch.Success)
		{
			int used = int.Parse(creditMatch.Groups["used"].Value.Replace(",", ""));
			int total = int.Parse(creditMatch.Groups["total"].Value.Replace(",", ""));
			return new CreditInfo(used, total);
		}
		return null;
	}

	private async Task EnsureChromiumInstalledAsync(IPlaywright playwright)
	{
		if (chromiumInstallChecked || File.Exists(playwright.Chromium.ExecutablePath))
		{
			chromiumInstallChecked = true;
			return;
		}

		int exitCode = await Task.Run(() => Microsoft.Playwright.Program.Main(new[] { "install", "chromium" }));
		if (exitCode != 0 || !File.Exists(playwright.Chromium.ExecutablePath))
		{
			throw new InvalidOperationException(
				"Chromium could not be installed. Run 'dotnet run' again with an internet connection.");
		}

		chromiumInstallChecked = true;
	}
}