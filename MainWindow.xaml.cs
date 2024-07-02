using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapEditor2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int sizeX = 34;
        private static int sizeY = 20;
        private static int grid = 16;

        private Dictionary<int, BitmapImage> sprites = new Dictionary<int, BitmapImage>();
        private Dictionary<TextBox, int> textBoxSprites = new Dictionary<TextBox, int>();
        private BitmapImage[] backgroundTiles = { new BitmapImage(new Uri(@"sprites\block03-1.png", UriKind.Relative)),
                                                  new BitmapImage(new Uri(@"sprites\block03-2.png", UriKind.Relative)),
                                                  new BitmapImage(new Uri(@"sprites\block03-3.png", UriKind.Relative)),                        
                                                  new BitmapImage(new Uri(@"sprites\block03-4.png", UriKind.Relative))};
        private int[,] map = new int[sizeY, sizeX];
        private Image[,] images;

        private int selectedId = 0;
        private bool mouseDown = false;

        private bool showOutput = false;
        public MainWindow()
        {
            InitializeComponent();
            images = new Image[sizeY, sizeX];
            for (int i = 0; i < sizeY; i++)
            {
                for (int j = 0; j < sizeX; j++)
                {
                    Image img = new Image();
                    img.Source = backgroundTiles[j % 2 + (i % 2) * 2];
                    img.Width = grid;
                    img.Height = grid;
                    Canvas.SetTop(img, i * grid);
                    Canvas.SetLeft(img, j * grid);
                    images[i, j] = img;
                    myCanvas.Children.Add(images[i,j]);
                }
            }
        }

        private void UpdateCanvas()
        {
            for (int i = 0; i < sizeY; i++)
            {
                for (int j = 0; j < sizeX; j++)
                {
                    if (map[i,j] == 0)
                    {
                        images[i, j].Source = backgroundTiles[j % 2 + (i % 2) * 2];
                        continue;
                    }
                    images[i, j].Source = sprites[map[i, j]];
                }
            }
        }

        private void UpdateCanvas(int posX, int posY)
        {
            if (map[posY, posX] == 0)
            {
                images[posY, posX].Source = backgroundTiles[posX % 2 + (posY % 2) * 2];
                return;
            }
            images[posY, posX].Source = sprites[map[posY, posX]];
        }

        private void UpdateOutput()
        {
            output.Text = "";
            output.Text += '[';
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < sizeY; i++)
            {
                stringBuilder.Append('[');
                for (int j = 0; j < sizeX; j++)
                {
                    stringBuilder.Append(map[i, j].ToString() + ',');
                    if (j == sizeX - 1) stringBuilder.Remove(stringBuilder.Length - 1, 1);
                }
                stringBuilder.Append("],");
                if (i == sizeY - 1) stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }
            output.Text += stringBuilder.ToString();
            output.Text += ']';
        }

        private void myCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            int posX = (int)(e.GetPosition(myCanvas).X / grid);
            int posY = (int)(e.GetPosition(myCanvas).Y / grid);

            map[posY, posX] = selectedId;
            UpdateCanvas(posX, posY);
        }

        private void myCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            int posX = (int)(e.GetPosition(myCanvas).X / grid);
            int posY = (int)(e.GetPosition(myCanvas).Y / grid);
            point.Content = $"{posX} x {posY}";
            if (!mouseDown) return;
            if (posX >= sizeX || posY >= sizeY) return;
            map[posY, posX] = selectedId;
            UpdateCanvas(posX, posY);
        }

        private void myCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseDown = false;
            UpdateOutput();
        }

        private void clearAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите очистить всю область? P.S Кнопку назад я ещё не реализовал",
                "Очистить всю область",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes) 
            {
                map = new int[sizeY, sizeX];
                UpdateCanvas();
            }
        }

        private void erasing_Click(object sender, RoutedEventArgs e)
        {
            selectedId = 0;
        }

        private void addSprite_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog { Filter = "image files (*.png) | *.png" };

            if (true == openDlg.ShowDialog())
            {
                // Наполнить StrokeCollection из файла
                using (FileStream fs = new FileStream(openDlg.FileName, FileMode.Open, FileAccess.Read))
                {
                    StackPanel sp = new StackPanel();
                    sp.Margin = new Thickness(5);
                    Image img = new Image();
                    img.Width = 32;
                    img.Height = 32;
                    BitmapImage bi = new BitmapImage(new Uri(fs.Name));
                    img.Source = bi;
                    int id = (sprites.Count + 1);
                    sprites.Add(id, bi);
                    
                    sp.Children.Add(img);
                    TextBox tb = new TextBox();
                    tb.Text = (sprites.Count).ToString();
                    tb.FontSize = 9;
                    tb.FontWeight = FontWeights.Bold;
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.Background = new SolidColorBrush(Color.FromArgb(255,21,21,21));
                    tb.BorderBrush = null;
                    tb.Foreground = new SolidColorBrush(Color.FromArgb(255, 214, 214, 214));
                    tb.Width = 32;
                    tb.TextAlignment = TextAlignment.Center;
                    tb.TextChanged += (sender, e) =>
                    {
                        if (int.TryParse(tb.Text, out int num))
                        {

                        }
                    };
                    textBoxSprites.Add(tb, id);
                    sp.Children.Add(tb);
                    sp.Name = 'i' + id.ToString();
                    sp.MouseDown += (panel, ee) => {
                        string id = ((StackPanel)panel).Name.Trim('i');
                        selectedId = int.Parse(id);
                    };
                    UIElement b = itemsPanel.Children[itemsPanel.Children.Count-1];
                    itemsPanel.Children.Remove(b);
                    itemsPanel.Children.Add(sp);
                    itemsPanel.Children.Add(b);
                    selectedId = id;
                }
            }
        }

        private void copyMap_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(output.Text);
        }

        private void showOutputButton_Click(object sender, RoutedEventArgs e)
        {
            showOutput = !showOutput;
            if(showOutput)
            {
                showOutputButton.Content = "Скрыть вывод";
                outputRow.MinHeight = 30;
                outputRow.Height = new GridLength(50, GridUnitType.Pixel);
            }
            else
            {
                outputRow.MinHeight = 0;
                outputRow.Height = new GridLength(0, GridUnitType.Pixel);
                showOutputButton.Content = "Показать вывод";
            }
            
        }
    }
}
