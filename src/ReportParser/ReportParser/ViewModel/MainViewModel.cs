using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportParser.ViewModel
{
    public class MainViewModel: INotifyPropertyChanged
    {
        #region Sub View Models

        private DropzoneViewModel dropzoneVM;
        public DropzoneViewModel DropzoneVM
        {
            get { return dropzoneVM; }
            set { dropzoneVM = value; }
        }

        private FooterViewModel footerVM;
        public FooterViewModel FooterVM
        {
            get { return footerVM; }
            set { footerVM = value; }
        }

        #endregion
        
        public MainViewModel()
        {
            this.DropzoneVM = new DropzoneViewModel();
            this.FooterVM = new FooterViewModel();
        }

        // Property Change Event
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
