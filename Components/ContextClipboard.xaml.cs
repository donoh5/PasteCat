using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace PasteIt
{
    public partial class ContextClipboard : Window
    {
        private Matrix transform;

        public ContextClipboard()
        {
            InitializeComponent();

            DataContext = this;
            ShowInTaskbar = false;
            Deactivated += ContextClipboard_Deactivated;
            ClipboardList.MouseLeftButtonUp += ClipboardList_MouseLeftButtonUp;
            PreviewKeyDown += ContextClipboard_PreviewKeyDown;
        }

        #region Window Location

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
            MoveBottomRightEdgeOfWindowToMousePosition();
        }

        public void Relocate()
        {
            MoveBottomRightEdgeOfWindowToMousePosition();
        }

        private void MoveBottomRightEdgeOfWindowToMousePosition()
        {
            Point mouse = transform.Transform(GetMousePosition());
            System.Drawing.Rectangle screenBounds = GetScaledWorkingArea(transform);

            double left, top;

            left = mouse.X + ActualWidth > screenBounds.Right ? mouse.X - ActualWidth + 20 : mouse.X - 15;

            if (mouse.Y + ActualHeight > screenBounds.Bottom)
            {
                top = mouse.Y - ActualHeight + 20;
                ItemsVerticalAlignment = VerticalAlignment.Bottom;
                ((App)System.Windows.Application.Current).OrderFromBottom();
            }
            else
            {
                top = mouse.Y - 15;
                ItemsVerticalAlignment = VerticalAlignment.Top;
                ((App)System.Windows.Application.Current).OrderFromTop();
            }

            Left = left;
            Top = top;
        }

        private static Point GetMousePosition()
        {
            System.Drawing.Point point = Control.MousePosition;
            return new Point(point.X, point.Y);
        }

        private System.Drawing.Rectangle GetScaledWorkingArea(Matrix transform)
        {
            Screen screen = Screen.FromControl(new Control());
            System.Drawing.Rectangle workingArea = screen.WorkingArea;

            workingArea.X = (int)(workingArea.X * transform.M11);
            workingArea.Y = (int)(workingArea.Y * transform.M22);
            workingArea.Width = (int)(workingArea.Width * transform.M11);
            workingArea.Height = (int)(workingArea.Height * transform.M22);

            return workingArea;
        }

        #endregion


        #region Event Handler

        private void ContextClipboard_Deactivated(object sender, EventArgs e)
        {
            App appInstance = (App)System.Windows.Application.Current;

            appInstance.OnClickNoPaste(this);
        }

        private void ClipboardList_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            App appInstance = (App)System.Windows.Application.Current;

            if (ClipboardList.SelectedItem != null)
            {
                string selectedItemText = ClipboardList.SelectedItem as string;
                if (ClipboardList.SelectedIndex != 0)
                {
                    appInstance.OnClickDeleteItem(ClipboardList.SelectedIndex);
                }
                System.Windows.Clipboard.SetText(selectedItemText);
                appInstance.OnClickPasteItem(this);
            }
            else
            {
                appInstance.OnClickNoPaste(this);
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            App appInstance = (App)System.Windows.Application.Current;

            int selectedIndex = ClipboardList.SelectedIndex;

            appInstance.OnClickDeleteItem(selectedIndex);
        }

        private void ContextClipboard_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Hide();
            }
        }

        #endregion

    }

    public partial class ContextClipboard : Window, INotifyPropertyChanged
    {
        private VerticalAlignment _itemsVerticalAlignment;
        public VerticalAlignment ItemsVerticalAlignment
        {
            get { return _itemsVerticalAlignment; }
            set
            {
                _itemsVerticalAlignment = value;
                OnPropertyChanged("ItemsVerticalAlignment");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
