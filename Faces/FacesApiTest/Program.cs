using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FacesApiTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var imagePath = @"oscars-2017.jpg";
            var urlAddress = "http://localhost:4301/api/faces";
            ImageUtility imageUtility = new ImageUtility();

            byte[] bytes = imageUtility.ConvertToByte(imagePath);
            List<byte[]> faceList = new List<byte[]>();

            var byteContent = new ByteArrayContent(bytes);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            using (var httpClient = new HttpClient()) 
            {
                using (var response = await httpClient.PostAsync(urlAddress, byteContent)) 
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    faceList = JsonConvert.DeserializeObject<List<byte[]>>(apiResponse);
                }
            }

            if (faceList.Count > 0) 
            {
                for (int i = 0; i < faceList.Count; i += 1) 
                {
                    imageUtility.FromByteToImage(faceList[i],$"face{i}");
                }
            }
        }
    }
}
