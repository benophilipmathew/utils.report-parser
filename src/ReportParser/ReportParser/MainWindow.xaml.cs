using MahApps.Metro.Controls;
using ReportParser.ViewModel;

namespace ReportParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MasterViewModel();
        }
    }
}
