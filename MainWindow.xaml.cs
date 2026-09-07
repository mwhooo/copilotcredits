using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
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
	
	// Carbon footprint calculation: ~5mg CO2 per credit (based on research)
	private const double CO2_PER_CREDIT_MG = 5.0;

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
			
			// Marshal UI updates back to the UI thread
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				ErrorText.IsVisible = false;
				UsedCreditsText.Text = credits.Used.ToString("N0");
				TotalCreditsText.Text = credits.Total.ToString("N0");
				
				// Calculate percentage
				double percentageUsed = credits.Total > 0 ? (double)credits.Used / credits.Total * 100 : 0;
				double percentageRemaining = 100 - percentageUsed;
				
				// Calculate money spent (1 credit = 0.01 euro)
				double moneySpent = credits.Used * 0.01;
				
				// Calculate carbon footprint (CO2 in grams)
				double co2Grams = (credits.Used * CO2_PER_CREDIT_MG) / 1000.0;
				
				UsageProgressBar.Value = percentageUsed;
				PercentageText.Text = $"{percentageUsed:F1}%";
				RemainingText.Text = $"{credits.Total - credits.Used:N0} remaining";
				MoneySpentText.Text = $"€{moneySpent:F2}";
				
				// Display carbon footprint
				if (this.FindControl<TextBlock>("CarbonText") is TextBlock carbonText)
				{
					if (co2Grams < 1)
					{
						carbonText.Text = $"{co2Grams * 1000:F0}mg";
					}
					else if (co2Grams < 1000)
					{
						carbonText.Text = $"{co2Grams:F2}g";
					}
					else
					{
						carbonText.Text = $"{co2Grams / 1000:F3}kg";
					}
				}
				
				// Change color based on usage
				if (percentageUsed > 85)
				{
					UsageProgressBar.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red);
				}
				else if (percentageUsed >= 50)
				{
					UsageProgressBar.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Orange);
				}
				else
				{
					UsageProgressBar.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Green);
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
			});
		}
		catch (Exception exception)
		{
			Debug.WriteLine($"Failed to load Copilot credits: {exception}");

			// Marshal error UI updates back to the UI thread
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				UsedCreditsText.Text = "--";
				TotalCreditsText.Text = "--";
				PercentageText.Text = "--";
				RemainingText.Text = "-- remaining";
				MoneySpentText.Text = "€0.00";
				UsageProgressBar.Value = 0;
				ErrorText.Text = "Unable to load credits. Check Chromium setup and GitHub sign-in.";
				ErrorText.IsVisible = true;
				
				if (this.FindControl<TextBlock>("CarbonText") is TextBlock carbonText)
				{
					carbonText.Text = "--";
				}
			});
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
			// Only try Windows notifications on Windows
			if (!OperatingSystem.IsWindows())
			{
				return;
			}

			// Write PowerShell script to a temp file to avoid escaping headaches
			string tempScript = Path.Combine(Path.GetTempPath(), $"notify_{Guid.NewGuid()}.ps1");
			
			// Escape single quotes for PowerShell
			string escapedTitle = title.Replace("'", "''");
			string escapedMessage = message.Replace("'", "''");
			
			// Embed the values directly in the script to avoid argument passing issues
			string scriptContent = $@"[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

$title = '{escapedTitle}'
$message = '{escapedMessage}'

$template = @""
<toast>
    <visual>
        <binding template=""ToastText02"">
            <text id=""1"">$title</text>
            <text id=""2"">$message</text>
        </binding>
    </visual>
</toast>
""@

$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
$xml.LoadXml($template)
$toast = New-Object Windows.UI.Notifications.ToastNotification $xml
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('CopilotCredits').Show($toast)
";
			
			File.WriteAllText(tempScript, scriptContent);
			
			var process = new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			
			var proc = Process.Start(process);
			if (proc != null)
			{
				proc.WaitForExit(5000);
				string error = proc.StandardError.ReadToEnd();
				if (!string.IsNullOrEmpty(error))
				{
					Debug.WriteLine($"PowerShell error: {error}");
				}
				else
				{
					Debug.WriteLine("Windows notification displayed successfully");
				}
				proc.Dispose();
			}
			
			// Clean up
			try { File.Delete(tempScript); } catch { }
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"ShowWindowsNotification exception: {ex.Message}");
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