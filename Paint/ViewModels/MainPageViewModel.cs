using Paint.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Paint.ViewModels
{
    public class ActionCommand : ICommand
    {
        public ActionCommand(Action action)
        {
            _action = action;
        }

        public void SetAction(Action action)
        {
            _action = action;
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public bool CanExecute(object parameter)
        {
            return _action != null;
        }

        public void Execute(object parameter)
        {
            _action.Invoke();
        }

        private Action _action;

        public event EventHandler CanExecuteChanged;
    }

    public class ActionCommand<T> : ICommand
    {
        public ActionCommand(Action<T> action)
        {
            _action = action;
        }

        public void SetAction(Action<T> action)
        {
            _action = action;
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public bool CanExecute(object parameter)
        {
            return _action != null;
        }

        public void Execute(object parameter)
        {
            _action.Invoke((T)parameter);
        }

        private Action<T> _action;

        public event EventHandler CanExecuteChanged;
    }

    public class MainPageViewModel : DependencyObject
    {
        private static DependencyProperty ToolIndexProperty = DependencyProperty.Register(nameof(ToolIndex), typeof(int), typeof(MainPageViewModel), new PropertyMetadata(0, ToolIndexPropertyChanged));
        private static DependencyProperty ToolColorIndexProperty = DependencyProperty.Register(nameof(ToolColorIndex), typeof(int), typeof(MainPageViewModel), new PropertyMetadata(0, ToolColorIndexPropertyChanged));

        private static void ToolColorIndexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = d as MainPageViewModel;
            var value = (int)e.NewValue;

            if (viewModel != null)
            {
                viewModel.SetColorIndex(value);
            }
        }

        private static void ToolIndexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = d as MainPageViewModel;
            var value = (int)e.NewValue;

            if (viewModel != null)
            {
                viewModel.SetToolIndex(value);
            }
        }

        public int ToolIndex { get { return (int)GetValue(ToolIndexProperty); } set { SetValue(ToolIndexProperty, value); } }
        public int ToolColorIndex { get { return (int)GetValue(ToolColorIndexProperty); } set { SetValue(ToolColorIndexProperty, value); } }
        public SolidColorBrush CurrentToolColor { get; } = new SolidColorBrush(Colors.Black);
        public IEnumerable<Color> ToolColors { get { return _colors; } }
        public IReadOnlyList<string> Tools { get; } = new List<string> { "Pencil", "Fill", "Selection" };

        public ICommand Save { get; }
        public ICommand SaveAs { get; }
        public ICommand Undo { get; }
        public ICommand Redo { get; }
        public ICommand Cut { get; }
        public ICommand Copy { get; }
        public ICommand Paste { get; }
        public ICommand NewImage { get; }
        public ICommand OpenFile { get; }
        public ICommand Clear { get; }

        public MainPageViewModel(Rectangle canvasRectangle)
        {
            _compositor = Window.Current.Compositor;

            _core = new PaintCore(_compositor);

            AttachToRectangle(canvasRectangle);

            InitColors();
            SetupInputHandler();

            Save = new ActionCommand(SaveCommand);
            SaveAs = new ActionCommand(SaveAsCommand);
            Undo = new ActionCommand(UndoCommand);
            Redo = new ActionCommand(RedoCommand);
            Cut = new ActionCommand(null);
            Copy = new ActionCommand(CopyCommand);
            Paste = new ActionCommand(PasteCommand);
            NewImage= new ActionCommand<Vector2>(NewImageCommand);
            OpenFile= new ActionCommand(OpenFileCommand);
            Clear = new ActionCommand(ClearCommand);
        }

        private void AttachToRectangle(Rectangle canvasRectangle)
        {
            _canvasRectangle = canvasRectangle;
            _canvasRectangle.Width = _core.CurrentSize.X;
            _canvasRectangle.Height = _core.CurrentSize.Y;
            _core.SizeChanged += OnCanvasSizeChanged;
            ElementCompositionPreview.SetElementChildVisual(_canvasRectangle, _core.Visual);
        }

        private void InitColors()
        {
            _colors = new List<Color>();
            _colors.Add(Colors.Black);
            _colors.Add(Colors.Red);
            _colors.Add(Colors.Blue);
            _colors.Add(Colors.Green);
            _colors.Add(Colors.Yellow);
            _colors.Add(Colors.White);
            _colors.Add(Colors.Brown);
            _colors.Add(Colors.Orange);
            _colors.Add(Colors.Purple);

            ToolColorIndex = _colors.IndexOf(_core.Color);
        }

        private void OnCanvasSizeChanged(object sender, Vector2 e)
        {
            _canvasRectangle.Width = e.X;
            _canvasRectangle.Height = e.Y;
        }

        private void SetupInputHandler()
        {
            var coreWindow = Window.Current.CoreWindow;

            _canvasRectangle.PointerPressed += CanvasRectangle_PointerPressed;
            _canvasRectangle.PointerMoved += CanvasRectangle_PointerMoved;
            _canvasRectangle.PointerReleased += CanvasRectangle_PointerReleased;
            _canvasRectangle.PointerExited += CanvasRectangle_PointerExited;

            coreWindow.KeyUp += CoreWindow_KeyUp;
        }

        private void CanvasRectangle_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _core.CanvasPointerExited(sender, e);
        }

        private void CanvasRectangle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _core.CanvasPointerReleased(sender, e);
        }

        private void CanvasRectangle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _core.CanvasPointerMoved(sender, e);
        }

        private void CanvasRectangle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _core.CanvasPointerPressed(sender, e);
        }

        private bool IsKeyDown(VirtualKey key)
        {
            var coreWindow = Window.Current.CoreWindow;

            return coreWindow.GetKeyState(key).HasFlag(CoreVirtualKeyStates.Down);
        }

        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            var coreWindow = Window.Current.CoreWindow;

            var isControlDown = IsKeyDown(VirtualKey.Control);

            if (isControlDown)
            {
                switch (args.VirtualKey)
                {
                    case VirtualKey.R:
                        _core.Resize(new Vector2(1000, 1000));
                        return;
                    case VirtualKey.N:
                        _core.ClearCanvas();
                        return;
                    case VirtualKey.S:
                        _core.SaveCanvas();
                        return;
                    case VirtualKey.C:
                        CopyToClipboard();
                        return;
                    case VirtualKey.V:
                        PasteFromClipboard();
                        return;
                    case VirtualKey.Z:
                        _core.Undo();
                        return;
                    case VirtualKey.Y:
                        _core.Redo();
                        return;
                }
            }
            else
            {
                switch (args.VirtualKey)
                {

                }
            }

        }

        private void SetColorIndex(int index)
        {
            if (index != -1)
            {
                CurrentToolColor.Color = _colors[ToolColorIndex];

                _core.SetColor(_colors[ToolColorIndex]);
            }
        }

        private async void PasteFromClipboard()
        {
            var dataPackage = Clipboard.GetContent();
            if (dataPackage.Contains(StandardDataFormats.Bitmap))
            {
                var bitmap = await dataPackage.GetBitmapAsync();
                using (var stream = await bitmap.OpenReadAsync())
                {
                    await _core.PasteAsync(stream.AsStream());
                }
            }
        }

        private async void CopyToClipboard()
        {
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;

            var reference = await _core.CopytToStreamReference();

            dataPackage.SetBitmap(reference);
            Clipboard.SetContent(dataPackage);
        }

        private void SaveCommand()
        {
            _core.SaveCanvas();
        }

        private void SaveAsCommand()
        {
            _core.SaveCanvas(null);
        }

        private void OpenFileCommand()
        {
            _core.OpenFile(null);
        }

        private void NewImageCommand(Vector2 input)
        {
            _core.NewImage(input);
        }

        private void ClearCommand()
        {
            _core.ClearCanvas();
        }

        private void UndoCommand()
        {
            _core.Undo();
        }

        private void RedoCommand()
        {
            _core.Redo();
        }

        private void CopyCommand()
        {
            CopyToClipboard();
        }

        private void PasteCommand()
        {
            PasteFromClipboard();
        }

        private void SetToolIndex(int index)
        {
            if (index >= 0 &&
                index < Tools.Count)
            {
                var name = Tools[index];

                switch (name)
                {
                    case "Pencil":
                        _core.SwitchToPencilTool();
                        break;
                    case "Fill":
                        _core.SwitchToFillTool();
                        break;
                    case "Selection":
                        _core.SwitchToSelectionTool();
                        break;
                }
            }
        }

        private Compositor _compositor;
        private List<Color> _colors;

        private PaintCore _core;

        private Rectangle _canvasRectangle;
    }
}
