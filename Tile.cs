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
        public Border Wrapper { get; set; }
        public BitmapImage BitImage { get; set; }
        public string ImagePath { get; set; }
        private StackPanel panel { get; set; }
        private Action<int> changeSelectedIdEvent;
        private Action<int, int> changeIdEvent; 
        private TextBox textBox;
        private SolidColorBrush borderBrush = new SolidColorBrush(Color.FromArgb(255, 61, 61, 61));
        private SolidColorBrush backgroundBrush = new SolidColorBrush(Color.FromArgb(255, 36, 36, 36));

        public Tile(int id, string path, Action<int> changeSelectedId, Action<int,int> changeId)
        {
            ID = id;
            ImagePath = path;
            changeSelectedIdEvent = changeSelectedId;
            changeIdEvent = changeId;

            Wrapper = new Border();
            Wrapper.BorderThickness = new Thickness(1);
            Wrapper.BorderBrush = null;
            Wrapper.Margin = new Thickness(5);
            Wrapper.Padding = new Thickness(5);

            panel = new StackPanel();
            Image img = new Image();
            img.Width = 32;
            img.Height = 32;
            BitImage = new BitmapImage(new Uri(path));
            img.Source = BitImage;

            panel.Children.Add(img);
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
            panel.Children.Add(textBox);
            Wrapper.Child = panel;
            Wrapper.MouseDown += (sender, ee) => {
                changeSelectedIdEvent.Invoke(ID);
            };
        }

        public void Select()
        {
            Wrapper.BorderBrush = borderBrush;
            Wrapper.Background = backgroundBrush;
        }

        public void Deselect()
        {
            Wrapper.BorderBrush = null;
            Wrapper.Background = null;
        }
    }
}
