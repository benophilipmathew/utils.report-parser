using Microsoft.Win32;
using ReportParser.Classes;
using ReportParser.ViewModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ReportParser.View
{
    /// <summary>
    /// Interaction logic for vwDropzone.xaml
    /// </summary>
    public partial class vwDropzone : UserControl
    {
        public vwDropzone()
        {
            InitializeComponent();
        }

        #region Action Events

        private void pdfDropzone_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string inputPath = files.Distinct().First();
                if (!string.IsNullOrWhiteSpace(inputPath) && File.Exists(inputPath))
                {
                    DropzoneViewModel vm = this.DataContext as DropzoneViewModel;
                    if (vm != null)
                    {
                        vm.InputFilePath = inputPath;

                        if (Constant.IsAutoCloseOn)
                            Application.Current.Shutdown();
                    }
                }
            }
        }

        private void pdfDropzone_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Pro Tool";
            fileDialog.InitialDirectory = @"c:\";
            fileDialog.Filter = "PDF Files|*.pdf";
            fileDialog.RestoreDirectory = true;

            bool? rs = fileDialog.ShowDialog();

            if (rs != null && rs.Value)
            {
                DropzoneViewModel vm = this.DataContext as DropzoneViewModel;
                if (vm != null)
                {
                    vm.InputFilePath = fileDialog.FileName;

                    if (Constant.IsAutoCloseOn)
                        Application.Current.Shutdown();
                }
            }

        }

        #endregion

        #region Style Events

        private void pdfDropzone_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            innerB.Style = this.FindResource("DropzoneInnerB_MouseEnter") as Style;
            outerB.Style = this.FindResource("DropzoneOuterB_MouseEnter") as Style;
        }

        private void pdfDropzone_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            innerB.Style = this.FindResource("DropzoneInnerB_MouseLeave") as Style;
            outerB.Style = this.FindResource("DropzoneOuterB_MouseLeave") as Style;
        }

        #endregion        
    }
}
