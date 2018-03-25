using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ReportParser.ViewModel
{
    public class BusyIndicatorViewModel: INotifyPropertyChanged
    {
        #region Properties

        private bool isBusy;
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                PropertyChange();
            }
        }

        private Visibility isVisible;
        public Visibility IsVisible
        {
            get { return isVisible; }
            set
            {
                isVisible = value;
                PropertyChange();
            }
        }

        private string progressDetail;
        public string ProgressDetail
        {
            get { return progressDetail; }
            set
            {
                progressDetail = value;
                PropertyChange();
            }
        }

        #endregion
        
        public void BusyOn(string message = null)
        {
            this.IsBusy = true;
            LogBusyProgress(message);
        }

        public void LogBusyProgress(string message = null)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                this.ProgressDetail += "\n" + message;
            }
        }

        public void BusyOff()
        {
            this.IsBusy = false;
            this.ProgressDetail = null;
        }

        #region Property Change Event

        public event PropertyChangedEventHandler PropertyChanged;

        private void PropertyChange([CallerMemberName] string property = "")
        {
            if (property == "IsBusy")
            {
                this.IsVisible = this.IsBusy ? Visibility.Visible : Visibility.Collapsed;
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
