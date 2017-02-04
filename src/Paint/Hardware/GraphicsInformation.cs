using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;

namespace Paint.Hardware
{
    static class GraphicsInformation
    {
        private static float _dpi = -1;
        public static float Dpi
        {
            get
            {
                if (_dpi < 0)
                {
                    var displayInfo = DisplayInformation.GetForCurrentView();
                    _dpi = displayInfo.LogicalDpi;
                    displayInfo.DpiChanged += DisplayInfo_DpiChanged;
                }

                return _dpi;
            }
        }

        private static void DisplayInfo_DpiChanged(DisplayInformation sender, object args)
        {
            _dpi = sender.LogicalDpi;
        }
    }
}
