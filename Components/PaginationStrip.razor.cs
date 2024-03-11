using Microsoft.AspNetCore.Components;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BarberApp_AdminPortal.Components
{
    public partial class PaginationStrip
    {
        [Parameter] public int TotalPages { get; set; }

        [Parameter] public int TotalRows { get; set; }


        private void UpdatePage(int currentIndex)
        {
            Value = currentIndex;
            ValueChanged.InvokeAsync(Value);
        }

    }
    public class PaginationStripModel : INotifyPropertyChanged
    {
        private int _currentIndex;

        public int CurrentIndex
        {
            get { return _currentIndex; }
            set
            {
                _currentIndex = value;
                NotifyPropertyChanged();
            }
        }


        private int _rowsPerPage;

        public int RowsPerPage
        {
            get { return _rowsPerPage; }
            set
            {
                _rowsPerPage = value;
                NotifyPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
