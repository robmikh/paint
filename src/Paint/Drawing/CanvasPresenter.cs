using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Composition;
using Paint.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Composition;

namespace Paint.Drawing
{
    class CanvasPresenter : IDisposable
    {
        public CanvasPresenter(Canvas canvas, CanvasDevice device)
        {
            var dpi = GraphicsInformation.Dpi;

            _device = device;
            _canvas = canvas;

            DrawBackgroundBrush();

            _swapChain = new CanvasSwapChain(_device, canvas.Size.X, canvas.Size.Y, dpi);
        }

        private void DrawLoop()
        {
            var canceled = _drawLoopCancellationTokenSource.Token;

            try
            {
                while (!canceled.IsCancellationRequested)
                {
                    DrawFrame();
                    _swapChain.Present();
                    _swapChain.WaitForVerticalBlank();
                }

                _swapChain.Dispose();
                _swapChain = null;
                _swapChainDestructionEvent.Set();
            }
            catch (Exception e) when (_swapChain.Device.IsDeviceLost(e.HResult))
            {
                _swapChain.Device.RaiseDeviceLost();
            }
        }

        private void DrawFrame()
        {
            using (var drawingSession = _swapChain.CreateDrawingSession(Colors.Transparent))
            {
                drawingSession.FillRectangle(0, 0, _canvas.Size.X, _canvas.Size.Y, _backgroundBrush);
                _canvas.Blit(drawingSession);
            }
        }

        private void StartDrawLoop()
        {
            _drawLoopCancellationTokenSource = new CancellationTokenSource();
            _swapChainDestructionEvent = new AutoResetEvent(false);
            Task.Factory.StartNew(
                DrawLoop,
                _drawLoopCancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private void DrawBackgroundBrush()
        {
            var dpi = GraphicsInformation.Dpi;

            if (_backgroundBrush != null)
            {
                _backgroundBrush.Dispose();
                _backgroundBrush = null;
            }

            using (var renderTarget = new CanvasRenderTarget(_device, 16, 16, dpi))
            {
                using (var drawingSession = renderTarget.CreateDrawingSession())
                {
                    drawingSession.Clear(Colors.Gray);
                    drawingSession.FillRectangle(8, 0, 8, 8, Colors.DarkGray);
                    drawingSession.FillRectangle(0, 8, 8, 8, Colors.DarkGray);
                }

                _backgroundBrush = new CanvasImageBrush(_device, renderTarget);
                _backgroundBrush.ExtendX = CanvasEdgeBehavior.Wrap;
                _backgroundBrush.ExtendY = CanvasEdgeBehavior.Wrap;
            }
        }

        public void Start()
        {
            StartDrawLoop();
        }

        public void Stop()
        {
            if (_drawLoopCancellationTokenSource != null &&
                !_drawLoopCancellationTokenSource.IsCancellationRequested)
            {
                _drawLoopCancellationTokenSource.Cancel();
                _swapChainDestructionEvent.WaitOne();

                if (_drawLoopCancellationTokenSource != null)
                {
                    _drawLoopCancellationTokenSource.Dispose();
                    _drawLoopCancellationTokenSource = null;
                }

                if (_swapChainDestructionEvent != null)
                {
                    _swapChainDestructionEvent.Dispose();
                    _swapChainDestructionEvent = null;
                }
            }
        }

        public void Resize()
        {
            _swapChain.ResizeBuffers(_canvas.Size.ToSize());
        }

        public ICompositionSurface GetSurface(Compositor compositor)
        {
            return CanvasComposition.CreateCompositionSurfaceForSwapChain(compositor, _swapChain);
        }

        public void Dispose()
        {
            Stop();
            _device = null;
            _backgroundBrush.Dispose();
            _backgroundBrush = null;
        }

        private Canvas _canvas;
        private CanvasDevice _device;
        private CanvasSwapChain _swapChain;
        private CanvasImageBrush _backgroundBrush;
        private CancellationTokenSource _drawLoopCancellationTokenSource;
        private AutoResetEvent _swapChainDestructionEvent;
    }
}
