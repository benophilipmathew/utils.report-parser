using HtmlAgilityPack;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ReportParser.Classes
{
    public class Helper
    {
        #region Variable Declaration

        private int endRowIndex = 0;
        private int siIpColumnIndex = 0;
        private int soIpColumnIndex = 0;

        #endregion

        public string ConvertPDFToHTML(string inputFilePath)
        {
            string htmlFileTempPath = string.Empty;
            SautinSoft.PdfFocus f = null;

            try
            {
                htmlFileTempPath = GetTempFolderPath() + DateTime.Now.ToFileTime() + ".html";

                f = new SautinSoft.PdfFocus();
                f.OpenPdf(inputFilePath);

                if (f.PageCount > 0)
                {
                    f.ToHtml(htmlFileTempPath);
                }
            }
            catch (Exception ex)
            {
                htmlFileTempPath = string.Empty;
                LogError(this.GetType().Name, MethodBase.GetCurrentMethod().Name, ex);
            }
            finally
            {
                if (f != null)
                    f.ClosePdf();
            }

            return htmlFileTempPath;
        }

        public DataSet ConvertHTMLToDataSet(string htmlFilePath, out int rowsCount)
        {
            DataSet ds = new DataSet();
            rowsCount = 0;
            HtmlDocument pdfDoc = null;

            try
            {
                // Create table's to store parsed data
                DataTable dtSourceIn = GenerateTableSchema();
                DataTable dtSourceOut = GenerateTableSchema();

                // Load HTML File
                if (File.Exists(htmlFilePath))
                {
                    pdfDoc = new HtmlDocument();
                    pdfDoc.Load(htmlFilePath);
                }

                if (pdfDoc != null && pdfDoc.DocumentNode != null)
                {
                    HtmlNodeCollection lstData = pdfDoc.DocumentNode.SelectNodes("//body").First().SelectNodes("//div");

                    #region Map PDF by HTML

                    int i = 0;
                    foreach (HtmlNode ndDiv in lstData)
                    {
                        if (ndDiv != null)
                            TrackColumnHeaderIndex(ndDiv.ChildNodes[0].InnerText, i);

                        i++;
                    }

                    #endregion

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

                if (dtSourceIn != null && dtSourceIn.Rows.Count > 0)
                    ds.Tables.Add(dtSourceIn);

                if (dtSourceOut != null && dtSourceOut.Rows.Count > 0)
                    ds.Tables.Add(dtSourceOut);
            }
            catch (Exception ex)
            {
                LogError(this.GetType().Name, MethodBase.GetCurrentMethod().Name, ex);
            }

            return ds;
        }

        public void ConvertTableDataToExcelSheet(DataSet dsData, int pdfRowCount)
        {            
            try
            {
                int startRow = Constant.ExcelStartRow;

                Microsoft.Office.Interop.Excel.Application oXL = new Microsoft.Office.Interop.Excel.Application();
                oXL.Visible = true;
                oXL.UserControl = false;

                if (dsData != null && dsData.Tables.Count >= 2)
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

                    for (int j = 0; j < pdfRowCount; j++)
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
                LogError(this.GetType().Name, MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        #region Utility Methods

        private string GetTempFolderPath()
        {
            if (!Directory.Exists(Constant.TempRootPath))
                Directory.CreateDirectory(Constant.TempRootPath);

            string tempFolderPath = Constant.TempRootPath + Constant.TempFolderPrefix + DateTime.Now.ToFileTime().ToString();
            if (!Directory.Exists(tempFolderPath))
                Directory.CreateDirectory(tempFolderPath);

            return tempFolderPath + @"\";
        }

        private DataTable GenerateTableSchema(string tableName = null)
        {
            DataTable dt = new DataTable();

            if (!string.IsNullOrWhiteSpace(tableName))
                dt.TableName = tableName;

            dt.Columns.Add("IP");
            dt.Columns.Add("Traffic");
            dt.Columns.Add("TrafficPerc");

            return dt;
        }

        bool isHeaderRowPassed = true;
        private void TrackColumnHeaderIndex(string innerText, int i)
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

        #endregion

        #region Exception Handling

        public void LogError(string className, string methodName, Exception ex)
        {

        }

        #endregion
    }
}
