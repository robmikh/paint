using Paint.ViewModels;

namespace Paint
{
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            _viewModel = new MainPageViewModel(CanvasRectangle);
            DataContext = _viewModel;
        }

        private void NewFlyoutButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            NewFlyout.Hide();
        }

        private MainPageViewModel _viewModel;
    }
}
