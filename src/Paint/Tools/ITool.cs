using Microsoft.Graphics.Canvas;
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
    interface ITool
    {
        void SetColor(Color color);

        void CanvasPointerExited(CanvasRenderTarget canvas, object drawLock, object sender, PointerRoutedEventArgs e);
        void CanvasPointerMoved(CanvasRenderTarget canvas, object drawLock, object sender, PointerRoutedEventArgs e);
        void CanvasPointerPressed(CanvasRenderTarget canvas, object drawLock, object sender, PointerRoutedEventArgs e);
        void CanvasPointerReleased(CanvasRenderTarget canvas, object drawLock, object sender, PointerRoutedEventArgs e);

        void PaintCanvas(CanvasRenderTarget canvas, object drawLock, Vector2 position);
    }
}
