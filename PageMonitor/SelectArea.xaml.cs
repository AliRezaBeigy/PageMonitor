using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace TestWpfApp
{
    public partial class SelectArea : Window
    {
        public SelectArea()
        {
            InitializeComponent();
        }

        private enum SelectMode
        {
            PageNumber,
            ChatBox
        }

        private readonly double DragThreshold = 5;

        private Rect pageNumberRect { get; set; }
        private Point origMouseDownPoint { get; set; }
        private bool isDraggingSelectionRect { get; set; }
        private bool isLeftMouseButtonDownOnWindow { get; set; }
        private SelectMode SelectRectMode { get; set; } = SelectMode.PageNumber;

        public void ResetState()
        {
            isDraggingSelectionRect = false;
            isLeftMouseButtonDownOnWindow = false;
            SelectRectMode = SelectMode.PageNumber;
            dragSelectionText.Text = "Select Page Number Area";
            chatBoxCanvas.Visibility = Visibility.Collapsed;
            pageNumberCanvas.Visibility = Visibility.Collapsed;
            dragSelectionCanvas.Visibility = Visibility.Collapsed;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isLeftMouseButtonDownOnWindow = true;
                origMouseDownPoint = e.GetPosition(this);

                CaptureMouse();

                e.Handled = true;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingSelectionRect)
            {
                Point curMouseDownPoint = e.GetPosition(this);
                UpdateDragSelectionRect(origMouseDownPoint, curMouseDownPoint, dragSelectionBorder, dragSelectionText);

                e.Handled = true;
            }
            else if (isLeftMouseButtonDownOnWindow)
            {
                Point curMouseDownPoint = e.GetPosition(this);
                var dragDelta = curMouseDownPoint - origMouseDownPoint;
                var dragDistance = Math.Abs(dragDelta.Length);
                if (dragDistance > DragThreshold)
                {
                    isDraggingSelectionRect = true;

                    UpdateDragSelectionRect(origMouseDownPoint, curMouseDownPoint, dragSelectionBorder, dragSelectionText);

                    dragSelectionCanvas.Visibility = Visibility.Visible;
                }

                e.Handled = true;
            }
        }
        private void UpdateDragSelectionRect(Point pt1, Point pt2, Border border, TextBlock textblock)
        {
            double x, y, width, height;

            if (pt2.X < pt1.X)
            {
                x = pt2.X;
                width = pt1.X - pt2.X;
            }
            else
            {
                x = pt1.X;
                width = pt2.X - pt1.X;
            }

            if (pt2.Y < pt1.Y)
            {
                y = pt2.Y;
                height = pt1.Y - pt2.Y;
            }
            else
            {
                y = pt1.Y;
                height = pt2.Y - pt1.Y;
            }

            border.Width = width;
            border.Height = height;
            Canvas.SetTop(border, y);
            Canvas.SetLeft(border, x);
            Canvas.SetLeft(textblock, x);
            Canvas.SetTop(textblock, y - 25);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (isDraggingSelectionRect)
                {
                    isDraggingSelectionRect = false;

                    dragSelectionCanvas.Visibility = Visibility.Collapsed;

                    var x = Canvas.GetLeft(dragSelectionBorder);
                    var y = Canvas.GetTop(dragSelectionBorder);
                    var width = dragSelectionBorder.Width;
                    var height = dragSelectionBorder.Height;
                    var dragRect = new Rect(x, y, width, height);

                    var curMouseDownPoint = e.GetPosition(this);

                    switch (SelectRectMode)
                    {
                        case SelectMode.PageNumber:
                            pageNumberRect = dragRect;
                            UpdateDragSelectionRect(origMouseDownPoint, curMouseDownPoint, pageNumberBorder, pageNumberText);
                            pageNumberCanvas.Visibility = Visibility.Visible;
                            dragSelectionText.Text = "Select ChatBox Area";
                            SelectRectMode = SelectMode.ChatBox;
                            break;
                        case SelectMode.ChatBox:
                            UpdateDragSelectionRect(origMouseDownPoint, curMouseDownPoint, chatBoxBorder, chatBoxText);
                            chatBoxCanvas.Visibility = Visibility.Visible;

                            Hide();

                            var mainWindow= new MainWindow(pageNumberRect, dragRect);
                            mainWindow.Owner = this;
                            mainWindow.Show();
                            break;
                    }

                    e.Handled = true;
                }

                if (isLeftMouseButtonDownOnWindow)
                {
                    isLeftMouseButtonDownOnWindow = false;
                    ReleaseMouseCapture();

                    e.Handled = true;
                }
            }
        }
    }
}
