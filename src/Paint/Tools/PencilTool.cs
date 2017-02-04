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
using Paint.Drawing;

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

        public void CanvasPointerExited(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            _previousPosition = null;
        }

        public void CanvasPointerReleased(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            _previousPosition = null;
            _cachedBuffer = null;
        }

        public void CanvasPointerMoved(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(sender as UIElement);

            var points = e.GetIntermediatePoints(sender as UIElement);
            foreach (var point in points)
            {
                if (point.IsInContact)
                {
                    PaintCanvas(_cachedBuffer, drawLock, point.Position.ToVector2());
                }
            }

            if (currentPoint.IsInContact)
            {
                PaintCanvas(_cachedBuffer, drawLock, currentPoint.Position.ToVector2());
            }
        }

        public void CanvasPointerPressed(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(sender as UIElement);
            _cachedBuffer = canvas.GetCanvasBuffer();

            _previousPosition = null;

            PaintCanvas(_cachedBuffer, drawLock, currentPoint.Position.ToVector2());
        }

        public void PaintCanvas(CanvasRenderTarget canvas, object drawLock, Vector2 position)
        {
            var size = _brushSize;
            var offset = (size / -2.0f) + position;
            offset = new Vector2((int)offset.X, (int)offset.Y);

            lock (drawLock)
            {
                using (var drawingSession = canvas.CreateDrawingSession())
                {
                    drawingSession.Antialiasing = CanvasAntialiasing.Aliased;
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
        private CanvasRenderTarget _cachedBuffer;
    }
}
