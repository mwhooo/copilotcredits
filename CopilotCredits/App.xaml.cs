using Avalonia;
using Avalonia.Markup.Xaml;

namespace CopilotCredits;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Create and show main window
        var lifetime = ApplicationLifetime;
        if (lifetime is not null)
        {
            var mainWindowType = typeof(MainWindow);
            var mainWindow = (MainWindow)Activator.CreateInstance(mainWindowType)!;
            
            var lifetimeType = lifetime.GetType();
            var mainWindowProperty = lifetimeType.GetProperty("MainWindow");
            if (mainWindowProperty is not null && mainWindowProperty.CanWrite)
            {
                mainWindowProperty.SetValue(lifetime, mainWindow);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}