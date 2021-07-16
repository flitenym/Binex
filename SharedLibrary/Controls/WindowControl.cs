using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;

namespace SharedLibrary.Controls
{
    public class WindowControl : Window
    {
        public static bool restoreIfMove = false;

        public string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        static WindowControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowControl),
                new FrameworkPropertyMetadata(typeof(WindowControl))); 
        }

        public WindowControl()
            : base()
        {
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            PreviewMouseMove += OnPreviewMouseMove;
            this.StateChanged += OnStateChanged;
        }

        protected void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed)
                Cursor = Cursors.Arrow;
        }

        protected void OnStateChanged(object sender, EventArgs e)
        {
            stateChangeWork();
        }

        public void stateChangeWork() {
            Border border = this.Template.FindName("PART_WindowBorder", this) as Border;
            StackPanel buttonsStackPanel = this.Template.FindName("buttonsStackPanel", this) as StackPanel;
            Button restoreButton = this.Template.FindName("restoreButton", this) as Button;
            TextBlock titleTextBlock = this.Template.FindName("titleTextBlock", this) as TextBlock; 

            if (WindowState == WindowState.Maximized)
            {
                border.Margin = new Thickness(-1, 6, -1, 5); 
                titleTextBlock.Margin = new Thickness(14, 0, 0, 0);
                buttonsStackPanel.Margin = new Thickness(0, 0, 8, 0);
                restoreButton.Content = 2;
            }
            else if (WindowState == WindowState.Normal)
            {
                border.Margin = new Thickness(0); 
                titleTextBlock.Margin = new Thickness(10, 0, 0, 0);
                buttonsStackPanel.Margin = new Thickness(0, 0, 4, 0);
                restoreButton.Content = 1;
            }
        }

        #region Click events

        protected void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        protected void RestoreClick(object sender, RoutedEventArgs e)
        { 
            stateChangeWork();
            WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal; 
        }

        protected void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion


        public override void OnApplyTemplate()
        {
            Button minimizeButton = GetTemplateChild("minimizeButton") as Button;
            if (minimizeButton != null)
                minimizeButton.Click += MinimizeClick;

            Button restoreButton = GetTemplateChild("restoreButton") as Button;
            if (restoreButton != null)
                restoreButton.Click += RestoreClick;

            Button closeButton = GetTemplateChild("closeButton") as Button;
            if (closeButton != null)
                closeButton.Click += CloseClick;

            Rectangle moveRectangle = GetTemplateChild("moveRectangle") as Rectangle;
            if (moveRectangle != null)
            {
                moveRectangle.PreviewMouseDown += moveRectangle_PreviewMouseDown; 
                moveRectangle.MouseLeftButtonUp += moveRectangle_MouseLeftButtonUp;
                moveRectangle.MouseMove += moveRectangle_MouseMove;
            }

            restoreIfMove = false;

            Grid resizeGrid = GetTemplateChild("resizeGrid") as Grid;
            if (resizeGrid != null)
            {
                foreach (UIElement element in resizeGrid.Children)
                {
                    Rectangle resizeRectangle = element as Rectangle;
                    if (resizeRectangle != null)
                    {
                        resizeRectangle.PreviewMouseDown += ResizeRectangle_PreviewMouseDown;
                        resizeRectangle.MouseMove += ResizeRectangle_MouseMove;
                    }
                }
            }

            stateChangeWork();
            base.OnApplyTemplate();
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        private void moveRectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (restoreIfMove)
            {
                restoreIfMove = false;

                double percentHorizontal = e.GetPosition(this).X / ActualWidth;
                double targetHorizontal = RestoreBounds.Width * percentHorizontal;

                double percentVertical = e.GetPosition(this).Y / ActualHeight;
                double targetVertical = RestoreBounds.Height * percentVertical;

                WindowState = WindowState.Normal;

                POINT lMousePosition;
                GetCursorPos(out lMousePosition);

                Left = lMousePosition.X - targetHorizontal;
                Top = lMousePosition.Y - targetVertical;

                if (Mouse.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            }
        }

        private void moveRectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) { return; }
            if (e.ClickCount == 2)
            {
                SwitchState();
                return;
            }
            else if (WindowState == WindowState.Maximized)
            {
                restoreIfMove = true;
                return;
            }
            DragMove(); 
        }

        private void moveRectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            restoreIfMove = false;
        }

        private void SwitchState()
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    {
                        WindowState = WindowState.Maximized;
                        break;
                    }
                case WindowState.Maximized:
                    {
                        WindowState = WindowState.Normal;
                        break;
                    }
            }
        }

        private void moveRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                SwitchState();
                return;
            }
            else if (WindowState == WindowState.Maximized)
            {
                restoreIfMove = true;
                return;
            }
            DragMove();
        }


        protected void ResizeRectangle_MouseMove(Object sender, MouseEventArgs e)
        {
            if (WindowState == WindowState.Maximized) return;
            Rectangle rectangle = sender as Rectangle;
            switch (rectangle.Name)
            {
                case "top":
                    Cursor = Cursors.SizeNS;
                    break;
                case "bottom":
                    Cursor = Cursors.SizeNS;
                    break;
                case "left":
                    Cursor = Cursors.SizeWE;
                    break;
                case "right":
                    Cursor = Cursors.SizeWE;
                    break;
                case "topLeft":
                    Cursor = Cursors.SizeNWSE;
                    break;
                case "topRight":
                    Cursor = Cursors.SizeNESW;
                    break;
                case "bottomLeft":
                    Cursor = Cursors.SizeNESW;
                    break;
                case "bottomRight":
                    Cursor = Cursors.SizeNWSE;
                    break;
                default:
                    break;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

        protected void ResizeRectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized) return;
            Rectangle rectangle = sender as Rectangle;
            switch (rectangle.Name)
            {
                case "top":
                    Cursor = Cursors.SizeNS;
                    ResizeWindow(ResizeDirection.Top);
                    break;
                case "bottom":
                    Cursor = Cursors.SizeNS;
                    ResizeWindow(ResizeDirection.Bottom);
                    break;
                case "left":
                    Cursor = Cursors.SizeWE;
                    ResizeWindow(ResizeDirection.Left);
                    break;
                case "right":
                    Cursor = Cursors.SizeWE;
                    ResizeWindow(ResizeDirection.Right);
                    break;
                case "topLeft":
                    Cursor = Cursors.SizeNWSE;
                    ResizeWindow(ResizeDirection.TopLeft);
                    break;
                case "topRight":
                    Cursor = Cursors.SizeNESW;
                    ResizeWindow(ResizeDirection.TopRight);
                    break;
                case "bottomLeft":
                    Cursor = Cursors.SizeNESW;
                    ResizeWindow(ResizeDirection.BottomLeft);
                    break;
                case "bottomRight":
                    Cursor = Cursors.SizeNWSE;
                    ResizeWindow(ResizeDirection.BottomRight);
                    break;
                default:
                    break;
            }
        }

        private void ResizeWindow(ResizeDirection direction)
        {
            SendMessage(_hwndSource.Handle, 0x112, (IntPtr)(61440 + direction), IntPtr.Zero);
        }

        private enum ResizeDirection
        {
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8,
        }

        private HwndSource _hwndSource;

        protected override void OnInitialized(EventArgs e)
        {
            SourceInitialized += OnSourceInitialized;
            base.OnInitialized(e);
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            _hwndSource = (HwndSource)PresentationSource.FromVisual(this); 
        }

    }

}
