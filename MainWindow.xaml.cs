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
        private int brushSizeValue = 1;

        private Dictionary<int, Tile> tiles = new Dictionary<int, Tile>();
        private BitmapImage[] backgroundTiles = { new BitmapImage(new Uri(@"sprites\block03-1.png", UriKind.Relative)),
                                                  new BitmapImage(new Uri(@"sprites\block03-2.png", UriKind.Relative)),
                                                  new BitmapImage(new Uri(@"sprites\block03-3.png", UriKind.Relative)),                        
                                                  new BitmapImage(new Uri(@"sprites\block03-4.png", UriKind.Relative))};
        private int[,] map = new int[sizeY, sizeX];
        private Image[,] images;
        private int selectedId = 0;
        private bool mouseDown = false;
        private bool showOutput = false;
        private Rectangle selectionRectangle;
        public MainWindow()
        {
            InitializeComponent();
            selectionRectangle = new Rectangle();
            selectionRectangle.Width = grid;
            selectionRectangle.Height = grid;
            selectionRectangle.Stroke = Brushes.Gray;
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
            myCanvas.Children.Add(selectionRectangle);
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
                    images[i, j].Source = tiles[map[i, j]].BitImage;
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
            images[posY, posX].Source = tiles[map[posY, posX]].BitImage;
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

        private void Painting(int posX, int posY)
        {
            
            int i = posY - (int)Math.Floor(((float)brushSizeValue / 2));
            for (; i < posY + brushSizeValue - (int)Math.Floor(((float)brushSizeValue / 2)); i++)
            {
                int j = posX - (int)Math.Floor(((float)brushSizeValue / 2));
                for (; j < posX + brushSizeValue - (int)Math.Floor(((float)brushSizeValue / 2)); j++)
                {
                    if (j >= sizeX || i >= sizeY || j < 0 || i < 0) continue;
                    map[i, j] = selectedId;
                    UpdateCanvas(j, i);
                }
            }
        }

        private void myCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            int posX = (int)(e.GetPosition(myCanvas).X / grid);
            int posY = (int)(e.GetPosition(myCanvas).Y / grid);
            Painting(posX, posY);
        }

        private void myCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            int posX = (int)(e.GetPosition(myCanvas).X / grid);
            int posY = (int)(e.GetPosition(myCanvas).Y / grid);
            point.Content = $"{posX} x {posY}";

            Canvas.SetTop(selectionRectangle, posY * grid);
            Canvas.SetLeft(selectionRectangle, posX * grid);

            if (!mouseDown) return;
            if (posX >= sizeX || posY >= sizeY)
            {
                mouseDown = false;
            }
            Painting(posX, posY);
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
                    int id = tiles.Count == 0 ? 1 : tiles[tiles.Keys.Last()].ID + 1;
                    tiles.Add(id, new Tile(id,
                                           fs.Name,
                                           ChangeSelectedID,
                                           (int oldId, int newId) =>
                                           {
                                               Tile t = tiles[oldId];
                                               tiles.Remove(oldId);
                                               tiles.Add(newId, t);
                                               selectedId = newId;
                                               ReplaсeTile(oldId, newId);
                                           }));
                    UIElement b = itemsPanel.Children[itemsPanel.Children.Count - 1];
                    itemsPanel.Children.Remove(b);
                    itemsPanel.Children.Add(tiles[id].Panel);
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

        private void ReplaсeTile(int currentId, int targetId)
        {
            for (int i = 0; i < sizeY; i++)
            {
                for (int j = 0; j < sizeX; j++)
                {
                    if (map[i, j] == currentId)
                    {
                        map[i, j] = targetId;
                    }
                }
            }
            UpdateOutput();
        }

        private void ChangeSelectedID(int id)
        {
            selectedId = id;
        }

        private void brushSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            brushSizeValue = (int)brushSize.Value;
        }
    }
}
