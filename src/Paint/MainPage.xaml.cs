using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Composition;
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
using Windows.UI.Xaml.Controls;
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
    public sealed partial class MainPage : Page
    {
        private Compositor _compositor;
        private SpriteVisual _visual;

        private CanvasDevice _device;
        private CanvasRenderTarget _canvasBrush;
        private CanvasRenderTarget _canvasBuffer;

        private object _lock;
        private CanvasSwapChain _swapChain;
        private CanvasImageBrush _backgroundBrush;
        private CancellationTokenSource _drawLoopCancellationTokenSource;
        private AutoResetEvent _swapChainDestructionEvent;

        private Vector2 _canvasSize;
        private Vector2 _brushSize;
        private float _dpi;
        private Color _currentColor;
        private List<Color> _colors;
        private int _currentColorIndex;

        private Vector2? _previousPosition;

        public MainPage()
        {
            this.InitializeComponent();

            _canvasSize = new Vector2(400, 400);
            CanvasRectangle.Width = _canvasSize.X;
            CanvasRectangle.Height = _canvasSize.Y;
            InitComposition();
            InitWin2D();
            SetupInputHandler();
        }

        private void InitComposition()
        {
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            _visual = _compositor.CreateSpriteVisual();
            _visual.Size = _canvasSize;

            ElementCompositionPreview.SetElementChildVisual(this, _visual);
        }

        private void InitWin2D()
        {
            _dpi = DisplayInformation.GetForCurrentView().LogicalDpi;

            _lock = new object();
            _device = new CanvasDevice();
            _canvasBuffer = new CanvasRenderTarget(_device, _canvasSize.X, _canvasSize.Y, _dpi);

            _colors = new List<Color>();
            _colors.Add(Colors.Black);
            _colors.Add(Colors.Red);
            _colors.Add(Colors.Blue);
            _colors.Add(Colors.Green);
            _colors.Add(Colors.Yellow);

            DrawBrush(new Vector2(1, 1), Colors.Black);
            DrawBackgroundBrush();

            _swapChain = new CanvasSwapChain(_device, _canvasSize.X, _canvasSize.Y, _dpi);

            var surface = CanvasComposition.CreateCompositionSurfaceForSwapChain(_compositor, _swapChain);
            var brush = _compositor.CreateSurfaceBrush(surface);
            _visual.Brush = brush;

            StartDrawLoop();
        }

        private void DrawBackgroundBrush()
        {
            if (_backgroundBrush != null)
            {
                _backgroundBrush.Dispose();
                _backgroundBrush = null;
            }

            using (var renderTarget = new CanvasRenderTarget(_device, 16, 16, _dpi))
            {
                using (var drawingSession = renderTarget.CreateDrawingSession())
                {
                    drawingSession.Clear(Colors.Gray);
                    drawingSession.FillRectangle(8, 0, 8, 8, Colors.DarkGray);
                    drawingSession.FillRectangle(0, 8, 8, 8, Colors.DarkGray);
                }

                _backgroundBrush = new CanvasImageBrush(_device, renderTarget);
                _backgroundBrush.ExtendX = CanvasEdgeBehavior.Wrap;
                _backgroundBrush.ExtendY = CanvasEdgeBehavior.Wrap;
            }
        }

        private void DrawBrush(Vector2 brushSize, Color color)
        {
            _currentColor = color;
            if (_brushSize != brushSize)
            {
                _brushSize = brushSize;

                if (_canvasBrush != null)
                {
                    _canvasBrush.Dispose();
                    _canvasBrush = null;
                }

                _canvasBrush = new CanvasRenderTarget(_device, _brushSize.X, _brushSize.Y, _dpi);
            }
            
            using (var drawingSession = _canvasBrush.CreateDrawingSession())
            {
                drawingSession.Clear(color);
            }
        }

        private void DrawLoop()
        {
            var canceled = _drawLoopCancellationTokenSource.Token;

            try
            {
                while (!canceled.IsCancellationRequested)
                {
                    DrawFrame();
                    _swapChain.Present();
                    _swapChain.WaitForVerticalBlank();
                }

                _swapChain.Dispose();
                _swapChain = null;
                _swapChainDestructionEvent.Set();
            }
            catch (Exception e) when (_swapChain.Device.IsDeviceLost(e.HResult))
            {
                _swapChain.Device.RaiseDeviceLost();
            }
        }

        private void DrawFrame()
        {
            lock (_lock)
            {
                using (var drawingSession = _swapChain.CreateDrawingSession(Colors.Transparent))
                {
                    drawingSession.FillRectangle(0, 0, _canvasSize.X, _canvasSize.Y, _backgroundBrush);
                    drawingSession.DrawImage(_canvasBuffer);
                }
            }
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
            _previousPosition = null;
        }

        private void CanvasRectangle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _previousPosition = null;
        }

        private void CanvasRectangle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(sender as UIElement);

            if (currentPoint.IsInContact)
            {
                PaintCanvas(_canvasBrush, currentPoint.Position.ToVector2());
            }
        }

        private void CanvasRectangle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(sender as UIElement);

            _previousPosition = null;

            if (IsKeyDown(VirtualKey.F))
            {
                FillCanvas(currentPoint.Position.ToVector2());
            }
            else
            {
                PaintCanvas(_canvasBrush, currentPoint.Position.ToVector2());
            }
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
                DrawBrush(_brushSize, _colors[_currentColorIndex]);
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
                        ClearCanvas();
                        return;
                    case VirtualKey.S:
                        SaveCanvas();
                        return;
                    case VirtualKey.V:
                        PasteFromClipboard();
                        return;
                }
            }
            
        }

        private void PaintCanvas(CanvasBitmap canvasBrush, Vector2 position)
        {
            var size = canvasBrush.Size.ToVector2();
            var offset = (size / -2.0f) + position;

            lock (_lock)
            {
                using (var drawingSession = _canvasBuffer.CreateDrawingSession())
                {
                    if (_previousPosition != null)
                    {
                        using (var brush = new CanvasImageBrush(_device, _canvasBrush))
                        {
                            drawingSession.DrawLine(offset, _previousPosition.Value, brush);
                        }
                    }
                    else
                    {
                        drawingSession.DrawImage(canvasBrush, offset);
                    }

                    _previousPosition = offset;
                }
            }
        }

        private void ClearCanvas()
        {
            lock (_lock)
            {
                using (var drawingSession = _canvasBuffer.CreateDrawingSession())
                {
                    drawingSession.Clear(Colors.Transparent);
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
                using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    await _canvasBuffer.SaveAsync(stream, CanvasBitmapFileFormat.Png);
                }
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

                    if (buffer.Size.Width > _canvasSize.X ||
                        buffer.Size.Height > _canvasSize.Y)
                    {
                        CanvasBitmap copy = null;
                        lock (_lock)
                        {
                            // Copy what's currently on the buffer
                            copy = CanvasBitmap.CreateFromDirect3D11Surface(_device, _canvasBuffer);
                        }

                        var newSize = new Vector2(
                                buffer.Size.Width > _canvasSize.X ? (float)buffer.Size.Width : _canvasSize.X,
                                buffer.Size.Height > _canvasSize.Y ? (float)buffer.Size.Height : _canvasSize.Y);

                        ResizeCanvas(newSize);

                        lock (_lock)
                        {
                            using (var drawingSession = _canvasBuffer.CreateDrawingSession())
                            {
                                drawingSession.DrawImage(copy);
                                drawingSession.DrawImage(buffer);
                            }
                        }
                    }
                    else
                    {
                        lock (_lock)
                        {
                            using (var drawingSession = _canvasBuffer.CreateDrawingSession())
                            {
                                drawingSession.DrawImage(buffer);
                            }
                        }
                    }
                }
            }
        }

        private void FillCanvas(Vector2 position)
        {
            lock (_lock)
            {
                var colors = _canvasBuffer.GetPixelColors();

                var targetColor = colors[(int)(position.X + position.Y * _canvasSize.X)];

                if (targetColor != _currentColor)
                {
                    FloodPixel((int)position.X, (int)position.Y, colors, targetColor);

                    _canvasBuffer.SetPixelColors(colors);
                }
            }
        }

        private void FloodPixel(int x, int y, Color[] colors, Color targetColor)
        {
            if (!(x >= 0 &&
                x < (int)_canvasSize.X &&
                y >= 0 &&
                y < (int)_canvasSize.Y))
            {
                return;
            }

            var index = x + y * (int)_canvasSize.X;

            if (colors[index] != targetColor)
            {
                return;
            }

            var queue = new Queue<int>();
            queue.Enqueue(index);

            while (queue.Count > 0)
            {
                var currentIndex = queue.Dequeue();
                int currentX = currentIndex % (int)_canvasSize.X;
                int currentY = currentIndex / (int)_canvasSize.X;

                int w = currentX;
                while (w >= 0 && colors[w + currentY * (int)_canvasSize.X] ==  targetColor)
                {
                    w--;
                }

                int e = currentX;
                while (e < (int)_canvasSize.X && colors[e + currentY * (int)_canvasSize.X] == targetColor)
                {
                    e++;
                }

                for (int tempX = w + 1; tempX < e; tempX++)
                {
                    var tempIndex = tempX + currentY * (int)_canvasSize.X;
                    colors[tempIndex] = _currentColor;

                    if (currentY - 1 >= 0)
                    {
                        var i = tempX + (currentY - 1) * (int)_canvasSize.X;

                        if (colors[i] == targetColor)
                        {
                            queue.Enqueue(i);
                        }
                    }

                    if (currentY + 1 < (int)_canvasSize.Y)
                    {
                        var i = tempX + (currentY + 1) * (int)_canvasSize.X;

                        if (colors[i] == targetColor)
                        {
                            queue.Enqueue(i);
                        }
                    }
                }
            }
        }

        private bool CheckPixel(int index, Color[] colors, Color targetColor)
        {
            if (colors[index] == targetColor)
            {
                colors[index] = _currentColor;
                return true;
            }

            return false;
        }

        private void ResizeCanvas(Vector2 newSize)
        {
            if (newSize != _canvasSize)
            {
                _canvasSize = newSize;
                _drawLoopCancellationTokenSource.Cancel();
                _swapChainDestructionEvent.WaitOne();

                lock (_lock)
                {
                    _canvasBuffer.Dispose();
                    _canvasBuffer = null;

                    _canvasBuffer = new CanvasRenderTarget(_device, _canvasSize.X, _canvasSize.Y, _dpi);
                    _swapChain = new CanvasSwapChain(_device, _canvasSize.X, _canvasSize.Y, _dpi);

                    var surface = CanvasComposition.CreateCompositionSurfaceForSwapChain(_compositor, _swapChain);
                    var brush = _compositor.CreateSurfaceBrush(surface);
                    _visual.Brush = brush;
                    _visual.Size = _canvasSize;
                    CanvasRectangle.Width = _canvasSize.X;
                    CanvasRectangle.Height = _canvasSize.Y;

                    StartDrawLoop();
                }
            }
        }

        private void StartDrawLoop()
        {
            if (_drawLoopCancellationTokenSource != null)
            {
                _drawLoopCancellationTokenSource.Dispose();
                _drawLoopCancellationTokenSource = null;
            }

            if (_swapChainDestructionEvent != null)
            {
                _swapChainDestructionEvent.Dispose();
                _swapChainDestructionEvent = null;
            }

            _drawLoopCancellationTokenSource = new CancellationTokenSource();
            _swapChainDestructionEvent = new AutoResetEvent(false);
            Task.Factory.StartNew(
                DrawLoop,
                _drawLoopCancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
    }
}
