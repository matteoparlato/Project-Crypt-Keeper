using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Svg;
using Newtonsoft.Json;

namespace Power_SCADA_Builder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _dragStartPoint;
        private Image _draggedImage;
        private Point _resizeStartPoint;
        private Image _resizingImage;
        private Border _selectionBorder;

        private List<ImageInfo> _imageInfoList = new List<ImageInfo>();

        public MainWindow()
        {
            InitializeComponent();

            // Load default images from resources
            List<DefaultImage> defaultImages = new List<DefaultImage>
            {
                new DefaultImage { Name = "Image 1", Image = LoadImageFromResource("AddEntity.png") },
                new DefaultImage { Name = "Image 2", Image = LoadImageFromResource("LinkAlert.png") },
                // Add more default images here
            };

            sidePanel.ItemsSource = defaultImages;

            // Attach event handlers for canvas
            mainCanvas.MouseMove += MainCanvas_MouseMove;
            mainCanvas.MouseLeave += MainCanvas_MouseLeave;
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point cursorPosition = e.GetPosition(mainCanvas);
            statusText.Text = $"Cursor Position: {cursorPosition.X}, {cursorPosition.Y}";
        }

        private void MainCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            statusText.Text = "Cursor Position: N/A";
        }

        private BitmapImage LoadImageFromResource(string resourceName)
        {
            BitmapImage image = new BitmapImage();
            using (Stream stream = GetType().Assembly.GetManifestResourceStream($"Power_SCADA_Builder.Resources.{resourceName}"))
            {
                if (stream != null)
                {
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                }
            }
            return image;
        }

        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif|All files|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                Image image = new Image();
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                image.Source = bitmap;
                image.MouseDown += Image_MouseDown;

                // Add image info to the list
                _imageInfoList.Add(new ImageInfo
                {
                    FileName = openFileDialog.FileName,
                    X = 50,
                    Y = 50
                });

                // Adjust image position and size as needed
                Canvas.SetLeft(image, 50);
                Canvas.SetTop(image, 50);

                mainCanvas.Children.Add(image);
            }
        }

        private void DefaultImage_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image clickedImage)
            {
                Image newImage = new Image
                {
                    Source = clickedImage.Source.Clone(),
                    Width = 100,
                    Height = 100
                };

                // Add image to the canvas
                Canvas.SetLeft(newImage, 50);
                Canvas.SetTop(newImage, 50);
                mainCanvas.Children.Add(newImage);

                // Add image info to the list
                _imageInfoList.Add(new ImageInfo
                {
                    FileName = null,
                    X = 50,
                    Y = 50
                });

                // Attach event handlers for moving the new image
                newImage.MouseDown += Image_MouseDown;
                newImage.MouseMove += Image_MouseMove;
                newImage.MouseUp += Image_MouseUp;
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _draggedImage = sender as Image;
            _dragStartPoint = e.GetPosition(mainCanvas);
            _draggedImage.CaptureMouse();

            if (IsResizeHitTest(sender as Image, e))
            {
                _resizingImage = sender as Image;
                _resizeStartPoint = e.GetPosition(_resizingImage);
                _resizingImage.CaptureMouse();
            }
            else
            {
                DeselectAllImages();
                SelectImage(sender as Image);
            }
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedImage != null)
            {
                Point currentPosition = e.GetPosition(mainCanvas);
                double deltaX = currentPosition.X - _dragStartPoint.X;
                double deltaY = currentPosition.Y - _dragStartPoint.Y;

                double newLeft = Canvas.GetLeft(_draggedImage) + deltaX;
                double newTop = Canvas.GetTop(_draggedImage) + deltaY;

                if (newLeft < 0) newLeft = 0;
                if (newTop < 0) newTop = 0;

                if (newLeft + _draggedImage.ActualWidth > mainCanvas.ActualWidth)
                    newLeft = mainCanvas.ActualWidth - _draggedImage.ActualWidth;

                if (newTop + _draggedImage.ActualHeight > mainCanvas.ActualHeight)
                    newTop = mainCanvas.ActualHeight - _draggedImage.ActualHeight;

                Canvas.SetLeft(_draggedImage, newLeft);
                Canvas.SetTop(_draggedImage, newTop);

                int index = mainCanvas.Children.IndexOf(_draggedImage);
                _imageInfoList[index].X = newLeft;
                _imageInfoList[index].Y = newTop;

                _dragStartPoint = currentPosition;
            }

            if (_resizingImage != null)
            {
                ResizeImage(_resizingImage, e.GetPosition(mainCanvas));
            }
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedImage != null)
            {
                _draggedImage.ReleaseMouseCapture();
                _draggedImage = null;
            }

            if (_resizingImage != null)
            {
                _resizingImage.ReleaseMouseCapture();
                _resizingImage = null;
            }
        }

        private void DeselectAllImages()
        {
            if (_selectionBorder != null)
            {
                mainCanvas.Children.Remove(_selectionBorder);
                _selectionBorder = null;
            }
        }

        private void SelectImage(Image image)
        {
            double borderWidth = 1.5;
            double selectionSquareSize = 10;

            _selectionBorder = new Border
            {
                BorderBrush = Brushes.DodgerBlue,
                BorderThickness = new Thickness(borderWidth),
                Width = image.Width + borderWidth * 2,
                Height = image.Height + borderWidth * 2,
                CornerRadius = new CornerRadius(5),
                Opacity = 0.7
            };

            Canvas.SetLeft(_selectionBorder, Canvas.GetLeft(image) - borderWidth);
            Canvas.SetTop(_selectionBorder, Canvas.GetTop(image) - borderWidth);

            /*
            Image resizeSquare = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Small.png", UriKind.Relative)),
                Width = selectionSquareSize,
                Height = selectionSquareSize,
                Cursor = Cursors.SizeNWSE
            };
            resizeSquare.MouseDown += ResizeSquare_MouseDown;

            Canvas.SetLeft(resizeSquare, Canvas.GetLeft(image) + image.Width - selectionSquareSize);
            Canvas.SetTop(resizeSquare, Canvas.GetTop(image) + image.Height - selectionSquareSize);

            _selectionBorder.Child = resizeSquare;
            */
            mainCanvas.Children.Add(_selectionBorder);
        }

        private void ResizeSquare_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _resizingImage = _selectionBorder.Child as Image;
            _resizeStartPoint = e.GetPosition(mainCanvas);
            _resizingImage.CaptureMouse();
        }


        private bool IsResizeHitTest(UIElement element, MouseEventArgs e)
        {
            if (element != null)
            {
                Point position = e.GetPosition(element);
                double width = element.RenderSize.Width;
                double height = element.RenderSize.Height;
                double hitZone = 6;

                return position.X >= width - hitZone && position.Y >= height - hitZone;
            }

            return false;
        }

        private void ResizeImage(Image image, Point mousePosition)
        {
            double borderWidth = 1.5;

            double deltaX = mousePosition.X - _resizeStartPoint.X;
            double deltaY = mousePosition.Y - _resizeStartPoint.Y;

            double newWidth = image.Width + deltaX;
            double newHeight = image.Height + deltaY;

            if (newWidth > 20 && newHeight > 20) // Minimum size
            {
                double currentLeft = Canvas.GetLeft(image);
                double currentTop = Canvas.GetTop(image);

                double newLeft = currentLeft - deltaX;
                double newTop = currentTop - deltaY;

                if (newLeft < 0) newLeft = 0;
                if (newTop < 0) newTop = 0;

                _resizeStartPoint = mousePosition;

                if (_selectionBorder != null)
                {
                    double selectionSquareSize = 10;
                    Canvas.SetLeft(_selectionBorder, newLeft - borderWidth);
                    Canvas.SetTop(_selectionBorder, newTop - borderWidth);

                    Image resizeSquare = _selectionBorder.Child as Image;
                    Canvas.SetLeft(resizeSquare, newLeft + newWidth - selectionSquareSize);
                    Canvas.SetTop(resizeSquare, newTop + newHeight - selectionSquareSize);
                }

                int index = mainCanvas.Children.IndexOf(image);
                _imageInfoList[index].Width = newWidth;
                _imageInfoList[index].Height = newHeight;

                image.Width = newWidth;
                image.Height = newHeight;

                Canvas.SetLeft(image, newLeft);
                Canvas.SetTop(image, newTop);
            }
        }


        private void ExportSvg_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "SVG files|*.svg";

            if (saveFileDialog.ShowDialog() == true)
            {
                SvgDocument svgDocument = new SvgDocument();

                foreach (ImageInfo imageInfo in _imageInfoList)
                {
                    SvgImage svgImage = new SvgImage
                    {
                        X = (float)imageInfo.X,
                        Y = (float)imageInfo.Y,
                        Width = 100, // You can set the width and height as per your requirements
                        Height = 100,
                        Href = new Uri(imageInfo.FileName, UriKind.Absolute).ToString()
                    };

                    svgDocument.Children.Add(svgImage);
                }

                svgDocument.Write(saveFileDialog.FileName);
            }
        }

        private void ExportJson_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files|*.json";

            if (saveFileDialog.ShowDialog() == true)
            {
                string json = JsonConvert.SerializeObject(_imageInfoList, Formatting.Indented);
                File.WriteAllText(saveFileDialog.FileName, json);
            }
        }
    }

    public class ImageInfo
    {
        public string FileName { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public class DefaultImage
    {
        public string Name { get; set; }
        public BitmapImage Image { get; set; }
    }
}
