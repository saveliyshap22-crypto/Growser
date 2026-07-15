using System.Threading;
using System.Windows;
using Xunit;

namespace Chroma.Browser.Tests;

public sealed class MainWindowStartupTests
{
    [Fact]
    public void MainWindow_Xaml_Can_Be_Parsed_At_Runtime()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = Application.Current as App ?? new App();
                app.InitializeComponent();
                var window = new MainWindow();
                if (window.Content is null)
                {
                    throw new InvalidOperationException("MainWindow content was not created.");
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "MainWindow XAML startup test timed out.");
        Assert.Null(failure);
    }
}
