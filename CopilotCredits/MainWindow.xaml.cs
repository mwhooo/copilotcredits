using Avalonia;
using Avalonia.Controls;
using System.Timers;

namespace CopilotCredits;

public partial class MainWindow : Window
{
	private readonly CopilotCreditReader creditReader = new();
	private readonly System.Timers.Timer refreshTimer;
	private bool isRefreshing;
	internal int minutesToWaitForRefresh = 1;

	public MainWindow()
	{
		InitializeComponent();
		refreshTimer = new System.Timers.Timer(TimeSpan.FromMinutes(minutesToWaitForRefresh).TotalMilliseconds);
		refreshTimer.Elapsed += RefreshTimer_Tick;
		refreshTimer.Start();
		_ = RefreshUsageAsync();
	}

	protected override void OnOpened(EventArgs e)
	{
		base.OnOpened(e);
		refreshTimer.Start();
	}

	private async void RefreshTimer_Tick(object? sender, EventArgs eventArgs)
	{
		await RefreshUsageAsync();
	}

	private async Task RefreshUsageAsync()
	{
		if (isRefreshing)
		{
			return;
		}

		isRefreshing = true;

		try
		{
			var credits = await creditReader.ReadUsedCreditsAsync();
			UsedCreditsText.Text = credits.Used.ToString("N0");
			TotalCreditsText.Text = credits.Total.ToString("N0");
		}
		catch (Exception)
		{
			UsedCreditsText.Text = "--";
			TotalCreditsText.Text = "--";
		}
		finally
		{
			isRefreshing = false;
		}
	}
}