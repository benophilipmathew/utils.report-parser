using HtmlAgilityPack;
using ReportParser.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportParser.ViewModel
{
    public class DropzoneViewModel : INotifyPropertyChanged
    {
        #region Properties

        private string inputFilePath;
        public string InputFilePath
        {
            get { return inputFilePath; }
            set
            {
                inputFilePath = value;
                PropertyChange("InputFilePath");
            }
        }

        #endregion

        public DropzoneViewModel()
        {

        }

        public void StartParsing()
        {
            if (!string.IsNullOrWhiteSpace(inputFilePath) && File.Exists(inputFilePath))
            {
                Helper h = new Helper();

                // Convert pdf to html
                string htmlFilePath = h.ConvertPDFToHTML(inputFilePath);

                // Parse HTML File - Convert html data to data table
                int pdfRowCount;
                DataSet dsData = h.ConvertHTMLToDataSet(htmlFilePath, out pdfRowCount);

                #region Delete HTML Data from client machine

                FileInfo f = new FileInfo(htmlFilePath);
                if (f != null && f.Directory.Name.Contains(Constant.TempFolderPrefix))
                    f.Directory.Delete(true);

                #endregion

                // Write tabular data into excel
                h.ConvertTableDataToExcelSheet(dsData, pdfRowCount);
            }
        }
        
        #region Property Change Event

        public event PropertyChangedEventHandler PropertyChanged;

        private void PropertyChange(string propertyName)
        {
            if (propertyName == "InputFilePath")
            {
                StartParsing();
            }

            OnPropertyChanged(propertyName);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
