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
using Microsoft.Graphics.Canvas.Effects;

namespace Paint.Tools
{
    class SelectionTool : ITool
    {
        public SelectionTool(CanvasDevice device, Compositor compositor)
        {
            _device = device;
            _compositor = compositor;
            _graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(_compositor, _device);

            _rootVisual = _compositor.CreateSpriteVisual();
            //((SpriteVisual)_rootVisual).Brush = _compositor.CreateColorBrush(Colors.Red);
            //_rootVisual.Opacity = 0.7f;
            _gripperVisual = _compositor.CreateSpriteVisual();

            float size = 3;

            var topSurface = _graphicsDevice.CreateDrawingSurface(new Size(size * 2, size), DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
            var bottomSurface = _graphicsDevice.CreateDrawingSurface(new Size(size * 2, size), DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
            var leftSurface = _graphicsDevice.CreateDrawingSurface(new Size(size, size * 2), DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
            var rightSurface = _graphicsDevice.CreateDrawingSurface(new Size(size, size * 2), DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);

            var color1 = Colors.Black;
            var color2 = Colors.White;
            using (var drawingSession = CanvasComposition.CreateDrawingSession(topSurface))
            {
                drawingSession.Clear(Colors.Transparent);
                drawingSession.FillRectangle(0, 0, size, size, color1);
                drawingSession.FillRectangle(size, 0, size, size, color2);
            }
            using (var drawingSession = CanvasComposition.CreateDrawingSession(leftSurface))
            {
                drawingSession.Clear(Colors.Transparent);
                drawingSession.FillRectangle(0, 0, size, size, color1);
                drawingSession.FillRectangle(0, size, size, size, color2);
            }

            color1 = Colors.White;
            color2 = Colors.Black;
            using (var drawingSession = CanvasComposition.CreateDrawingSession(bottomSurface))
            {
                drawingSession.Clear(Colors.Transparent);
                drawingSession.FillRectangle(0, 0, size, size, color1);
                drawingSession.FillRectangle(size, 0, size, size, color2);
            }
            using (var drawingSession = CanvasComposition.CreateDrawingSession(rightSurface))
            {
                drawingSession.Clear(Colors.Transparent);
                drawingSession.FillRectangle(0, 0, size, size, color1);
                drawingSession.FillRectangle(0, size, size, size, color2);
            }

            var topVisual = _compositor.CreateSpriteVisual();
            var bottomVisual = _compositor.CreateSpriteVisual();
            var leftVisual = _compositor.CreateSpriteVisual();
            var rightVisual = _compositor.CreateSpriteVisual();

            topVisual.RelativeSizeAdjustment = new Vector2(1, 0);
            topVisual.Size = new Vector2(0, size);
            topVisual.Brush = CreateEffectBrushFromSurface(topSurface);

            bottomVisual.RelativeSizeAdjustment = new Vector2(1, 0);
            bottomVisual.Size = new Vector2(0, size);
            bottomVisual.RelativeOffsetAdjustment = new Vector3(0, 1, 0);
            bottomVisual.Offset = new Vector3(0, -size, 0);
            bottomVisual.Brush = CreateEffectBrushFromSurface(bottomSurface);

            leftVisual.RelativeSizeAdjustment = new Vector2(0, 1);
            leftVisual.Size = new Vector2(size, 0);
            leftVisual.Brush = CreateEffectBrushFromSurface(leftSurface);

            rightVisual.RelativeSizeAdjustment = new Vector2(0, 1);
            rightVisual.Size = new Vector2(size, 0);
            rightVisual.RelativeOffsetAdjustment = new Vector3(1, 0, 0);
            rightVisual.Offset = new Vector3(-size, 0, 0);
            rightVisual.Brush = CreateEffectBrushFromSurface(rightSurface);

            _gripperVisual.Children.InsertAtTop(topVisual);
            _gripperVisual.Children.InsertAtTop(bottomVisual);
            _gripperVisual.Children.InsertAtTop(leftVisual);
            _gripperVisual.Children.InsertAtTop(rightVisual);

            _gripperVisual.RelativeSizeAdjustment = Vector2.One;

            _rootVisual.Children.InsertAtTop(_gripperVisual);
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

        private void EnsureEffectFactory()
        {
            if (s_effectFactory == null)
            {
                var effectDescription = new BorderEffect
                {
                    ExtendX = CanvasEdgeBehavior.Wrap,
                    ExtendY = CanvasEdgeBehavior.Wrap,
                    Source = new CompositionEffectSourceParameter("source")
                };

                s_effectFactory = _compositor.CreateEffectFactory(effectDescription);
            }
        }

        private CompositionBrush CreateEffectBrushFromSurface(ICompositionSurface surface)
        {
            EnsureEffectFactory();
            var surfaceBrush = _compositor.CreateSurfaceBrush(surface);
            var effectBrush = s_effectFactory.CreateBrush();
            effectBrush.SetSourceParameter("source", surfaceBrush);

            return effectBrush;
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

                var delta = pointPosition - _originalPosition;
                var newSize = _rootVisual.Size;
                var newOffset = _rootVisual.Offset;

                if (delta.X < 0)
                {
                    newSize.X = delta.X * -1.0f;
                    newOffset.X = pointPosition.X;
                }
                else
                {
                    newSize.X = delta.X;
                }

                if (delta.Y < 0)
                {
                    newSize.Y = delta.Y * -1.0f;
                    newOffset.Y = pointPosition.Y;
                }
                else
                {
                    newSize.Y = delta.Y;
                }

                _rootVisual.Size = newSize;
                _rootVisual.Offset = newOffset;
            }
        }

        public void CanvasPointerPressed(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            EnsureParent(sender);
            var currentPoint = e.GetCurrentPoint(sender as UIElement);
            var pointPosition = currentPoint.Position.ToVector2();
            _rootVisual.Offset = new Vector3(pointPosition, 0);
            _rootVisual.Size = new Vector2(0);
            _originalPosition = pointPosition;
        }

        public void CanvasPointerReleased(Canvas canvas, object drawLock, object sender, PointerRoutedEventArgs e)
        {
            
        }

        public void Dispose()
        {
            _gripperVisual.Dispose();
            _gripperVisual = null;
            _rootVisual.Dispose();
            _rootVisual = null;
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

        private CanvasDevice _device;
        private CompositionGraphicsDevice _graphicsDevice;

        private Vector2 _originalPosition;

        private static CompositionEffectFactory s_effectFactory;
    }
}
