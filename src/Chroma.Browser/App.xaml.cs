using System.Windows;
using Chroma.Browser.Services;

namespace Chroma.Browser;

public partial class App : Application
{
    private readonly LogService _log = LogService.Instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            _log.Error("Unhandled UI exception", args.Exception);
            MessageBox.Show(
                "Chroma Browser столкнулся с ошибкой. Подробности сохранены в журнале.",
                "Chroma Browser",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            _log.Error("Unhandled process exception", args.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _log.Error("Unobserved task exception", args.Exception);
            args.SetObserved();
        };

        _log.Info("Application starting");
        new MainWindow(false, e.Args.FirstOrDefault()).Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _log.Info($"Application exiting with code {e.ApplicationExitCode}");
        base.OnExit(e);
    }
}
