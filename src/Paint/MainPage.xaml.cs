using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Composition;
using Paint.Core;
using Paint.Drawing;
using Paint.Hardware;
using Paint.Tools;
using Robmikh.CompositionSurfaceFactory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Paint
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        private static DependencyProperty ColorIndexProperty = DependencyProperty.Register("ColorIndex", typeof(int), typeof(MainPage), new PropertyMetadata(0, ColorIndexPropertyChanged));

        private static void ColorIndexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var page = d as MainPage;
            var value = (int)e.NewValue;

            if (page != null)
            {
                page.SetColorIndex(value);
            }
        }

        private Compositor _compositor;
        private List<Color> _colors;
        private int _currentColorIndex;

        private PaintCore _core;

        public MainPage()
        {
            this.InitializeComponent();

            _compositor = Window.Current.Compositor;

            _core = new PaintCore(_compositor);

            CanvasRectangle.Width = _core.CurrentSize.X;
            CanvasRectangle.Height = _core.CurrentSize.Y;
            PencilToolButton.IsChecked = true;
            ElementCompositionPreview.SetElementChildVisual(CanvasRectangle, _core.Visual);

            InitColors();
            SetupInputHandler();

            DataContext = this;
        }

        private void InitColors()
        {
            var dpi = GraphicsInformation.Dpi;
            

            _colors = new List<Color>();
            _colors.Add(Colors.Black);
            _colors.Add(Colors.Red);
            _colors.Add(Colors.Blue);
            _colors.Add(Colors.Green);
            _colors.Add(Colors.Yellow);
            _colors.Add(Colors.White);
            _colors.Add(Colors.Brown);
            _colors.Add(Colors.Orange);
            _colors.Add(Colors.Purple);

            ColorGridView.ItemsSource = _colors;

            SetColorIndex(_colors.IndexOf(_core.Color));
        }

        private void OnCanvasSizeChanged(object sender, Vector2 e)
        {
            CanvasRectangle.Width = e.X;
            CanvasRectangle.Height = e.Y;
        }

        private void SetupInputHandler()
        {
            var coreWindow = Window.Current.CoreWindow;

            CanvasRectangle.PointerPressed += CanvasRectangle_PointerPressed;
            CanvasRectangle.PointerMoved += CanvasRectangle_PointerMoved;
            CanvasRectangle.PointerReleased += CanvasRectangle_PointerReleased;
            CanvasRectangle.PointerExited += CanvasRectangle_PointerExited;

            coreWindow.KeyUp += CoreWindow_KeyUp;
        }

        private void CanvasRectangle_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _core.CanvasPointerExited(sender, e);
        }

        private void CanvasRectangle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _core.CanvasPointerReleased(sender, e);
        }

        private void CanvasRectangle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _core.CanvasPointerMoved(sender, e);
        }

        private void CanvasRectangle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _core.CanvasPointerPressed(sender, e);
        }

        private bool IsKeyDown(VirtualKey key)
        {
            var coreWindow = Window.Current.CoreWindow;

            return coreWindow.GetKeyState(key).HasFlag(CoreVirtualKeyStates.Down);
        }

        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            var coreWindow = Window.Current.CoreWindow;

            var isControlDown = IsKeyDown(VirtualKey.Control);

            if (isControlDown)
            {
                switch (args.VirtualKey)
                {
                    case VirtualKey.R:
                        _core.Resize(new Vector2(1000, 1000));
                        return;
                    case VirtualKey.N:
                        _core.ClearCanvas();
                        return;
                    case VirtualKey.S:
                        _core.SaveCanvas();
                        return;
                    case VirtualKey.C:
                        CopyToClipboard();
                        return;
                    case VirtualKey.V:
                        PasteFromClipboard();
                        return;
                    case VirtualKey.Z:
                        _core.Undo();
                        return;
                    case VirtualKey.Y:
                        _core.Redo();
                        return;
                }
            }
            else
            {
                switch (args.VirtualKey)
                {
                    
                }
            }
            
        }

        private void SetColorIndex(int index)
        {
            _currentColorIndex = index;

            var brush = ColorRectangle.Fill as SolidColorBrush;
            brush.Color = _colors[_currentColorIndex];

            _core.SetColor(_colors[_currentColorIndex]);
        }

        private async void PasteFromClipboard()
        {
            var dataPackage = Clipboard.GetContent();
            if (dataPackage.Contains(StandardDataFormats.Bitmap))
            {
                var bitmap = await dataPackage.GetBitmapAsync();
                using (var stream = await bitmap.OpenReadAsync())
                {
                    await _core.PasteAsync(stream.AsStream());
                }
            }
        }

        private async void CopyToClipboard()
        {
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;

            var reference = await _core.CopytToStreamReference();

            dataPackage.SetBitmap(reference);
            Clipboard.SetContent(dataPackage);
        }

        private void PencilToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (PencilToolButton.IsChecked == true)
            {
                _core.SwitchToPencilTool();
                SelectionToolButton.IsChecked = false;
                FillToolButton.IsChecked = false;
            }
        }

        private void FillToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (FillToolButton.IsChecked == true)
            {
                _core.SwitchToFillTool();
                SelectionToolButton.IsChecked = false;
                PencilToolButton.IsChecked = false;
            }
        }

        private void SelectionToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectionToolButton.IsChecked == true)
            {
                _core.SwitchToSelectionTool();
                PencilToolButton.IsChecked = false;
                FillToolButton.IsChecked = false;
            }
        }

        private void ColorGridView_ItemClick(object sender, Windows.UI.Xaml.Controls.ItemClickEventArgs e)
        {

        }

        private void SaveAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            _core.SaveCanvas();
        }

        private void SaveAsAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            _core.SaveCanvas(null);
        }

        private void OpenFileAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            _core.OpenFile(null);
        }

        private void NewImageAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            _core.NewImage(new Vector2(300, 300));
        }

        private void ClearAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            _core.ClearCanvas();
        }

        private void UndoAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            _core.Undo();
        }

        private void RedoAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            _core.Redo();
        }

        private void CopyAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard();
        }

        private void PasteAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            PasteFromClipboard();
        }
    }
}
