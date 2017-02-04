using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Paint.Drawing;
using Windows.UI;
using Windows.UI.Xaml.Input;
using Windows.UI.Composition;
using Microsoft.Graphics.Canvas.UI.Composition;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace Paint.Tools
{
    class SelectionTool : ITool
    {
        public SelectionTool(CanvasDevice device, Compositor compositor)
        {
            _device = device;
            _compositor = compositor;
            _graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(_compositor, _device);

            _rootVisual = _compositor.CreateContainerVisual();
            _gripperVisual = _compositor.CreateSpriteVisual();

            _surface = _graphicsDevice.CreateDrawingSurface(new Size(60, 60), DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
            _surfaceBrush = _compositor.CreateSurfaceBrush(_surface);
            _surfaceBrush.Stretch = CompositionStretch.Fill;
            _surfaceBrush.BitmapInterpolationMode = CompositionBitmapInterpolationMode.NearestNeighbor;
            _nineGridBrush = _compositor.CreateNineGridBrush();
            _nineGridBrush.Source = _surfaceBrush;
            _nineGridBrush.IsCenterHollow = true;
            _nineGridBrush.LeftInset = 20;
            _nineGridBrush.RightInset = 20;
            _nineGridBrush.TopInset = 20;
            _nineGridBrush.BottomInset = 20;

            _gripperVisual.Brush = _nineGridBrush;

            var expression = _compositor.CreateExpressionAnimation();
            expression.Expression = "Vector2(visual.Size.X + 20, visual.Size.Y + 20)";
            expression.SetReferenceParameter("visual", _rootVisual);
            _gripperVisual.StartAnimation(nameof(Visual.Size), expression);
            _gripperVisual.Offset = new Vector3(-10, -10, 0);

            _rootVisual.Children.InsertAtTop(_gripperVisual);

            DrawBrush();
        }

        private void DrawBrush()
        {
            using (var drawingSession = CanvasComposition.CreateDrawingSession(_surface))
            {
                drawingSession.Clear(Colors.Transparent);
                drawingSession.DrawRectangle(10, 10, 40, 40, Colors.Black, 2.0f);
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        drawingSession.FillRectangle((x * 20) +  5, (y * 20) + 5, 10, 10, Colors.White);
                    }
                }
            }
        }

        private void EnsureParent(object uiElement)
        {
            var control = uiElement as UIElement;

            if (control != null && _rootVisual.Parent == null)
            {
                var visual = (SpriteVisual)ElementCompositionPreview.GetElementChildVisual(control);
                visual.Children.InsertAtTop(_rootVisual);
            }
        }

        public void CanvasPointerExited(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            
        }

        public void CanvasPointerMoved(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            EnsureParent(sender);
            var currentPoint = e.GetCurrentPoint(sender as UIElement);

            if (currentPoint.IsInContact)
            {
                var pointPosition = currentPoint.Position.ToVector2();
                var newSize = pointPosition - new Vector2(_rootVisual.Offset.X, _rootVisual.Offset.Y);

                _rootVisual.Size = newSize;
            }
        }

        public void CanvasPointerPressed(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            EnsureParent(sender);
            var currentPoint = e.GetCurrentPoint(sender as UIElement);
            var pointPosition = currentPoint.Position.ToVector2();
            _rootVisual.Offset = new Vector3(pointPosition, 0);
            _rootVisual.Size = new Vector2(20);
        }

        public void CanvasPointerReleased(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            
        }

        public void Dispose()
        {
            _rootVisual.Dispose();
            _surface.Dispose();
            _surfaceBrush.Dispose();
            _nineGridBrush.Dispose();
            _device = null;
            _graphicsDevice.Dispose();
            _graphicsDevice = null;
        }

        public void PaintCanvas(CanvasRenderTarget canvas, object drawLock, Vector2 position)
        {
            
        }

        public void SetColor(Color color)
        {
            
        }

        public Rect GetSelectionRect()
        {
            return new Rect(
                _rootVisual.Offset.X,
                _rootVisual.Offset.Y,
                _rootVisual.Size.X,
                _rootVisual.Size.Y);
        }

        private Compositor _compositor;
        private ContainerVisual _rootVisual;
        private SpriteVisual _gripperVisual;
        private CompositionDrawingSurface _surface;
        private CompositionSurfaceBrush _surfaceBrush;
        private CompositionNineGridBrush _nineGridBrush;

        private CanvasDevice _device;
        private CompositionGraphicsDevice _graphicsDevice;
    }
}
