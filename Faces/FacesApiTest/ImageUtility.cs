using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacesApiTest
{
    public class ImageUtility
    {
        public byte[] ConvertToByte(string imagePath) 
        {
            MemoryStream ms = new MemoryStream();
            using (FileStream fs = new FileStream(imagePath, FileMode.Open)) 
            {
                fs.CopyTo(ms);
            }

            return ms.ToArray();
        }

        public void FromByteToImage(byte[] imageBytes,string fileName) 
        {
            using (MemoryStream ms = new MemoryStream(imageBytes)) 
            {
                Image img = Image.FromStream(ms);

                img.Save($"{fileName}.jpg", ImageFormat.Jpeg);
            }
        }
    }
}
