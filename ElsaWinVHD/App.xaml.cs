using ElsaWinVHD.ViewModel;
using System.Windows;

#if !NET35
using System.Text;
#endif

namespace ElsaWinVHD
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#if !NET35
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif

            var w = new MainWindow() 
            { 
                DataContext = new MainViewModel()
            };
            //this.MainWindow = w;
            w.Show();

        }
    }
}
