using System.Linq;
using System.Reflection;

namespace ReportParser.ViewModel
{
    public class FooterViewModel
    {
        public string FooterTitle { get; set; }

        public FooterViewModel()
        {
            Assembly a = Assembly.GetExecutingAssembly();

            string version = a.GetName().Version.Major.ToString() 
                + (a.GetName().Version.Minor > 0 ? "." + a.GetName().Version.Minor.ToString() : string.Empty);
            AssemblyTitleAttribute title = a.GetCustomAttributes(typeof(AssemblyTitleAttribute), false).FirstOrDefault() as AssemblyTitleAttribute;

            if (title != null)
                this.FooterTitle = title.Title + " - v" + version;
        }
    }
}
