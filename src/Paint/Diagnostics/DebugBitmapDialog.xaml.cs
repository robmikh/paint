using Microsoft.Graphics.Canvas;
using Robmikh.CompositionSurfaceFactory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Paint.Diagnostics
{
    public sealed partial class DebugBitmapDialog : ContentDialog, IDisposable
    {
        private CanvasBitmap _bitmap;
        private CompositionDrawingSurface _surface;

        public DebugBitmapDialog(string title, CanvasBitmap bitmap)
        {
            this.InitializeComponent();

            Title = title;
            RootGrid.Width = bitmap.Size.Width;
            RootGrid.Height = bitmap.Size.Height;

            var compositor = Window.Current.Compositor;
            var visual = compositor.CreateSpriteVisual();
            visual.RelativeSizeAdjustment = Vector2.One;

            var brush = compositor.CreateSurfaceBrush();
            brush.Stretch = CompositionStretch.Uniform;
            brush.BitmapInterpolationMode = CompositionBitmapInterpolationMode.NearestNeighbor;
            brush.HorizontalAlignmentRatio = 0.5f;
            brush.VerticalAlignmentRatio = 0.5f;

            visual.Brush = brush;

            var surfaceFactory = SurfaceFactory.GetSharedSurfaceFactoryForCompositor(compositor);
            _surface = surfaceFactory.CreateSurface(bitmap.Size);
            SurfaceUtilities.FillSurfaceWithDirect3DSurface(surfaceFactory, _surface, bitmap);
            brush.Surface = _surface;
            _bitmap = bitmap;

            ElementCompositionPreview.SetElementChildVisual(RootGrid, visual);
        }

        public void Dispose()
        {
            _surface.Dispose();
            _surface = null;
            var visual = ElementCompositionPreview.GetElementChildVisual(RootGrid);
            ElementCompositionPreview.SetElementChildVisual(RootGrid, null);
            visual.Dispose();
        }

        private async void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.SuggestedFileName = "debug";
            picker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
            picker.DefaultFileExtension = ".png";

            var file = await picker.PickSaveFileAsync();

            if (file != null)
            {
                using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    await _bitmap.SaveAsync(stream, CanvasBitmapFileFormat.Png, 1.0f);
                }

                await Launcher.LaunchFileAsync(file);
            }
        }
    }
}
