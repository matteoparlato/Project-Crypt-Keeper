using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Power_SCADA_Builder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<ImageViewModel> images = new ObservableCollection<ImageViewModel>();
        private Image selectedImage;

        public MainWindow()
        {
            InitializeComponent();

            // Add some predefined images to the collection
            images.Add(new ImageViewModel("pack://application:,,,/Resources/AvatarFourDotsOff.png"));
            images.Add(new ImageViewModel("pack://application:,,,/Resources/AvatarFourDotsOff.png"));

            imageListView.ItemsSource = images;
            canvas.MouseMove += Canvas_MouseMove;
        }

        private void Image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image)
            {
                selectedImage = image;

                DataObject data = new DataObject();
                data.SetData(DataFormats.StringFormat, image.Source.ToString()); // Set the data format

                DragDrop.DoDragDrop(image, data, DragDropEffects.Copy);
            }
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            if (selectedImage != null)
            {
                string imageSourceString = e.Data.GetData(DataFormats.StringFormat) as string;

                if (!string.IsNullOrEmpty(imageSourceString))
                {
                    BitmapImage imageSource = new BitmapImage(new Uri(imageSourceString));
                    Image newImage = new Image { Source = imageSource, Width = 100, Height = 100 };
                    canvas.Children.Add(newImage);

                    Point dropPoint = e.GetPosition(canvas);
                    Canvas.SetLeft(newImage, dropPoint.X);
                    Canvas.SetTop(newImage, dropPoint.Y);

                    // Enable resizing of the dropped image
                    newImage.MouseMove += Image_MouseMove;
                    newImage.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                    newImage.MouseLeftButtonUp += Image_MouseLeftButtonUp;

                    selectedImage = null;
                }
            }
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is Image image)
            {
                Canvas canvas = image.Parent as Canvas;

                if (canvas != null)
                {
                    double newX = e.GetPosition(canvas).X - image.ActualWidth / 2;
                    double newY = e.GetPosition(canvas).Y - image.ActualHeight / 2;

                    Canvas.SetLeft(image, newX);
                    Canvas.SetTop(image, newY);
                }
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectedImage = sender as Image;
            selectedImage.CaptureMouse();
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedImage != null)
            {
                selectedImage.ReleaseMouseCapture();
                selectedImage = null;
            }
        }

        private void Canvas_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void SetBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;
                ImageBrush brush = new ImageBrush(new BitmapImage(new Uri(imagePath)));
                canvas.Background = brush;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            List<ImageData> imageDataList = new List<ImageData>();

            foreach (UIElement element in canvas.Children)
            {
                if (element is Image image)
                {
                    ImageViewModel imageViewModel = image.DataContext as ImageViewModel;

                    double left = Canvas.GetLeft(image);
                    double top = Canvas.GetTop(image);
                    double width = image.ActualWidth;
                    double height = image.ActualHeight;

                    //Dictionary<string, string> properties = imageViewModel.Properties;

                    ImageData imageData = new ImageData
                    {
                        Left = left,
                        Top = top,
                        Width = width,
                        Height = height,
                        //Properties = properties
                    };

                    imageDataList.Add(imageData);
                }
            }

            string json = JsonConvert.SerializeObject(imageDataList, Formatting.Indented);
            File.WriteAllText("exported_data.json", json);

            MessageBox.Show("Export completed. Data saved to 'exported_data.json'");
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point cursorPosition = e.GetPosition(canvas);
            coordinatesTextBlock.Text = $"X: {cursorPosition.X}, Y: {cursorPosition.Y}";
        }
    }

    public class ImageViewModel
    {
        public string ImageSource { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public ImageViewModel(string imageSource)
        {
            ImageSource = imageSource;
            Properties = new Dictionary<string, string>();
        }
    }

    public class ImageData
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }
}
