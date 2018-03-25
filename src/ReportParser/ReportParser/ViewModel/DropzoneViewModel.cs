using HtmlAgilityPack;
using ReportParser.Classes;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ReportParser.ViewModel
{
    public class DropzoneViewModel : INotifyPropertyChanged
    {
        #region Private

        private int endRowIndex = 0;
        private int siIpColumnIndex = 0;
        private int soIpColumnIndex = 0;

        #endregion

        #region Properties

        private MasterViewModel _master;
        public MasterViewModel master
        {
            get { return _master; }
            set
            {
                _master = value;
                PropertyChange();
            }
        }

        private string inputFilePath;
        public string InputFilePath
        {
            get { return inputFilePath; }
            set
            {
                inputFilePath = value;
                PropertyChange();
            }
        }

        // Parsing related
        public int rowsCount { get; set; }
        public DataSet dsData { get; set; }
        public string htmlPath { get; set; }

        #endregion

        public DropzoneViewModel(MasterViewModel vm)
        {
            this.master = vm;
        }

        public void StartParsing()
        {
            if (!string.IsNullOrWhiteSpace(inputFilePath) && File.Exists(inputFilePath))
            {
                this.master.bi.BusyOn();

                var tskConvertPDF2HTML = new Task(() => ConvertPDFToHTML());
                var tskConvertHTML2Dataset = tskConvertPDF2HTML.ContinueWith((t) => 
                {
                    ConvertHTMLToDataSet();

                    #region Delete HTML Data from client machine

                    this.master.bi.LogBusyProgress("Deleting html file..");
                    FileInfo f = new FileInfo(this.master.DropzoneVM.htmlPath);
                    if (f != null && f.Directory.Name.Contains(Constant.TempFolderPrefix))
                        f.Directory.Delete(true);

                    #endregion
                });
                var tskTableDataToExcel = tskConvertHTML2Dataset.ContinueWith((t) =>  
                {
                    this.master.bi.LogBusyProgress("Writing data to excel sheet..");
                    ConvertTableDataToExcelSheet();
                    master.bi.BusyOff();
                });

                tskConvertPDF2HTML.Start();
            }
        }
        
        #region Pasing Methods
        
        public Task ConvertPDFToHTML()
        {
            var tcs = new TaskCompletionSource<int>();

            this.htmlPath = string.Empty;
            SautinSoft.PdfFocus f = null;

            try
            {
                this.htmlPath = GetTempFolderPath() + DateTime.Now.ToFileTime() + ".html";

                this.master.bi.LogBusyProgress("Openning pdf file..");
                f = new SautinSoft.PdfFocus();
                f.OpenPdf(inputFilePath);

                if (f.PageCount > 0)
                {
                    this.master.bi.LogBusyProgress("Converting pdf to html..");
                    f.ToHtml(htmlPath);
                }
            }
            catch (Exception ex)
            {
                this.master.bi.BusyOff();
                this.htmlPath = string.Empty;
                Helper.LogError(this.GetType().Name, MethodBase.GetCurrentMethod().Name, ex);
            }
            finally
            {
                if (f != null)
                    f.ClosePdf();
            }

            return tcs.Task;
        }

        public Task ConvertHTMLToDataSet()
        {
            var tcs = new TaskCompletionSource<int>();
            rowsCount = 0;
            HtmlDocument pdfDoc = null;

            try
            {
                this.master.bi.LogBusyProgress("Creating table schema..");
                DataTable dtSourceIn = GenerateTableSchema();
                DataTable dtSourceOut = GenerateTableSchema();

                this.master.bi.LogBusyProgress("Loading html file..");
                if (File.Exists(this.htmlPath))
                {
                    pdfDoc = new HtmlDocument();
                    pdfDoc.Load(this.htmlPath);
                }

                if (pdfDoc != null && pdfDoc.DocumentNode != null)
                {
                    HtmlNodeCollection lstData = pdfDoc.DocumentNode.SelectNodes("//body").First().SelectNodes("//div");

                    this.master.bi.LogBusyProgress("Mapping html data..");

                    #region Map HTML

                    int i = 0;
                    foreach (HtmlNode ndDiv in lstData)
                    {
                        if (ndDiv != null)
                            TrackColumnHeaderIndex(ndDiv.ChildNodes[0].InnerText, i);

                        i++;
                    }

                    #endregion

                    this.master.bi.LogBusyProgress("Converting to tabular format..");

                    #region Convert to Table

                    int columnCount = Constant.NoOfColumnInOneRow;
                    int itemCount = (endRowIndex + (columnCount - 1)) - siIpColumnIndex;
                    rowsCount = (itemCount / columnCount) + 1;

                    int skipCount = 0;
                    for (int r = 0; r < rowsCount;)
                    {
                        skipCount = (r != 0 ? r : 1) * columnCount;

                        DataRow drSI = dtSourceIn.NewRow();
                        drSI["IP"] = lstData[siIpColumnIndex + skipCount]?.InnerText;
                        drSI["Traffic"] = lstData[(siIpColumnIndex + 1) + skipCount]?.InnerText;
                        drSI["TrafficPerc"] = lstData[(siIpColumnIndex + 2) + skipCount]?.InnerText;
                        dtSourceIn.Rows.Add(drSI);

                        DataRow drSO = dtSourceOut.NewRow();
                        drSO["IP"] = lstData[soIpColumnIndex + skipCount]?.InnerText;
                        drSO["Traffic"] = lstData[(soIpColumnIndex + 1) + skipCount]?.InnerText;
                        drSO["TrafficPerc"] = lstData[(soIpColumnIndex + 2) + skipCount]?.InnerText;
                        dtSourceOut.Rows.Add(drSO);

                        r++;
                    }

                    #endregion
                }

                this.dsData = new DataSet();

                if (dtSourceIn != null && dtSourceIn.Rows.Count > 0)
                    this.dsData.Tables.Add(dtSourceIn);

                if (dtSourceOut != null && dtSourceOut.Rows.Count > 0)
                    this.dsData.Tables.Add(dtSourceOut);
            }
            catch (Exception ex)
            {
                this.master.bi.BusyOff();
                Helper.LogError(this.GetType().Name, MethodBase.GetCurrentMethod().Name, ex);
            }

            return tcs.Task;
        }

        public Task ConvertTableDataToExcelSheet()
        {
            var tcs = new TaskCompletionSource<int>();

            try
            {
                Microsoft.Office.Interop.Excel.Application oXL = new Microsoft.Office.Interop.Excel.Application();
                int startRow = Constant.ExcelStartRow;

                oXL.Visible = true;
                oXL.UserControl = false;

                if (this.dsData != null && this.dsData.Tables.Count >= 2)
                {                    
                    Microsoft.Office.Interop.Excel._Workbook oWB = oXL.Workbooks.Add();
                    Microsoft.Office.Interop.Excel._Worksheet oSheet = (Microsoft.Office.Interop.Excel._Worksheet)oWB.ActiveSheet;

                    // Source IN
                    oSheet.Cells[(startRow - 1), 1] = "IP";
                    oSheet.Cells[(startRow - 1), 2] = "Traffic";
                    oSheet.Cells[(startRow - 1), 3] = "Traffic %";

                    // Source OUT
                    oSheet.Cells[(startRow - 1), 5] = "IP";
                    oSheet.Cells[(startRow - 1), 6] = "Traffic";
                    oSheet.Cells[(startRow - 1), 7] = "Traffic %";

                    Microsoft.Office.Interop.Excel.Range oRange = oSheet.get_Range("A2", "Z2");
                    oRange.Font.Bold = true;
                    oRange.VerticalAlignment = Microsoft.Office.Interop.Excel.XlVAlign.xlVAlignCenter;

                    for (int j = 0; j < this.rowsCount; j++)
                    {
                        oSheet.Cells[(j + startRow), 1] = dsData.Tables[0].Rows[j]["IP"];
                        oSheet.Cells[(j + startRow), 2] = dsData.Tables[0].Rows[j]["Traffic"];
                        oSheet.Cells[(j + startRow), 3] = dsData.Tables[0].Rows[j]["TrafficPerc"];

                        oSheet.Cells[(j + startRow), 5] = dsData.Tables[1].Rows[j]["IP"];
                        oSheet.Cells[(j + startRow), 6] = dsData.Tables[1].Rows[j]["Traffic"];
                        oSheet.Cells[(j + startRow), 7] = dsData.Tables[1].Rows[j]["TrafficPerc"];
                    }

                    oXL.UserControl = false;
                    oWB.Close();
                }
            }
            catch (Exception ex)
            {
                this.master.bi.BusyOff();
                Helper.LogError(this.GetType().Name, MethodBase.GetCurrentMethod().Name, ex);
            }

            return tcs.Task;
        }

        #endregion

        #region Utility Methods

        private string GetTempFolderPath()
        {
            string path = string.Empty;

            try
            {
                if (!Directory.Exists(Constant.TempRootPath))
                    Directory.CreateDirectory(Constant.TempRootPath);

                string tempFolderPath = Constant.TempRootPath + Constant.TempFolderPrefix + DateTime.Now.ToFileTime().ToString();
                if (!Directory.Exists(tempFolderPath))
                    Directory.CreateDirectory(tempFolderPath);

                path = tempFolderPath + @"\";
            }
            catch (Exception ex)
            {
                Helper.LogError(this.GetType().Name, MethodBase.GetCurrentMethod().Name, ex);
                throw;
            }

            return path;
        }

        private DataTable GenerateTableSchema(string tableName = null)
        {
            DataTable dt = new DataTable();

            try
            {
                if (!string.IsNullOrWhiteSpace(tableName))
                    dt.TableName = tableName;

                dt.Columns.Add("IP");
                dt.Columns.Add("Traffic");
                dt.Columns.Add("TrafficPerc");
            }
            catch (Exception ex)
            {
                Helper.LogError(this.GetType().Name, MethodBase.GetCurrentMethod().Name, ex);
                throw;
            }

            return dt;
        }

        bool isHeaderRowPassed = true;
        private void TrackColumnHeaderIndex(string innerText, int i)
        {
            try
            {
                switch (innerText.Trim())
                {
                    case "SourceIN": siIpColumnIndex = i; break;
                    case "SourceOUT":
                        {
                            isHeaderRowPassed = true;
                            soIpColumnIndex = i;
                        };
                        break;
                    case "Total":
                        {
                            if (isHeaderRowPassed)
                            {
                                endRowIndex = i;
                                isHeaderRowPassed = false;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Helper.LogError(this.GetType().Name, MethodBase.GetCurrentMethod().Name, ex);
                throw;
            }
        }

        #endregion

        #region Property Change Event

        public event PropertyChangedEventHandler PropertyChanged;

        private void PropertyChange([CallerMemberName] string property = "")
        {
            if (property == "InputFilePath")
            {
                StartParsing();
            }

            OnPropertyChanged(property);
        }

        protected void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        #endregion
    }
}
