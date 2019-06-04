using System;
using System.Numerics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Paint.Controls
{
    public sealed partial class SizeEntry : UserControl
    {
        public static DependencyProperty SizeInputProperty = DependencyProperty.Register(nameof(SizeInput), typeof(Vector2), typeof(SizeEntry), new PropertyMetadata((object)new Vector2(300, 300)));

        public SizeEntry()
        {
            this.InitializeComponent();
        }

        public Vector2 SizeInput
        {
            get { return (Vector2)GetValue(SizeInputProperty); }
            private set { SetValue(SizeInputProperty, value); }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var min = 1;
            SizeInput = new Vector2(GetTextBoxValue(WidthTextBox, min), GetTextBoxValue(HeightTextBox, min));
        }

        private int GetTextBoxValue(TextBox textBox, int value)
        {
            var result = value;

            if (!string.IsNullOrEmpty(textBox.Text))
            {
                result = Math.Max(int.Parse(textBox.Text), result);
            }

            return result;
        }
    }
}
