using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Playwright;

namespace CopilotCredits;

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

	public async Task<int> ReadUsedCreditsAsync()
	{
		int? usedCredits = await TryReadUsedCreditsAsync(true, HeadlessTimeoutMilliseconds);

		if (usedCredits is not null)
		{
			return usedCredits.Value;
		}

		usedCredits = await TryReadUsedCreditsAsync(false, SignInTimeoutMilliseconds);
		return usedCredits ?? throw new InvalidOperationException("Sign in to GitHub in the opened browser.");
	}

	private async Task<int?> TryReadUsedCreditsAsync(bool headless, float timeoutMilliseconds)
	{
		using IPlaywright playwright = await Playwright.CreateAsync();
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
		return creditMatch.Success
			? int.Parse(creditMatch.Groups["used"].Value.Replace(",", ""))
			: null;
	}
}