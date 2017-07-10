using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Paint.Extensions
{
    static class BitmapSizeExtensions
    {
        public static Vector2 ToVector2(this BitmapSize size)
        {
            return new Vector2(size.Width, size.Height);
        }
    }
}
