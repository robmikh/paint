using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Composition;
using Paint.Drawing;
using Paint.Hardware;
using Paint.Tools;
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
using Windows.Storage.Pickers;
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
        private Compositor _compositor;
        private SpriteVisual _visual;

        private CanvasDevice _device;
        private Canvas _canvas;
        private CanvasPresenter _canvasPresenter;
        
        private ITool _currentTool;

        private Color _currentColor;
        private List<Color> _colors;
        private int _currentColorIndex;

        public MainPage()
        {
            this.InitializeComponent();

            var canvasSize = new Vector2(400, 400);
            CanvasRectangle.Width = canvasSize.X;
            CanvasRectangle.Height = canvasSize.Y;
            InitComposition();
            InitWin2D(canvasSize);
            SetupInputHandler();
        }

        private void InitComposition()
        {
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            _visual = _compositor.CreateSpriteVisual();

            ElementCompositionPreview.SetElementChildVisual(this, _visual);
        }

        private void InitWin2D(Vector2 canvasSize)
        {
            var dpi = GraphicsInformation.Dpi;

            _device = new CanvasDevice();
            _canvas = new Canvas(_device, canvasSize);
            

            _colors = new List<Color>();
            _colors.Add(Colors.Black);
            _colors.Add(Colors.Red);
            _colors.Add(Colors.Blue);
            _colors.Add(Colors.Green);
            _colors.Add(Colors.Yellow);

            _currentColorIndex = 0;
            _currentColor = _colors[_currentColorIndex];
            SwitchToPencilTool();

            _canvasPresenter = new CanvasPresenter(_canvas, _device);

            var surface = _canvasPresenter.GetSurface(_compositor);
            var brush = _compositor.CreateSurfaceBrush(surface);
            brush.BitmapInterpolationMode = CompositionBitmapInterpolationMode.NearestNeighbor;
            _visual.Brush = brush;
            _visual.Size = _canvas.Size;

            _canvasPresenter.Start();
        }

        private void SetupInputHandler()
        {
            var coreWindow = Window.Current.CoreWindow;

            CanvasRectangle.PointerPressed += CanvasRectangle_PointerPressed;
            CanvasRectangle.PointerMoved += CanvasRectangle_PointerMoved;
            CanvasRectangle.PointerReleased += CanvasRectangle_PointerReleased;
            CanvasRectangle.PointerExited += CanvasRectangle_PointerExited;

            coreWindow.KeyUp += CoreWindow_KeyUp;
            coreWindow.PointerWheelChanged += CoreWindow_PointerWheelChanged;
        }

        private void CanvasRectangle_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _currentTool.CanvasPointerExited(_canvas.GetCanvasBuffer(), _canvas.GetDrawingLock(), sender, e);
        }

        private void CanvasRectangle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _currentTool.CanvasPointerReleased(_canvas.GetCanvasBuffer(), _canvas.GetDrawingLock(), sender, e);
        }

        private void CanvasRectangle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _currentTool.CanvasPointerMoved(_canvas.GetCanvasBuffer(), _canvas.GetDrawingLock(), sender, e);
        }

        private void CanvasRectangle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _currentTool.CanvasPointerPressed(_canvas.GetCanvasBuffer(), _canvas.GetDrawingLock(), sender, e);
        }

        private void CoreWindow_PointerWheelChanged(CoreWindow sender, PointerEventArgs args)
        {
            var delta = args.CurrentPoint.Properties.MouseWheelDelta;
            var step = 120;
            delta /= step;

            if (delta != 0)
            {
                var index = (_currentColorIndex + delta + _colors.Count) % _colors.Count;
                SetColorIndex(index);
                _currentColor = _colors[_currentColorIndex];
                _currentTool.SetColor(_currentColor);
            }
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
                        ResizeCanvas(new Vector2(1000, 1000));
                        return;
                    case VirtualKey.N:
                        _canvas.ClearCanvas();
                        return;
                    case VirtualKey.S:
                        SaveCanvas();
                        return;
                    case VirtualKey.V:
                        PasteFromClipboard();
                        return;
                }
            }
            else
            {
                switch (args.VirtualKey)
                {
                    case VirtualKey.F:
                        SwitchToFillTool();
                        return;
                    case VirtualKey.P:
                        SwitchToPencilTool();
                        return;
                }
            }
            
        }

        private async void SaveCanvas()
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
            picker.SuggestedFileName = "untitled";

            var file = await picker.PickSaveFileAsync();

            if (file != null)
            {
                await _canvas.SaveCanvas(file);
            }
        }

        private void SetColorIndex(int index)
        {
            _currentColorIndex = index;

            var brush = ColorRectangle.Fill as SolidColorBrush;
            brush.Color = _colors[_currentColorIndex];
        }

        private async void PasteFromClipboard()
        {
            var dataPackage = Clipboard.GetContent();
            if (dataPackage.Contains(StandardDataFormats.Bitmap))
            {
                var bitmap = await dataPackage.GetBitmapAsync();
                using (var stream = await bitmap.OpenReadAsync())
                {
                    var buffer = await CanvasBitmap.LoadAsync(_device, stream);

                    if (buffer.Size.Width > _canvas.Size.X ||
                        buffer.Size.Height > _canvas.Size.Y)
                    {
                        var copy = _canvas.Copy();

                        var newSize = new Vector2(
                                buffer.Size.Width > _canvas.Size.X ? (float)buffer.Size.Width : _canvas.Size.X,
                                buffer.Size.Height > _canvas.Size.Y ? (float)buffer.Size.Height : _canvas.Size.Y);

                        ResizeCanvas(newSize);

                        lock (_canvas.GetDrawingLock())
                        {
                            using (var drawingSession = _canvas.GetCanvasBuffer().CreateDrawingSession())
                            {
                                drawingSession.DrawImage(copy);
                                drawingSession.DrawImage(buffer);
                            }
                        }
                    }
                    else
                    {
                        lock (_canvas.GetDrawingLock())
                        {
                            using (var drawingSession = _canvas.GetCanvasBuffer().CreateDrawingSession())
                            {
                                drawingSession.DrawImage(buffer);
                            }
                        }
                    }
                }
            }
        }

        private void ResizeCanvas(Vector2 newSize)
        {
            if (_canvas.Resize(newSize))
            {
                var surface = _canvasPresenter.GetSurface(_compositor);
                var brush = _compositor.CreateSurfaceBrush(surface);
                brush.BitmapInterpolationMode = CompositionBitmapInterpolationMode.NearestNeighbor;
                _visual.Brush = brush;
                _visual.Size = _canvas.Size;
                CanvasRectangle.Width = _canvas.Size.X;
                CanvasRectangle.Height = _canvas.Size.Y;
            }
        }

        private void SwitchToFillTool()
        {
            var tool = new FillTool(_device, _currentColor);
            SwitchTool(tool);
        }

        private void SwitchToPencilTool()
        {
            var tool = new PencilTool(_device, new Vector2(1, 1), _currentColor);
            SwitchTool(tool);
        }

        private void SwitchTool(ITool tool)
        {
            if (_currentTool != null)
            {
                _currentTool.Dispose();
                _currentTool = null;
            }

            _currentTool = tool;
        }
    }
}
