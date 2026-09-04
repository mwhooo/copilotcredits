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
			
			// Calculate percentage
			double percentageUsed = credits.Total > 0 ? (double)credits.Used / credits.Total * 100 : 0;
			double percentageRemaining = 100 - percentageUsed;
			
			UsageProgressBar.Value = percentageUsed;
			PercentageText.Text = $"{percentageUsed:F1}%";
			RemainingText.Text = $"{credits.Total - credits.Used:N0} remaining";
			
			// Change color based on usage
			if (percentageUsed > 80)
			{
				UsageProgressBar.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red);
			}
			else if (percentageUsed > 50)
			{
				UsageProgressBar.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Orange);
			}
			else
			{
				UsageProgressBar.Foreground = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(255, 212, 86, 61));
			}
		}
		catch (Exception)
		{
			UsedCreditsText.Text = "--";
			TotalCreditsText.Text = "--";
			PercentageText.Text = "--";
			RemainingText.Text = "-- remaining";
			UsageProgressBar.Value = 0;
		}
		finally
		{
			isRefreshing = false;
		}
	}
}