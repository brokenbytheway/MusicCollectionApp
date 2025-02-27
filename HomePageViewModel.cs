using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace MusicCollectionApp
{
    public class HomePageViewModel : INotifyPropertyChanged
    {
        private UserControl _currentView;
        private string _currentTitle;

        public UserControl CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public string CurrentTitle
        {
            get => _currentTitle;
            set
            {
                _currentTitle = value;
                OnPropertyChanged();
            }
        }

        public HomePageViewModel()
        {
            // Устанавливаем страницу по умолчанию
            CurrentView = new HomeUserControl();
            CurrentTitle = "Home";
        }

        public void SwitchView(UserControl newView, string newTitle)
        {
            CurrentView = newView;
            CurrentTitle = newTitle;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
