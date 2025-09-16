using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    /// Нужно selectedId ввиде свойства и при записи прописать выделение выбранных тайлов
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int sizeX = 0;
        private static int sizeY = 0;
        private static int grid = 16;
        private int brushSizeValue = 1;

        // Тайлы
        private Dictionary<int, Tile> tiles = new Dictionary<int, Tile>();
        private Action<int, int> tileChangeId;


        private BitmapImage[] backgroundTiles = { new BitmapImage(new Uri(@"sprites\block03-1.png", UriKind.Relative)),
                                                  new BitmapImage(new Uri(@"sprites\block03-2.png", UriKind.Relative)),
                                                  new BitmapImage(new Uri(@"sprites\block03-3.png", UriKind.Relative)),                        
                                                  new BitmapImage(new Uri(@"sprites\block03-4.png", UriKind.Relative))};
        private BitmapImage emptyTile = new BitmapImage(new Uri(@"sprites\empty.jpg", UriKind.Relative));
        private int[,] map;
        private Image[,] images;
        private int selectedId = 0;
        public int SelectedID
        {
            get { return selectedId; }
            set { 
                selectedId = value;
                foreach (var item in tiles.Values)
                {
                    item.Deselect();
                }
                if (selectedId == 0) return;
                tiles[selectedId].Select();
            }
        }
        private bool mouseDown = false;
        private bool showOutput = false;
        private System.Windows.Shapes.Rectangle selectionRectangle = new System.Windows.Shapes.Rectangle();

        private enum Tools { Pencil = 0, Move = 1, Select = 2}

        private Tools currentTool = Tools.Pencil;
        private Tools CurrentTool
        {
            get { return currentTool; }
            set 
            { 
                currentTool = value;
                switch (currentTool) { 
                    case Tools.Pencil:
                        this.Cursor = Cursors.Arrow;
                        break;
                    case Tools.Move:
                        this.Cursor = Cursors.SizeAll;
                        break;
                }
            }
        }
        private Point startMouseMove;

        public MainWindow()
        {
            InitializeComponent();
            sizeX = int.Parse(sizeXTextBox.Text);
            sizeY = int.Parse(sizeYTextBox.Text);
            myCanvas.Width = sizeX * grid;
            myCanvas.Height = sizeY * grid;

            // 
            selectionRectangle.Width = grid;
            selectionRectangle.Height = grid;
            selectionRectangle.Stroke = Brushes.Gray;
            // Устанавливаем точку привязки в правый нижний угол (1,1)
            selectionRectangle.RenderTransformOrigin = new System.Windows.Point(1, 1);

            Canvas.SetZIndex(selectionRectangle, 1);
            map = new int[sizeY, sizeX];
            images = new Image[sizeY, sizeX];
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    Image img = new Image();
                    img.Source = backgroundTiles[x % 2 + (y % 2) * 2];
                    img.Width = grid;
                    img.Height = grid;
                    Canvas.SetTop(img, y * grid);
                    Canvas.SetLeft(img, x * grid);
                    images[y, x] = img;
                    myCanvas.Children.Add(images[y,x]);
                }
            }
            myCanvas.Children.Add(selectionRectangle);

            tileChangeId = (int oldId, int newId) => // Делегат, который вызывается при изменении ID у тайла
            {
                if (tiles.ContainsKey(newId))  // Если ID уже занято
                {
                    tiles[oldId].SetIDForTextBox(oldId.ToString()); // Возврощаем текстбоксу прежний ID
                    return;
                };
                Tile t = tiles[oldId];   // записываем сами себя (с точки зрения тайла)
                tiles.Remove(oldId);     // удаляем себя из словаря
                tiles.Add(newId, t);     // добавляем уже с новым ID
                SelectedID = newId;      // выбираем этот измененный ID
                ReplaсeTile(oldId, newId);// проходимся по всей карте и заменяем старый ID на новый
                t.ID = newId;
                UpdateCanvas();
            };
        }

        private void UpdateCanvas()
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    if (map[y,x] == 0)
                    {
                        images[y, x].Source = backgroundTiles[x % 2 + (y % 2) * 2];
                        continue;
                    }
                    // Если тайл с нужным ID существует, то отрисовываем его   // Если нет, то заливаем пурпурным цветом
                    images[y, x].Source = tiles.Keys.Contains(map[y, x]) ? tiles[map[y, x]].BitImage : emptyTile;
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
            for (int y = 0; y < sizeY; y++)
            {
                stringBuilder.Append('[');
                for (int x = 0; x < sizeX; x++)
                {
                    stringBuilder.Append(map[y, x].ToString() + ',');
                    if (x == sizeX - 1) stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    if (x % 2 == 1) stringBuilder.Append("  ");
                }
                stringBuilder.Append("],\n");
                if (y == sizeY - 1) stringBuilder.Remove(stringBuilder.Length - 2, 1);
                if (y % 2 == 1) stringBuilder.Append('\n');
            }
            output.Text += stringBuilder.ToString();
            output.Text += ']';
        }

        private void Painting(int posX, int posY)
        {
            if (posX >= sizeX || posY >= sizeY) { return; }
            
            int y = posY - (int)Math.Floor(((float)brushSizeValue / 2));
            for (; y < posY + brushSizeValue - (int)Math.Floor(((float)brushSizeValue / 2)); y++)
            {
                int x = posX - (int)Math.Floor(((float)brushSizeValue / 2));
                for (; x < posX + brushSizeValue - (int)Math.Floor(((float)brushSizeValue / 2)); x++)
                {
                    if (x >= sizeX || y >= sizeY || x < 0 || y < 0) continue;
                    map[y, x] = SelectedID;
                    UpdateCanvas(x, y);
                }
            }
        }

        private void CanvasWrapper_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            int posX = (int)(e.GetPosition(myCanvas).X / grid);
            int posY = (int)(e.GetPosition(myCanvas).Y / grid);

            if (CurrentTool == Tools.Pencil)
            {
                Painting(posX, posY);
            }
            else if (CurrentTool == Tools.Move) {
                startMouseMove = new Point(posX, posY);
            }
            
        }

        private void CanvasWrapper_MouseMove(object sender, MouseEventArgs e)
        {
            int posX = (int)(e.GetPosition(myCanvas).X / grid);
            int posY = (int)(e.GetPosition(myCanvas).Y / grid);
            point.Content = $"{posX} x {posY}";

            if (CurrentTool == Tools.Pencil)
            {
                Canvas.SetTop(selectionRectangle, posY * grid);
                Canvas.SetLeft(selectionRectangle, posX * grid);
                if (!mouseDown) return;
                Painting(posX, posY);
            }
            else if (CurrentTool == Tools.Move) 
            {
                if (!mouseDown) return;
                int dirX = posX - startMouseMove.X;
                int dirY = posY - startMouseMove.Y;
                if (dirX < 0) // Смещение влево
                {
                    for (int y = 0; y < sizeY; y++)
                    {
                        for (int x = 0; x < sizeX; x++)
                        {
                            if (map[y, x] != 0)
                            {
                                if (x + dirX < 0)
                                {
                                    map[y, x] = 0;
                                    continue;
                                }
                                map[y, x + dirX] = map[y, x];
                                map[y, x] = 0;
                            }
                        }
                    }
                }
                else if (dirX > 0) // Смещение вправо
                {
                    for (int y = 0; y < sizeY; y++)
                    {
                        for (int x = sizeX-1; x >= 0; x--)
                        {
                            if (map[y, x] != 0)
                            {
                                if (x + dirX >= sizeX)
                                {
                                    map[y, x] = 0;
                                    continue;
                                }
                                map[y, x + dirX] = map[y, x];
                                map[y, x] = 0;
                            }
                        }
                    }
                }
                if (dirY < 0) // Смещение вверх
                {
                    for (int y = 0; y < sizeY; y++)
                    {
                        for (int x = 0; x < sizeX; x++)
                        {
                            if (map[y, x] != 0)
                            {
                                if (y + dirY < 0)
                                {
                                    map[y, x] = 0;
                                    continue;
                                }
                                map[y + dirY, x] = map[y, x];
                                map[y, x] = 0;
                            }
                        }
                    }
                }
                else if (dirY > 0) // Смещение вниз
                {
                    for (int y = sizeY - 1; y >= 0; y--)
                    {
                        for (int x = 0; x < sizeX; x++)
                        {
                            if (map[y, x] != 0)
                            {
                                if (y + dirY >= sizeY)
                                {
                                    map[y, x] = 0;
                                    continue;
                                }
                                map[y + dirY, x] = map[y, x];
                                map[y, x] = 0;
                            }
                        }
                    }
                }
                startMouseMove.X = posX; 
                startMouseMove.Y = posY;
                UpdateCanvas();
            }
        }

        private void CanvasWrapper_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseDown = false;
            UpdateOutput();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
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

        private void Erasing_Click(object sender, RoutedEventArgs e)
        {
            SelectedID = 0;
            CurrentTool = Tools.Pencil;
        }
        private void PencilTool_Click(object sender, RoutedEventArgs e)
        {
            CurrentTool = Tools.Pencil;
        }

        private void MoveTool_Click(object sender, RoutedEventArgs e)
        {
            CurrentTool = Tools.Move;
        }

        private void AddTile_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog { Filter = "image files (*.png;*.jpg) | *.png;*.jpg" };

            if (true == openDlg.ShowDialog())
            {
                // Наполнить StrokeCollection из файла
                using (FileStream fs = new FileStream(openDlg.FileName, FileMode.Open, FileAccess.Read))
                {
                    int id = tiles.Count == 0 ? 1 : tiles[tiles.Keys.Last()].ID + 1;
                    tiles.Add(id, new Tile(id, fs.Name, ChangeSelectedID, tileChangeId));
                    
                    UIElement b = itemsPanel.Children[itemsPanel.Children.Count - 1];
                    itemsPanel.Children.Remove(b);
                    itemsPanel.Children.Add(tiles[id].Wrapper);
                    itemsPanel.Children.Add(b);
                    SelectedID = id;
                    UpdateCanvas();
                }
            }
        }

        private void CopyMap_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(output.Text);
        }

        private void ShowOutputButton_Click(object sender, RoutedEventArgs e)
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
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    if (map[y, x] == currentId)
                    {
                        map[y, x] = targetId;
                    }
                }
            }
            UpdateOutput();
        }

        private void ChangeSelectedID(int id)
        {
            SelectedID = id;
            CurrentTool = Tools.Pencil;
        }

        private void BrushSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            brushSizeValue = (int)brushSize.Value;
            TranslateTransform translateTransform = new TranslateTransform();
            translateTransform.X = -brushSizeValue * grid + grid;
            translateTransform.Y = -brushSizeValue * grid + grid;
            selectionRectangle.RenderTransform = translateTransform;
            selectionRectangle.Width = brushSizeValue * grid;
            selectionRectangle.Height = brushSizeValue * grid;

        }

        private void SizeYTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out _)) {
                e.Handled = true;
            }
        }

        private void SizeXTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
            }
        }

        private void AcceptResize_Click(object sender, RoutedEventArgs e)
        {
            if (sizeYTextBox.Text.Length == 0 || sizeXTextBox.Text.Length == 0) { return; }
            int oldSizeX = sizeX;
            int oldSizeY = sizeY;
            sizeY = int.Parse(sizeYTextBox.Text);
            sizeX = int.Parse(sizeXTextBox.Text);
            int[,] newMap = new int[sizeY, sizeX];
            Image[,] newImages = new Image[sizeY, sizeX];

            // Новая ось больше старой? Если да то перечисляем её
            for (int y = 0; y < (sizeY > oldSizeY ? sizeY : oldSizeY); y++)
            {
                // Новая ось больше старой? Если да то перечисляем её
                for (int x = 0; x < (sizeX > oldSizeX ? sizeX : oldSizeX); x++)
                {

                    if (x < sizeX && y < sizeY)
                    {
                        if (x < oldSizeX && y < oldSizeY)
                        {
                            newImages[y, x] = images[y, x];
                            newMap[y, x] = map[y, x];
                        }

                        if (newImages[y, x] == null)
                        {
                            Image img = new Image();
                            img.Source = backgroundTiles[x % 2 + (y % 2) * 2];
                            img.Width = grid;
                            img.Height = grid;
                            Canvas.SetTop(img, y * grid);
                            Canvas.SetLeft(img, x * grid);
                            newImages[y, x] = img;
                            myCanvas.Children.Add(newImages[y, x]);
                        }
                    }// Если новая карта меньше старой, то удаляем более неиспользуемые картинки
                    else if (x < oldSizeX && y < oldSizeY) 
                    {
                        myCanvas.Children.Remove(images[y, x]);
                    }
                }
            }
            map = newMap;
            images = newImages;
            myCanvas.Width = sizeX * grid;
            myCanvas.Height = sizeY * grid;
        }

        private void ExportTiles_Click(object sender, RoutedEventArgs e)
        {
            if (tiles.Values.Count == 0) return;
            var saveDlg = new SaveFileDialog { Filter = "image files (*.tl) | *.tl" };
            if (true == saveDlg.ShowDialog()) {
                var data = new List<object>();
                foreach (var t in tiles.Values)
                {
                    string nameImage = t.ImagePath.Split('\\')[t.ImagePath.Split('\\').Length - 1];
                    string newImagePath = $"{saveDlg.FileName.Replace(saveDlg.FileName.Split('\\')[saveDlg.FileName.Split('\\').Length - 1], "")}{nameImage}"; // Получаем путь к json файлу без его имени + имя изображения

                    if (t.ImagePath != newImagePath)
                    {
                        File.Copy(t.ImagePath, newImagePath); // Обработать already exist
                    }
                    data.Add(new { t.ID, ImagePath = nameImage });       // Имя берётся аналогичное t.ID
                }
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(saveDlg.FileName, json);
            }
        }

        private void ImportTiles_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog { Filter = "image files (*.tl) | *.tl" };

            if (true == openDlg.ShowDialog())
            {
                tiles.Clear(); // Очищаем все тайлы перед импортом
                string json = File.ReadAllText(openDlg.FileName);
                List<JsonElement> jsonElements = JsonSerializer.Deserialize<List<JsonElement>>(json);
                foreach (var item in jsonElements)
                {
                    int id = item.GetProperty("id").GetInt32();
                    string imagePath = $"{openDlg.FileName.Replace(openDlg.SafeFileName, "")}{item.GetProperty("imagePath").GetString()}";
                    tiles.Add(id, new Tile(id, imagePath, ChangeSelectedID, tileChangeId));

                    UIElement b = itemsPanel.Children[itemsPanel.Children.Count - 1];
                    itemsPanel.Children.Remove(b);
                    itemsPanel.Children.Add(tiles[id].Wrapper);
                    itemsPanel.Children.Add(b);
                    SelectedID = id;
                }
                UpdateCanvas();
            }
        }

        private void ImportMap_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog { Filter = "image files (*.mp) | *.mp" };

            if (true == openDlg.ShowDialog())
            {
                //tiles.Clear(); // Очищаем все тайлы перед импортом
                string json = File.ReadAllText(openDlg.FileName);
                List<List<int>> arrays = JsonSerializer.Deserialize<List<List<int>>>(json);

                sizeY = arrays.Count;
                sizeX = arrays[0].Count;
                sizeYTextBox.Text = sizeY.ToString();
                sizeXTextBox.Text = sizeX.ToString();
                map = new int[sizeY, sizeX];
                images = new Image[sizeY, sizeX];
                myCanvas.Children.Clear();
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        map[y, x] = arrays[y][x];
                        Image img = new Image();
                        img.Source = backgroundTiles[x % 2 + (y % 2) * 2];
                        img.Width = grid;
                        img.Height = grid;
                        Canvas.SetTop(img, y * grid);
                        Canvas.SetLeft(img, x * grid);
                        images[y, x] = img;
                        myCanvas.Children.Add(images[y, x]);
                    }
                }
                myCanvas.Width = sizeX * grid;
                myCanvas.Height = sizeY * grid;
                UpdateCanvas();
            }
        }
    }
}
