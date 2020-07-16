    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    namespace EsenCompany.Client
    {
        class Program
        {
            public static TcpClient clientSocket;
            public static NetworkStream serverStream = default(NetworkStream);
            public static string readData = null;
            static Thread ctThread;
            String name = null;
            //Dictionary<string, Object> nowChatting = new Dictionary<string, Object>();
            List<string> nowChatting = new List<string>();
            List<string> chat = new List<string>();
            static string ip = "";
            static void Main(string[] args)
            {
                Listen();
                string iPHost = Dns.GetHostName();
                ip = Dns.GetHostByName(iPHost).AddressList[1].ToString();

                Console.WriteLine("Ipiniz => " + ip);
                //string name = Console.ReadLine();
                string connect = "";
                do
                {
                    Console.WriteLine("Baglanmak için c ye basınız.");
                    connect = Console.ReadLine();
                }
                while (connect != "c");
                clientSocket = new TcpClient();
                try
                {
                    clientSocket.Connect("192.168.2.54", 5000);

                    readData = "Connected to Server ";
                    //msg();

                    serverStream = clientSocket.GetStream();

                    byte[] outStream = Encoding.ASCII.GetBytes(ip + "$");
                    serverStream.Write(outStream, 0, outStream.Length);
                    serverStream.Flush();
                    //btnConnect.Enabled = false;


                    //ctThread = new Thread(getMessage);
                    //ctThread.Start();
                }
                catch (Exception er)
                {
                    Console.WriteLine("Server Not Started");
                }
                Console.ReadKey();

            }
            private void getMessage()
            {
                try
                {
                    while (true)
                    {
                        serverStream = clientSocket.GetStream();
                        byte[] inStream = new byte[10025];
                        serverStream.Read(inStream, 0, inStream.Length);
                        List<string> parts = null;

                        if (!SocketConnected(clientSocket))
                        {
                            Console.WriteLine("You've been Disconnected");
                            ctThread.Abort();
                            clientSocket.Close();

                        }

                        // parts = (List<string>)ByteArrayToObject(inStream);
                        switch (parts[0])
                        {
                            case "userList":
                                //  getUsers(parts);
                                break;

                            case "gChat":
                                readData = "" + parts[1];
                                // msg();
                                break;

                            case "pChat":
                                //  managePrivateChat(parts);
                                break;
                        }

                        if (readData[0].Equals('\0'))
                        {
                            readData = "Reconnect Again";


                            ctThread.Abort();
                            clientSocket.Close();
                            break;
                        }
                        chat.Clear();
                    }
                }
                catch (Exception e)
                {
                    ctThread.Abort();
                    clientSocket.Close();

                    Console.WriteLine(e);
                }

            }

            bool SocketConnected(TcpClient s) //check whether client is connected server
            {
                bool flag = false;
                try
                {
                    bool part1 = s.Client.Poll(10, SelectMode.SelectRead);
                    bool part2 = (s.Available == 0);
                    if (part1 && part2)
                    {
                        //indicator.BackColor = Color.Red;
                        //this.Invoke((MethodInvoker)delegate // cross threads
                        //{
                        //    btnConnect.Enabled = true;
                        //});
                        flag = false;
                    }
                    else
                    {
                        //indicator.BackColor = Color.Green;
                        flag = true;
                    }
                }
                catch (Exception er)
                {
                    Console.WriteLine(er);
                }
                return flag;
            }


            public static void Listen()
            {
            try
            {
                FileSystemWatcher watcher = new FileSystemWatcher(@"C:\Json");

                watcher.Created += Watcher_Created;
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            }

            private static void Watcher_Created(object sender, FileSystemEventArgs e)
            {
            Thread.Sleep(200);
                var Response = e.FullPath.Split('_');
                if (Response[2] == "Request.json")
                {
                Thread.Sleep(200);
                switch (Response[1])
                    {
                        case "IDList":
                            var Request = ReadJsonFile<KontrolClass>(e.FullPath);
                            IDListesiDoldur(Request.Select(x => x.ID).ToList());
                            break;
                        default:
                            break;
                    }
                }
            }
            public static List<T> ReadJsonFile<T>(string Path) where T : class
            {
                string jsonFilePath = Path;

                try
                {
                    string json = File.ReadAllText(jsonFilePath);
                    if (!String.IsNullOrEmpty(json))
                    {
                        return JsonConvert.DeserializeObject<List<T>>(json);
                    }
                    else
                    {
                        Console.WriteLine("Dosya Boş");

                    }
                }
                catch (Exception)
                {

                    Console.WriteLine("Okuma Sırasında Hata Oluştu");
                }
                return null;

            }

            static void IDListesiDoldur(List<string> idListesi)
            {
                var ReturnList = new List<KontrolClass>();
                Random rnd = new Random();
                idListesi.ForEach(x => ReturnList.Add(new KontrolClass { Deger = Convert.ToBoolean(rnd.Next(2)), ID = x }));
                CreteJsonFile(ip + "_IDList_Response", ReturnList);
                SendJson(ip + "_IDList_Response");
            }

            public static void SendJson(string FileName)
            {

                string FTPDosyaYolu = "ftp://" + "192.168.2.54" + "//" + FileName + ".json";
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
            JsonDeleted();
        }

        private static void JsonDeleted()
        {
            System.IO.DirectoryInfo klasor = new DirectoryInfo(@"C:\Json");

            foreach (FileInfo dosya in klasor.GetFiles())
            {
                dosya.Delete();
            }
        }

        public static void CreteJsonFile<T>(string FileName, List<T> Data) where T : class
            {
                var path = @"C:\Json\" + FileName + ".json";
                using (var tw = new StreamWriter(path, true))
                {
                    tw.WriteLine(JsonConvert.SerializeObject(Data));
                    tw.Close();
                }
            }
            public class KontrolClass
            {
                public string ID { get; set; }
                public bool Deger { get; set; }
            }
        }
    }

