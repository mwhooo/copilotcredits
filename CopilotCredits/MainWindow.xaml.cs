using Avalonia;
using Avalonia.Controls;
using System.Timers;
using System.Diagnostics;

namespace CopilotCredits;

public partial class MainWindow : Window
{
	private readonly CopilotCreditReader creditReader = new();
	private readonly System.Timers.Timer refreshTimer;
	private bool isRefreshing;
	private double lastNotifiedPercentage = -1;
	internal int minutesToWaitForRefresh = 1;
	internal bool notifyOnPercentageThreshold = true;
	internal int percentageThreshold = 5;

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
			
			// Check if we should notify about threshold
			if (notifyOnPercentageThreshold && percentageUsed >= percentageThreshold)
			{
				if (lastNotifiedPercentage < 0 || percentageUsed - lastNotifiedPercentage >= percentageThreshold)
				{
					ShowThresholdNotification(percentageUsed, credits);
					lastNotifiedPercentage = percentageUsed;
				}
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

	private void ShowThresholdNotification(double percentageUsed, CreditInfo credits)
	{
		try
		{
			string title = "Copilot Credits Alert";
			string message = $"You have used {percentageUsed:F1}% of your AI credits ({credits.Used:N0} / {credits.Total:N0})";
			
			// Try to show system notification
			if (OperatingSystem.IsWindows())
			{
				ShowWindowsNotification(title, message);
			}
			else if (OperatingSystem.IsMacOS())
			{
				ShowMacOSNotification(title, message);
			}
			else if (OperatingSystem.IsLinux())
			{
				ShowLinuxNotification(title, message);
			}
		}
		catch (Exception ex)
		{
			// If notification fails, silently continue
			Debug.WriteLine($"Failed to show notification: {ex.Message}");
		}
	}

	private void ShowWindowsNotification(string title, string message)
	{
		try
		{
			// Use PowerShell to show Windows notification
			// Build XML with proper escaping
			string xmlContent = $"<toast><visual><binding template=\"ToastText02\"><text id=\"1\">{System.Xml.XmlConvert.EncodeName(title)}</text><text id=\"2\">{System.Xml.XmlConvert.EncodeName(message)}</text></binding></visual></toast>";
			
			// Use Base64 encoding to avoid quote escaping issues
			string base64Xml = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xmlContent));
			
			string psCommand = $"[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications] > $null; " +
				$"[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument] > $null; " +
				$"$xml = New-Object Windows.Data.Xml.Dom.XmlDocument; " +
				$"$xmlString = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{base64Xml}')); " +
				$"$xml.LoadXml($xmlString); " +
				$"$toast = New-Object Windows.UI.Notifications.ToastNotification $xml; " +
				$"[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('CopilotCredits').Show($toast)";
			
			var process = new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = $"-NoProfile -Command \"{psCommand}\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};
			
			Process.Start(process);
		}
		catch
		{
			// Fallback: silently fail
		}
	}

	private void ShowMacOSNotification(string title, string message)
	{
		try
		{
			// Use osascript for macOS notifications
			string escapedMessage = message.Replace("\"", "\\\"");
			string script = $"display notification \"{escapedMessage}\" with title \"{title}\"";
			
			var process = new ProcessStartInfo
			{
				FileName = "/usr/bin/osascript",
				Arguments = $"-e '{script}'",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};
			
			Process.Start(process);
		}
		catch
		{
			// Fallback: silently fail
		}
	}

	private void ShowLinuxNotification(string title, string message)
	{
		try
		{
			// Use notify-send for Linux
			var process = new ProcessStartInfo
			{
				FileName = "notify-send",
				Arguments = $"\"{title}\" \"{message}\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};
			
			Process.Start(process);
		}
		catch
		{
			// Fallback: silently fail
		}
	}
}