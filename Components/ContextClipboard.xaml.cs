using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace PasteCat
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
            Relocate();
        }

        public void Relocate()
        {
            Point mouse = transform.Transform(new Point(Control.MousePosition.X, Control.MousePosition.Y));
            System.Drawing.Rectangle screenBounds = Screen.FromControl(new Control()).WorkingArea;

            screenBounds.X = (int)(screenBounds.X * transform.M11);
            screenBounds.Y = (int)(screenBounds.Y * transform.M22);
            screenBounds.Width = (int)(screenBounds.Width * transform.M11);
            screenBounds.Height = (int)(screenBounds.Height * transform.M22);

            double left, top;

            left = mouse.X + ActualWidth > screenBounds.Right ? mouse.X - ActualWidth + 20 : mouse.X - 15;

            if (mouse.Y + ActualHeight > screenBounds.Bottom)
            {
                top = mouse.Y - ActualHeight + 20;
                ItemsVerticalAlignment = VerticalAlignment.Bottom;
                ((App)System.Windows.Application.Current).OrderListItem(false);
                ClipboardList.ScrollIntoView(ClipboardList.Items[ClipboardList.Items.Count - 1]);
            }
            else
            {
                top = mouse.Y - 15;
                ItemsVerticalAlignment = VerticalAlignment.Top;
                ((App)System.Windows.Application.Current).OrderListItem(true);
                ClipboardList.ScrollIntoView(ClipboardList.Items[0]);
            }

            Left = left;
            Top = top;
        }

        #endregion


        #region Event Handler

        private void ContextClipboard_Deactivated(object sender, EventArgs e)
        {
            ((App)System.Windows.Application.Current).OnClickNoPaste(this);
        }

        private void ClipboardList_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            App appInstance = (App)System.Windows.Application.Current;

            if (ClipboardList.SelectedItem != null)
            {
                ClipboardHistoryItem selectedItemText = ClipboardList.SelectedItem as ClipboardHistoryItem;
                System.Windows.Clipboard.SetText(selectedItemText.Text);
                appInstance.OnClickPasteCatem(this);
            }
            else
            {
                appInstance.OnClickNoPaste(this);
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            ((App)System.Windows.Application.Current).OnClickDeleteItem(ClipboardList.SelectedIndex);
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
