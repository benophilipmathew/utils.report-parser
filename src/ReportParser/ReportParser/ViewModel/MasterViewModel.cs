using System.ComponentModel;

namespace ReportParser.ViewModel
{
    public class MasterViewModel: INotifyPropertyChanged
    {
        #region Properties - Sub View Models
        
        public DropzoneViewModel DropzoneVM { get; set; }
        public BusyIndicatorViewModel bi { get; set; }
        public FooterViewModel FooterVM { get; set; }

        #endregion
        
        public MasterViewModel()
        {
            this.bi = new BusyIndicatorViewModel();
            this.bi.BusyOn("Application loading...");

            this.DropzoneVM = new DropzoneViewModel(this);
            this.FooterVM = new FooterViewModel();

            this.bi.BusyOff();
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
