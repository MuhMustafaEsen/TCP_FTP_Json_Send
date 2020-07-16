using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EsenCompany.UI.Helper
{
    public class JsonHelper
    {
        public static void CreteJsonFile<T>(string FileName, List<T> Data) where T : class
        {
            var path = @"C:\Json\" + FileName + ".json";
            using (var tw = new StreamWriter(path, true))
            {
                tw.WriteLine(JsonConvert.SerializeObject(Data));
                tw.Close();
            }
        }
        public static void SendJson(string ip, string FileName)
        {

            string FTPDosyaYolu = "ftp://" + ip + "//" + FileName + ".json";
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(FTPDosyaYolu);

            //string username = "kullaniciadi";
            //string password = "şifre";
            //request.Credentials = new NetworkCredential(username, password);

            request.UsePassive = true; // pasif olarak kullanabilme
            request.UseBinary = true; // aktarım binary ile olacak
            request.KeepAlive = false; // sürekli açık tutm

            request.Method = WebRequestMethods.Ftp.UploadFile;

            FileStream stream = File.OpenRead(@"C:\Json\" + FileName + ".json");
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            stream.Close();

            Stream reqStream = request.GetRequestStream(); // yükleme işini yapan kodlar
            reqStream.Write(buffer, 0, buffer.Length);
            reqStream.Close();
        }
    }
}
