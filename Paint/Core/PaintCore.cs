using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Paint.Drawing;
using Paint.Hardware;
using Paint.Tools;
using Robmikh.CompositionSurfaceFactory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml.Input;

namespace Paint.Core
{
    class PaintCore : IDisposable
    {
        public Visual Visual { get { return _visual; } }
        public Vector2 CurrentSize { get { return _currentImage.Size.ToVector2(); } }
        public Color Color { get { return _currentColor; } }

        public event EventHandler<Vector2> SizeChanged;

        public PaintCore(Compositor compositor)
        {
            var canvasSize = new Vector2(400, 400);
            _currentImage = new Image(null, canvasSize.ToSize());

            InitComposition(compositor);
            InitWin2D(canvasSize);
        }

        public void CanvasPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _currentTool.CanvasPointerExited(_canvas, _canvas.GetDrawingLock(), sender, e);
        }

        public void CanvasPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _currentTool.CanvasPointerReleased(_canvas, _canvas.GetDrawingLock(), sender, e);
        }

        public void CanvasPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _currentTool.CanvasPointerMoved(_canvas, _canvas.GetDrawingLock(), sender, e);
        }

        public void CanvasPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _currentTool.CanvasPointerPressed(_canvas, _canvas.GetDrawingLock(), sender, e);
        }

        public void Dispose()
        {

        }

        private void InitComposition(Compositor compositor)
        {
            _compositor = compositor;
            _visual = _compositor.CreateSpriteVisual();
        }

        private void InitWin2D(Vector2 canvasSize)
        {
            var dpi = GraphicsInformation.Dpi;
            var surfaceFactory = SurfaceFactory.GetSharedSurfaceFactoryForCompositor(_compositor);

            _device = CanvasComposition.GetCanvasDevice(surfaceFactory.GraphicsDevice);
            _canvas = new Canvas(_device, canvasSize);
            _canvas.SizeChanged += OnCanvasSizeChanged;

            _currentColor = Colors.Black;
            SwitchToPencilTool();

            _canvasPresenter = new CanvasPresenter(_canvas, _device);

            var surface = _canvasPresenter.GetSurface(_compositor);
            var brush = _compositor.CreateSurfaceBrush(surface);
            brush.BitmapInterpolationMode = CompositionBitmapInterpolationMode.NearestNeighbor;
            _visual.Brush = brush;
            _visual.Size = _canvas.Size;

            _canvasPresenter.Start();
        }

        private void OnCanvasSizeChanged(object sender, Vector2 e)
        {
            _visual.Size = e;
            _currentImage.Size = e.ToSize();
            SizeChanged?.Invoke(this, e);
        }

        public void SwitchToFillTool()
        {
            var tool = new FillTool(_device, _currentColor);
            SwitchTool(tool);
        }

        public void SwitchToPencilTool()
        {
            var tool = new PencilTool(_device, new Vector2(1, 1), _currentColor);
            SwitchTool(tool);
        }

        public void SwitchTool(ITool tool)
        {
            if (_currentTool != null)
            {
                _currentTool.Dispose();
                _currentTool = null;
            }

            _currentTool = tool;
        }

        public void SwitchToSelectionTool()
        {
            var tool = new SelectionTool(_device, _compositor);
            SwitchTool(tool);
        }

        public void NewImage(Vector2 size)
        {
            _currentImage.AssignNewFile(null);
            _currentImage.Size = size.ToSize();
            _canvas.Reset(size);
        }

        public async void OpenFile(StorageFile file)
        {
            if (file == null)
            {
                var picker = new FileOpenPicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".png");

                file = await picker.PickSingleFileAsync();

                if (file == null)
                {
                    return;
                }
            }

            await _canvas.OpenCanvasAsync(file);
            _currentImage.AssignNewFile(file);
        }

        public void SaveCanvas()
        {
            SaveCanvas(_currentImage.File);
        }

        public async void SaveCanvas(StorageFile file)
        {
            if (file == null)
            {
                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
                picker.SuggestedFileName = "untitled";

                file = await picker.PickSaveFileAsync();

                if (file == null)
                {
                    return;
                }

                _currentImage.AssignNewFile(file);
            }

            await _canvas.SaveCanvasAsync(file);
        }

        public async Task PasteAsync(Stream stream)
        {
            var bitmap = await CanvasBitmap.LoadAsync(_device, stream.AsRandomAccessStream(), GraphicsInformation.Dpi);

            if (bitmap.Size.Width > _canvas.Size.X ||
                        bitmap.Size.Height > _canvas.Size.Y)
            {
                var copy = _canvas.Copy();

                var newSize = new Vector2(
                        bitmap.Size.Width > _canvas.Size.X ? (float)bitmap.Size.Width : _canvas.Size.X,
                        bitmap.Size.Height > _canvas.Size.Y ? (float)bitmap.Size.Height : _canvas.Size.Y);

                _canvas.Resize(newSize);

                var canvasBuffer = _canvas.GetCanvasBuffer();
                lock (_canvas.GetDrawingLock())
                {
                    using (var drawingSession = canvasBuffer.CreateDrawingSession())
                    {
                        drawingSession.DrawImage(copy);
                        drawingSession.DrawImage(bitmap);
                    }
                }
            }
            else
            {
                var canvasBuffer = _canvas.GetCanvasBuffer();
                lock (_canvas.GetDrawingLock())
                {
                    using (var drawingSession = canvasBuffer.CreateDrawingSession())
                    {
                        drawingSession.DrawImage(bitmap);
                    }
                }
            }
        }

        public async Task<RandomAccessStreamReference> CopytToStreamReference()
        {
            if (_currentTool is SelectionTool)
            {
                var rect = ((SelectionTool)_currentTool).GetSelectionRect();

                var copy = _canvas.Copy(rect);
                var folder = ApplicationData.Current.TemporaryFolder;
                var file = await folder.CreateFileAsync("clipboard.png", CreationCollisionOption.ReplaceExisting);
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await copy.SaveAsync(stream, CanvasBitmapFileFormat.Png, 1.0f);
                }

                var reference = RandomAccessStreamReference.CreateFromFile(file);

                //var bits = copy.GetPixelBytes();
                //var stream = bits.AsBuffer().AsStream().AsRandomAccessStream();
                //var reference = RandomAccessStreamReference.CreateFromStream(stream);

                return reference;
            }

            return null;
        }

        public void SetColor(Color color)
        {
            _currentColor = color;
            _currentTool.SetColor(_currentColor);
        }

        public void Resize(Vector2 newSize)
        {
            _canvas.Resize(newSize);
        }

        public void ClearCanvas()
        {
            _canvas.ClearCanvas();
        }

        public void Undo()
        {
            _canvas.Undo();
        }

        public void Redo()
        {
            _canvas.Redo();
        }

        private Compositor _compositor;
        private SpriteVisual _visual;

        private CanvasDevice _device;
        private Canvas _canvas;
        private CanvasPresenter _canvasPresenter;

        private ITool _currentTool;

        private Color _currentColor;

        private Image _currentImage;
    }
}
