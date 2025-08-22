using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace MapEditor2
{
    internal class Tile
    {
        public int ID { get; set; }
        public StackPanel Panel { get; set; }
        public BitmapImage BitImage { get; set; }
        public string ImagePath { get; set; }
        private Action<int> changeSelectedIdEvent;
        private Action<int, int> changeIdEvent; 
        private TextBox textBox;

        public Tile(int id, string path, Action<int> changeSelectedId, Action<int,int> changeId)
        {
            ID = id;
            ImagePath = path;
            changeSelectedIdEvent = changeSelectedId;
            changeIdEvent = changeId;
            Panel = new StackPanel();
            Panel.Margin = new Thickness(5);
            Image img = new Image();
            img.Width = 32;
            img.Height = 32;
            BitImage = new BitmapImage(new Uri(path));
            img.Source = BitImage;

            Panel.Children.Add(img);
            textBox = new TextBox();
            textBox.Text = id.ToString();
            textBox.FontSize = 9;
            textBox.FontWeight = FontWeights.Bold;
            textBox.HorizontalAlignment = HorizontalAlignment.Center;
            textBox.Background = new SolidColorBrush(Color.FromArgb(255, 21, 21, 21));
            textBox.BorderBrush = null;
            textBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 214, 214, 214));
            textBox.Width = 32;
            textBox.TextAlignment = TextAlignment.Center;
            textBox.TextChanged += (sender, e) =>
            {
                if (int.TryParse(textBox.Text, out int newId) && newId < 20)
                {
                    changeIdEvent.Invoke(ID, newId);
                    ID = newId;
                }
                else
                {
                    textBox.Text = ID.ToString();
                }
            };
            Panel.Children.Add(textBox);
            Panel.MouseDown += (sender, ee) => {
                changeSelectedIdEvent.Invoke(ID);
            };
        }
    }
}
