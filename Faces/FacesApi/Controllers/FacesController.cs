using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FacesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacesController : ControllerBase
    {
        private AzureFaceConfiguration config;
        public FacesController(AzureFaceConfiguration config)
        {
            this.config = config;
        }

        [HttpGet]
        public async Task<List<byte[]>> ReadFaces() 
        {
            List<byte[]> faceCropped = null;

            using (var ms = new MemoryStream(2048)) 
            {
                await Request.Body.CopyToAsync(ms);

                var bytes = ms.ToArray();

                Image img = Image.Load(bytes);
                img.Save("dummy.jpg");

                faceCropped = await UploadAndDetectFaces(img, new MemoryStream(bytes));
            }

            return faceCropped;
        }

        [HttpPost]
        public async Task<Tuple<List<byte[]>,Guid>> ReadFaces(Guid orderId)
        {
            List<byte[]> faceCropped = null;

            using (var ms = new MemoryStream(2048))
            {
                await Request.Body.CopyToAsync(ms);

                var bytes = ms.ToArray();

                Image img = Image.Load(bytes);
                img.Save("dummy.jpg");

                faceCropped = await UploadAndDetectFaces(img,new MemoryStream(bytes));
            }

            return new Tuple<List<byte[]>, Guid>(faceCropped, orderId);
        }

        private async Task<List<byte[]>> UploadAndDetectFaces(Image img, MemoryStream memoryStream)
        {
            var subKey = this.config.AzureSubscriptionKey;
            var endPoint = this.config.AzureEndPoint;

            IFaceClient client = Authenticate(endPoint, subKey);
            var faceList = new List<byte[]>();
            IList<DetectedFace> faces = null;

            try 
            {
                faces = await client.Face.DetectWithStreamAsync(memoryStream,true,false,null);

                int j = 0;
                foreach (var face in faces) 
                {
                    var s = new MemoryStream();
                    var zoom = 1.0;
                    int h = (int)(face.FaceRectangle.Height / zoom);
                    int w = (int)(face.FaceRectangle.Width / zoom);
                    int x = (int)(face.FaceRectangle.Left);
                    int y = (int)(face.FaceRectangle.Top);

                    // 擷取原圖片辨識後的臉部座標方框，並存成圖片
                    img.Clone(ctx => ctx.Crop(new Rectangle(x, y, w, h))).Save("face" + j + ".jpg");
                    img.Clone(ctx => ctx.Crop(new Rectangle(x, y, w, h))).SaveAsJpeg(s);
                    faceList.Add(s.ToArray());

                    j += 1;
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }

            return faceList;
        }

        private IFaceClient Authenticate(string endPoint, string subKey)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(subKey)) { Endpoint = endPoint };
        }

        //private List<byte[]> GetFaces(byte[] image)
        //{
        //    Mat src = Cv2.ImDecode(image, ImreadModes.Color);

        //    // Convert the byte array into jpeg image and Save the image coming from the source
        //    // in the root directory for testing purposes. 
        //    src.SaveImage("image.jpg",new ImageEncodingParam(ImwriteFlags.JpegProgressive,255));

        //    var file = Path.Combine(Directory.GetCurrentDirectory(), "CascadeFile", "haarcascade_frontalface_default.xml");
        //    var faceCascade = new CascadeClassifier();
        //    faceCascade.Load(file);

        //    Rect[] faces = faceCascade.DetectMultiScale(src, 1.1, 6, HaarDetectionTypes.DoRoughSearch, new Size(60, 60));

        //    var faceList = new List<byte[]>();
        //    int j = 0;
        //    foreach (Rect rect in faces) 
        //    {
        //        var faceImg = new Mat(src, rect);
        //        faceList.Add(faceImg.ToBytes(".jpg"));
        //        faceImg.SaveImage($"face{j}.jpg",new ImageEncodingParam(ImwriteFlags.JpegProgressive,255));
        //        j += 1;
        //    }

        //    return faceList;
        //}
    }
}
