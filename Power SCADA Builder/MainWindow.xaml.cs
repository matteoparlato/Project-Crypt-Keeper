using ExCSS;
using Microsoft.Win32;
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
using System.Text.Json;

namespace Power_SCADA_Builder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<ImageViewModel> images = new ObservableCollection<ImageViewModel>();
        private Image? selectedImage;
        private Dictionary<string, imgProp> store = new Dictionary<string, imgProp>();
        private Dictionary<string, Image> storeImage = new Dictionary<string, Image>();


        //inizializza le immagini standard
        public MainWindow()
        {
            InitializeComponent();

            // Add some predefined images to the collection

            images.Add(new ImageViewModel("C:/Users/usoffia/source/repos/Project-Crypt-Keeper/Power SCADA Builder/Resources/motor.png"));
            images.Add(new ImageViewModel("C:/Users/usoffia/source/repos/Project-Crypt-Keeper/Power SCADA Builder/Resources/analog.jpg"));
            images.Add(new ImageViewModel("C:/Users/usoffia/source/repos/Project-Crypt-Keeper/Power SCADA Builder/Resources/label.png"));
            images.Add(new ImageViewModel("C:/Users/usoffia/source/repos/Project-Crypt-Keeper/Power SCADA Builder/Resources/valve.png"));
            images.Add(new ImageViewModel("C:/Users/usoffia/source/repos/Project-Crypt-Keeper/Power SCADA Builder/Resources/button.png"));
            images.Add(new ImageViewModel("C:/Users/usoffia/source/repos/Project-Crypt-Keeper/Power SCADA Builder/Resources/digital.png"));


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
                string? imageSourceString = e.Data.GetData(DataFormats.StringFormat) as string;

                if (!string.IsNullOrEmpty(imageSourceString))
                {
                    BitmapImage imageSource = new BitmapImage(new Uri(imageSourceString));
                    Image newImage = new Image { Source = imageSource, Width = 100, Height = 100 };
                    
                    var rand =new Random();
                    newImage.Uid = rand.Next().ToString();
                    //define device type
                    var deviceType = "";
                    if (imageSourceString.Contains("analog")) deviceType = "analog";
                    if (imageSourceString.Contains("motor")) deviceType = "motor";
                    if (imageSourceString.Contains("valve")) deviceType = "valve";
                    if (imageSourceString.Contains("digital")) deviceType = "digital";
                    if (imageSourceString.Contains("button")) deviceType = "button";
                    if (imageSourceString.Contains("label")) deviceType = "label";

                    store.Add(newImage.Uid, new imgProp("",deviceType, newImage.Uid));
                    storeImage.Add(newImage.Uid, newImage);
                        
                    canvas.Children.Add(newImage);

                    var dropPoint = e.GetPosition(canvas);
                    Canvas.SetLeft(newImage, dropPoint.X);
                    Canvas.SetTop(newImage, dropPoint.Y);

                    
                    // Enable resizing of the dropped image
                    newImage.MouseMove += Image_MouseMove;
                    newImage.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                    newImage.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                    newImage.MouseRightButtonDown += Image_MouseRightButtonDown;

                    selectedImage = null;
                    
                }
            }
        }

        private void Image_MouseRightButtonDown(object sender, MouseEventArgs e)
        {
            

            var nameBox = (TextBox)FindName("DeviceName");
            var DeviceSize = (TextBox)FindName("DeviceSize");
            var DeviceFont = (TextBox)FindName("DeviceFont");
            var DeviceColor = (TextBox)FindName("DeviceColor");
            var DeviceText = (TextBox)FindName("DeviceText");
            var DeviceApi = (TextBox)FindName("DeviceApi");

            var uid = (Label)FindName("uid");
            var id = ((Image)sender).Uid;

            uid.Content = id;
            if ( id != "")
            {
                nameBox.Text = this.store[id].name;
                DeviceSize.Text = this.store[id].size;
                DeviceFont.Text = this.store[id].font;
                DeviceColor.Text = this.store[id].color;
                DeviceText.Text = this.store[id].text;
                if (this.store[id].type == "label"|| this.store[id].type == "button")
                {
                    if (this.store[id].type == "button")
                        DeviceApi.Visibility = System.Windows.Visibility.Visible;
                    DeviceSize.Visibility = System.Windows.Visibility.Visible;
                    DeviceFont.Visibility = System.Windows.Visibility.Visible;
                    DeviceColor.Visibility = System.Windows.Visibility.Visible;
                    DeviceText.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    DeviceFont.Visibility = System.Windows.Visibility.Hidden;
                    DeviceSize.Visibility = System.Windows.Visibility.Hidden;
                    DeviceColor.Visibility = System.Windows.Visibility.Hidden;
                    DeviceText.Visibility = System.Windows.Visibility.Hidden;
                    DeviceApi.Visibility = System.Windows.Visibility.Hidden;


                }


            }

        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is Image image)
            {
                Canvas? canvas = image.Parent as Canvas;

                if (canvas != null)
                {
                    double newX = e.GetPosition(canvas).X - (image.Width / 2);
                    double newY = e.GetPosition(canvas).Y - (image.Height / 2);

                    Canvas.SetLeft(image, newX);
                    Canvas.SetTop(image, newY);
                }
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectedImage = (Image)sender;
            selectedImage.CaptureMouse();
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedImage != null)
            {
                store[((Image)sender).Uid].posX= Canvas.GetLeft(((Image)sender));
                store[((Image)sender).Uid].posY = Canvas.GetTop(((Image)sender));
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
                brush.Stretch = Stretch.Uniform;
                canvas.Background = brush;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            List<imgProp> imageDataList = new List<imgProp>();

            foreach (var element in store)
            {
                if (element.Value.name != "")
                {
                    element.Value.posX = element.Value.posX + storeImage[(string)element.Key].Width/2;
                    element.Value.posY = element.Value.posY + storeImage[(string)element.Key].Height / 2;
                    imageDataList.Add(element.Value);

                }
            }

            string json = JsonConvert.SerializeObject(imageDataList, Formatting.Indented);
            json=string.Concat("{\"value\":\n",json, "\n}");

            SaveFileDialog saveFileDialog = new SaveFileDialog();

            // Set the initial directory and file name (optional)
            saveFileDialog.InitialDirectory = @"C:\";
            saveFileDialog.FileName = "export_data.json";

            // Set the file filter (optional)
            saveFileDialog.Filter = "Text Files (*.json)|*.json|All Files (*.*)|*.*";

            // Show the dialog and capture the result
            Nullable<bool> result = saveFileDialog.ShowDialog();

            // Check if the user clicked the "Save" button
            if (result == true)
            {
                // Get the selected file name and save your data
                string selectedFilePath = saveFileDialog.FileName;
                File.WriteAllText(selectedFilePath, json);


                Console.WriteLine("File saved at: " + selectedFilePath);
                MessageBox.Show("Export completed.");
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var cursorPosition = e.GetPosition(canvas);
            
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            var nameBox = FindName("DeviceName") as TextBox;
            var uid = FindName("uid") as Label;
            if (nameBox != null && uid != null)
            {
                string? c = uid.Content as string;
                if (c != null) 
                    this.store[c].name=nameBox.Text;
            }
        }

        private void deleteImage_Click(object sender, RoutedEventArgs e)
        {
            var uid = FindName("uid") as Label;
            if (uid != null && (uid.Content as string) != "")
            {
                String S = (string)uid.Content;
                canvas.Children.Remove(storeImage[S]);
                storeImage.Remove(S);
                store.Remove(S);

            }



        }

        private void DeviceColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            var colorBox = (TextBox)FindName("DeviceColor") ;
            var uid =(Label) FindName("uid")  ;
            if (colorBox != null && uid != null)
            {
                string? c = uid.Content as string;
                if (c != null)
                    this.store[c].color = colorBox.Text;
            }
        }

        private void DeviceFont_TextChanged(object sender, TextChangedEventArgs e)
        {
            var DeviceFont = (TextBox)FindName("DeviceFont");
            var uid = (Label)FindName("uid");
            if (DeviceFont != null && uid != null)
            {
                string? c = uid.Content as string;
                if (c != null)
                    this.store[c].font = DeviceFont.Text;
            }
        }

        private void DeviceSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            var DeviceSize = (TextBox)FindName("DeviceSize");
            var uid = (Label)FindName("uid");
            if (DeviceSize != null && uid != null)
            {
                string? c = uid.Content as string;
                if (c != null && DeviceSize.Text!="")
                    this.store[c].size = DeviceSize.Text;
            }
        }

        private void DeviceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var DeviceText = (TextBox)FindName("DeviceText");
            var uid = (Label)FindName("uid");
            if (DeviceText != null && uid != null)
            {
                string? c = uid.Content as string;
                if (c != null && DeviceText.Text != "")
                    this.store[c].text = DeviceText.Text;
            }
        }

        private void DeviceApi_TextChanged(object sender, TextChangedEventArgs e)
        {
            var DeviceApi = (TextBox)FindName("DeviceApi");
            var uid = (Label)FindName("uid");
            if (DeviceApi != null && uid != null)
            {
                string? c = uid.Content as string;
                if (c != null && DeviceApi.Text != "")
                    this.store[c].api = DeviceApi.Text;
            }
        }
    }

    public class ImageViewModel
    {
        public string ImageSource { get; set; }


        public ImageViewModel(string imageSource)
        {
            ImageSource = imageSource;


        }
    }

    public class ImageData
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public  string? name { get; set; }
        
    }

    public  class imgProp
    {
        public string name { get; set; }
        public string type { get; set; }
        public double posX { get; set; }
        public double posY { get; set; }
        public string font { get; set; }
        public string color{ get; set; }
        public string size { get; set; }

        public string text { get; set; }
        public string api { get; set; }




        public imgProp( string name, string deviceType, string uid)
        {
            this.type = deviceType;
            this.name = name;
            
        }

    }


}