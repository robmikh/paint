using Microsoft.Graphics.Canvas;
using Paint.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;

namespace Paint.Drawing
{
    class Canvas : IDisposable
    {
        public event EventHandler<Vector2> SizeChanged;

        public Vector2 Size { get { return _canvasSize; } }

        public Canvas(CanvasDevice device, Vector2 canvasSize)
        {
            _lock = new object();
            _device = device;
            _canvasSize = canvasSize;
            CreateBuffers();
        }

        public object GetDrawingLock()
        {
            return _lock;
        }

        public CanvasRenderTarget GetCanvasBuffer()
        {
            var oldHead = _canvasBufferHead;
            var newHead = (_canvasBufferHead + 1) % _canvasBuffers.Length;
            var newTail = _canvasBufferTail;

            if (newHead == _canvasBufferTail)
            {
                newTail = (_canvasBufferTail + 1) % _canvasBuffers.Length;
            }

            _numUndone = 0;

            var oldBuffer = _canvasBuffers[oldHead];
            var newBuffer = _canvasBuffers[newHead];

            lock (_lock)
            {
                if (newBuffer.Size != oldBuffer.Size)
                {
                    var dpi = GraphicsInformation.Dpi;
                    newBuffer.Dispose();
                    _canvasBuffers[newHead] = new CanvasRenderTarget(_device, _canvasSize.X, _canvasSize.Y, dpi);
                    newBuffer = _canvasBuffers[newHead];
                }

                using (var drawingSession = newBuffer.CreateDrawingSession())
                {
                    drawingSession.Clear(Colors.Transparent);
                    drawingSession.DrawImage(oldBuffer);
                }

                _canvasBufferHead = newHead;
                _canvasBufferTail = newTail;
            }

            return GetCurrentBuffer();
        }

        public CanvasBitmap Copy()
        {
            CanvasBitmap copy = null;
            lock (_lock)
            {
                // Copy what's currently on the buffer
                copy = CanvasBitmap.CreateFromDirect3D11Surface(_device, GetCurrentBuffer());
            }

            return copy;
        }

        public bool Resize(Vector2 newSize)
        {
            var dpi = GraphicsInformation.Dpi;

            if (newSize != _canvasSize)
            {
                _canvasSize = newSize;

                if (_presenter != null)
                {
                    _presenter.Stop();
                }

                var canvasBuffer = GetCanvasBuffer();

                lock (_lock)
                {
                    canvasBuffer.Dispose();
                    canvasBuffer = null;

                    _canvasBuffers[_canvasBufferHead] = new CanvasRenderTarget(_device, _canvasSize.X, _canvasSize.Y, dpi);
                    canvasBuffer = _canvasBuffers[_canvasBufferHead];

                    if (_presenter != null)
                    {
                        _presenter.Resize();
                        _presenter.Start();
                    }
                }

                SizeChanged?.Invoke(this, newSize);
                return true;
            }

            return false;
        }

        public async Task SaveCanvas(StorageFile file)
        {
            using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                await GetCurrentBuffer().SaveAsync(stream, CanvasBitmapFileFormat.Png);
            }
        }

        public void ClearCanvas()
        {
            var buffer = GetCanvasBuffer();
            lock (_lock)
            {
                using (var drawingSession = buffer.CreateDrawingSession())
                {
                    drawingSession.Clear(Colors.Transparent);
                }
            }
        }

        public void Blit(CanvasDrawingSession drawingSession)
        {
            lock (_lock)
            {
                var buffer = GetCurrentBuffer();
                drawingSession.DrawImage(buffer);
            }
        }

        public void RegisterPresenter(CanvasPresenter presenter)
        {
            _presenter = presenter;
        }

        public void UnregisterPresenter()
        {
            _presenter = null;
        }

        public void Undo()
        {
            lock (_lock)
            {
                if (_canvasBufferHead != _canvasBufferTail)
                {
                    var oldHead = _canvasBufferHead;
                    _canvasBufferHead = (_canvasBufferHead - 1 + _canvasBuffers.Length) % _canvasBuffers.Length;
                    _numUndone++;

                    var oldBuffer = _canvasBuffers[oldHead];
                    var newBuffer = _canvasBuffers[_canvasBufferHead];

                    if (newBuffer.Size != oldBuffer.Size)
                    {
                        _canvasSize = newBuffer.Size.ToVector2();
                        if (_presenter != null)
                        {
                            _presenter.Resize();
                        }
                        SizeChanged?.Invoke(this, _canvasSize);
                    }
                }
            }
        }

        public void Redo()
        {
            lock (_lock)
            {
                if (_numUndone > 0)
                {
                    var oldHead = _canvasBufferHead;
                    _canvasBufferHead = (_canvasBufferHead + 1) % _canvasBuffers.Length;
                    _numUndone--;

                    var oldBuffer = _canvasBuffers[oldHead];
                    var newBuffer = _canvasBuffers[_canvasBufferHead];

                    if (newBuffer.Size != oldBuffer.Size)
                    {
                        _canvasSize = newBuffer.Size.ToVector2();
                        if (_presenter != null)
                        {
                            _presenter.Resize();
                        }
                        SizeChanged?.Invoke(this, _canvasSize);
                    }
                }
            }
        }

        private CanvasRenderTarget GetCurrentBuffer()
        {
            System.Diagnostics.Debug.WriteLine(_canvasBufferHead);
            return _canvasBuffers[_canvasBufferHead];
        }

        private void DisposeBuffers()
        {
            foreach (var buffer in _canvasBuffers)
            {
                buffer.Dispose();
            }
            _canvasBuffers = null;
        }

        private void CreateBuffers()
        {
            var dpi = GraphicsInformation.Dpi;
            _canvasBuffers = new CanvasRenderTarget[MAX_UNDO];

            for (int i = 0; i < _canvasBuffers.Length; i++)
            {
                _canvasBuffers[i] = new CanvasRenderTarget(_device, _canvasSize.X, _canvasSize.Y, dpi);
            }

            _canvasBufferHead = 0;
            _canvasBufferTail = 0;
            _numUndone = 0;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                DisposeBuffers();
                _device = null;
                _canvasSize = Vector2.Zero;
            }
            
            _lock = null;
        }

        private object _lock;
        private CanvasDevice _device;
        private CanvasRenderTarget[] _canvasBuffers;
        private int _canvasBufferHead;
        private int _canvasBufferTail;
        private int _numUndone;
        private Vector2 _canvasSize;

        private CanvasPresenter _presenter;

        private static readonly int MAX_UNDO = 5;
    }
}
