using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.UI.Xaml.Input;
using Windows.UI;
using Paint.Hardware;
using Windows.UI.Xaml;
using Microsoft.Graphics.Canvas.Brushes;

namespace Paint.Tools
{
    class PencilTool : ITool
    {
        public PencilTool(CanvasDevice device, Vector2 brushSize, Color color)
        {
            _device = device;
            DrawBrush(brushSize, color);
        }

        private void DrawBrush(Vector2 brushSize, Color color)
        {
            var dpi = GraphicsInformation.Dpi;

            if (_brushSize != brushSize)
            {
                _brushSize = brushSize;

                if (_canvasBrush != null)
                {
                    _canvasBrush.Dispose();
                    _canvasBrush = null;
                }

                _canvasBrush = new CanvasRenderTarget(_device, _brushSize.X, _brushSize.Y, dpi);
            }

            using (var drawingSession = _canvasBrush.CreateDrawingSession())
            {
                drawingSession.Clear(color);
            }
        }

        public void SetColor(Color color)
        {
            DrawBrush(_brushSize, color);
        }

        public void CanvasPointerExited(CanvasRenderTarget canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            _previousPosition = null;
        }

        public void CanvasPointerMoved(CanvasRenderTarget canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(sender as UIElement);

            if (currentPoint.IsInContact)
            {
                PaintCanvas(canvas, drawLock, currentPoint.Position.ToVector2());
            }
        }

        public void CanvasPointerPressed(CanvasRenderTarget canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(sender as UIElement);

            _previousPosition = null;

            PaintCanvas(canvas, drawLock, currentPoint.Position.ToVector2());
        }

        public void PaintCanvas(CanvasRenderTarget canvas, object drawLock, Vector2 position)
        {
            var size = _brushSize;
            var offset = (size / -2.0f) + position;

            lock (drawLock)
            {
                using (var drawingSession = canvas.CreateDrawingSession())
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
                        drawingSession.DrawImage(_canvasBrush, offset);
                    }

                    _previousPosition = offset;
                }
            }
        }

        public void CanvasPointerReleased(CanvasRenderTarget canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            _previousPosition = null;
        }

        public void Dispose()
        {
            _device = null;
            _canvasBrush.Dispose();
            _canvasBrush = null;
            _previousPosition = null;
        }

        private CanvasDevice _device;
        private CanvasRenderTarget _canvasBrush;
        private Vector2 _brushSize;
        private Vector2? _previousPosition;
    }
}
