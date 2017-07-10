using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.UI;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml;
using Paint.Drawing;
using Paint.Hardware;
using Windows.Foundation;
using Paint.Extensions;

namespace Paint.Tools
{
    class FillTool : ITool
    {
        public FillTool(CanvasDevice device, Color color)
        {
            _device = device;
            _color = color;
        }

        public void CanvasPointerExited(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            
        }

        public void CanvasPointerMoved(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            
        }

        public void CanvasPointerPressed(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(sender as UIElement);
            FillCanvas(canvas.GetCanvasBuffer(), drawLock, currentPoint.Position.ToVector2());
        }

        public void CanvasPointerReleased(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            
        }

        public void PaintCanvas(CanvasRenderTarget canvas, object drawLock, Vector2 position)
        {
            FillCanvas(canvas, drawLock, position);
        }

        public void SetColor(Color color)
        {
            _color = color;
        }

        private void FillCanvas(CanvasRenderTarget canvas, object drawLock, Vector2 position)
        {
            // This seems pretty hacky... maybe dpi should be handled in the layer above the tools
            position *= (GraphicsInformation.Dpi / 96.0f);

            lock (drawLock)
            {
                var canvasSize = canvas.SizeInPixels.ToVector2();
                var colors = canvas.GetPixelColors();
                var rect = new Rect(0, 0, canvas.SizeInPixels.Width, canvas.SizeInPixels.Height);

                if ((position.X >= rect.Left && position.X <= rect.Right) ||
                    (position.Y >= rect.Top && position.Y <= rect.Bottom))
                {
                    var targetColor = colors[(int)(position.X + position.Y * canvasSize.X)];

                    if (targetColor != _color)
                    {
                        FloodPixel((int)position.X, (int)position.Y, colors, targetColor, canvasSize, rect);

                        using (var bitmap = CanvasBitmap.CreateFromColors(_device, colors, (int)canvas.SizeInPixels.Width, (int)canvas.SizeInPixels.Height, GraphicsInformation.Dpi))
                        using (var drawingSession = canvas.CreateDrawingSession())
                        using (var layer = drawingSession.CreateLayer(1.0f, rect))
                        {
                            drawingSession.DrawImage(bitmap);
                        }
                    }
                }
            }
        }

        private void FloodPixel(int x, int y, Color[] colors, Color targetColor, Vector2 canvasSize, Rect clipRect)
        {
            if (!(x >= 0 &&
                x < (int)canvasSize.X &&
                y >= 0 &&
                y < (int)canvasSize.Y))
            {
                return;
            }

            if (!(x >= clipRect.Left &&
                x <= (int)clipRect.Right &&
                y >= clipRect.Top &&
                y <= (int)clipRect.Bottom))
            {
                return;
            }


            var index = x + y * (int)canvasSize.X;

            if (colors[index] != targetColor)
            {
                return;
            }

            var queue = new Queue<int>();
            queue.Enqueue(index);

            while (queue.Count > 0)
            {
                var currentIndex = queue.Dequeue();
                int currentX = currentIndex % (int)canvasSize.X;
                int currentY = currentIndex / (int)canvasSize.X;

                int w = currentX;
                while (w >= 0 && colors[w + currentY * (int)canvasSize.X] == targetColor)
                {
                    w--;
                }

                int e = currentX;
                while (e < (int)canvasSize.X && colors[e + currentY * (int)canvasSize.X] == targetColor)
                {
                    e++;
                }

                for (int tempX = w + 1; tempX < e; tempX++)
                {
                    var tempIndex = tempX + currentY * (int)canvasSize.X;
                    colors[tempIndex] = _color;

                    if (currentY - 1 >= 0)
                    {
                        var i = tempX + (currentY - 1) * (int)canvasSize.X;

                        if (colors[i] == targetColor)
                        {
                            queue.Enqueue(i);
                        }
                    }

                    if (currentY + 1 < (int)canvasSize.Y)
                    {
                        var i = tempX + (currentY + 1) * (int)canvasSize.X;

                        if (colors[i] == targetColor)
                        {
                            queue.Enqueue(i);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _device = null;
        }

        private CanvasDevice _device;
        private Color _color;
    }
}
