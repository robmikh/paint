using Paint.ViewModels;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Paint
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            _viewModel = new MainPageViewModel(CanvasRectangle);
            DataContext = _viewModel;
        }

        private MainPageViewModel _viewModel;
    }
}
