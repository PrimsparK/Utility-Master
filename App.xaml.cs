using System.Windows;
using UtilityMaster.Services;

namespace UtilityMaster;

public partial class App : System.Windows.Application
{
    public IDataService DataService { get; } = new DataService();

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        DatabaseService.InitializeDefaults(DatabaseService.CreateContext());
        
        var mainWin = new MainWindow();
        mainWin.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        DataService.Dispose();
        base.OnExit(e);
    }
}
