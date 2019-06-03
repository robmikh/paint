using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace Paint.Core
{
    class Image
    {
        public StorageFile File { get; private set; }
        public Size Size { get; set; }

        public Image(StorageFile file, Size size)
        {
            File = file;
            Size = size;
        }

        public void AssignNewFile(StorageFile file)
        {
            File = file;
        }
    }
}
