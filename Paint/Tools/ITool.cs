using Microsoft.Graphics.Canvas;
using Paint.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Input;

namespace Paint.Tools
{
    interface ITool : IDisposable
    {
        void SetColor(Color color);

        void CanvasPointerExited(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e);
        void CanvasPointerMoved(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e);
        void CanvasPointerPressed(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e);
        void CanvasPointerReleased(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e);

        void PaintCanvas(CanvasRenderTarget canvas, object drawLock, Vector2 position);
    }
}
