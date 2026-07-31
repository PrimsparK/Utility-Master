using System.Windows;
using UtilityMaster.Services;

namespace UtilityMaster;

public partial class App : System.Windows.Application
{
    public IDataService DataService { get; } = new DataService();

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        using (var db = DatabaseService.CreateContext())
        {
            DatabaseService.InitializeDefaults(db);
        }
        
        var mainWin = new MainWindow();
        mainWin.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        DataService.Dispose();
        base.OnExit(e);
    }
}
