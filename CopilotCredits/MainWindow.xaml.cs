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
			// Only try Windows notifications on Windows
			if (!OperatingSystem.IsWindows())
			{
				return;
			}

			// Write PowerShell script to a temp file to avoid escaping headaches
			string tempScript = Path.Combine(Path.GetTempPath(), $"notify_{Guid.NewGuid()}.ps1");
			
			// Use @"..." for the here-string to avoid escaping issues
			string scriptContent = @"[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

$title = $args[0]
$message = $args[1]

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
				Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\" -ArgumentList \"{title}\", \"{message}\"",
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