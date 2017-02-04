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
        public Vector2 Size { get { return _canvasSize; } }

        public Canvas(CanvasDevice device, Vector2 canvasSize)
        {
            var dpi = GraphicsInformation.Dpi;

            _lock = new object();
            _device = device;
            _canvasSize = canvasSize;
            _canvasBuffer = new CanvasRenderTarget(_device, _canvasSize.X, _canvasSize.Y, dpi);
        }

        public object GetDrawingLock()
        {
            return _lock;
        }

        public CanvasRenderTarget GetCanvasBuffer()
        {
            return _canvasBuffer;
        }

        public CanvasBitmap Copy()
        {
            CanvasBitmap copy = null;
            lock (_lock)
            {
                // Copy what's currently on the buffer
                copy = CanvasBitmap.CreateFromDirect3D11Surface(_device, _canvasBuffer);
            }

            return copy;
        }

        public void Resize(Vector2 newSize)
        {
            var dpi = GraphicsInformation.Dpi;

            if (newSize != _canvasSize)
            {
                _canvasSize = newSize;

                // FUTURE: hold the lock
                _canvasBuffer.Dispose();
                _canvasBuffer = null;

                _canvasBuffer = new CanvasRenderTarget(_device, _canvasSize.X, _canvasSize.Y, dpi);
            }
        }

        public async Task SaveCanvas(StorageFile file)
        {
            using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                await _canvasBuffer.SaveAsync(stream, CanvasBitmapFileFormat.Png);
            }
        }

        public void ClearCanvas()
        {
            lock (_lock)
            {
                using (var drawingSession = GetCanvasBuffer().CreateDrawingSession())
                {
                    drawingSession.Clear(Colors.Transparent);
                }
            }
        }

        public void Blit(CanvasDrawingSession drawingSession)
        {
            lock (_lock)
            {
                drawingSession.DrawImage(_canvasBuffer);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _canvasBuffer.Dispose();
                _canvasBuffer = null;
                _device = null;
                _canvasSize = Vector2.Zero;
            }
            
            _lock = null;
        }

        private object _lock;
        private CanvasDevice _device;
        private CanvasRenderTarget _canvasBuffer;
        private Vector2 _canvasSize;
    }
}
