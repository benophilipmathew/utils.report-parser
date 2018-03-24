using System.Configuration;

namespace ReportParser.Classes
{
    public class Constant
    {
        public static readonly int NoOfColumnInOneRow =
            int.Parse(ConfigurationManager.AppSettings["NoOfColumnInOneRow"].ToString());

        public static readonly int ExcelStartRow =
            int.Parse(ConfigurationManager.AppSettings["ExcelStartRow"].ToString());
        
        public static readonly string TempFolderPrefix = ConfigurationManager.AppSettings["TempFolderPrefix"].ToString();

        public static readonly string TempRootPath = ConfigurationManager.AppSettings["TempRootPath"].ToString();        
    }
}
