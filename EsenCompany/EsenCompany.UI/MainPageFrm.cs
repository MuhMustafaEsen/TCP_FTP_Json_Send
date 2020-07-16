using EsenCompany.Entities;
using EsenCompany.UI.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EsenCompany.UI
{
    public partial class MainPageFrm : Form
    {
                                                                //senin tcp server adresin ve portun yazacak
        TcpListener listener = new TcpListener(IPAddress.Parse("192.168.2.54"), 5000);
        TcpClient client;
        String clNo;
        Dictionary<string, TcpClient> clientList = new Dictionary<string, TcpClient>();

        public static List<KontrolClass> responseList = new List<KontrolClass>();
        public static List<KontrolClass> requestList = new List<KontrolClass>();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        List<string> chat = new List<string>();
        public MainPageFrm()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = true;
            btnStart.Enabled = false;
            cancellation = new CancellationTokenSource(); //resets the token when the server restarts
            StartServer();
        }
        public void updateUI(String m)
        {
            this.Invoke((MethodInvoker)delegate // To Write the Received data
            {
                textBox1.AppendText(">>" + m + Environment.NewLine);
            });
        }
        public async void StartServer()
        {
            listener.Start();
            updateUI("Server Started at " + listener.LocalEndpoint);
            updateUI("Waiting for Clients");
            try
            {
                int counter = 0;
                while (true)
                {
                    counter++;
                    //client = await listener.AcceptTcpClientAsync();
                    client = await Task.Run(() => listener.AcceptTcpClientAsync(), cancellation.Token);

                    /* get username */
                    byte[] name = new byte[50];
                    NetworkStream stre = client.GetStream(); //Gets The Stream of The Connection
                    stre.Read(name, 0, name.Length); //Receives Data 
                    String username = Encoding.ASCII.GetString(name); // Converts Bytes Received to String
                    username = username.Substring(0, username.IndexOf("$"));

                    /* add to dictionary, listbox and send userList  */
                    clientList.Add(username, client);
                    listBox1.Items.Add(username);
                    updateUI("Connected to user " + username + " - " + client.Client.RemoteEndPoint);
                    Announce(username + " Joined ", username, false);

                    //await Task.Delay(1000).ContinueWith(t => SendUsersList());


                    var c = new Thread(() => ServerReceive(client, username));
                    c.Start();

                }
            }
            catch (Exception)
            {
                listener.Stop();
            }

        }
        public void Announce(string msg, string uName, bool flag)
        {
            try
            {
                foreach (var Item in clientList)
                {
                    TcpClient broadcastSocket;
                    broadcastSocket = (TcpClient)Item.Value;
                    NetworkStream broadcastStream = broadcastSocket.GetStream();
                    Byte[] broadcastBytes = null;

                    if (flag)
                    {
                        //broadcastBytes = Encoding.ASCII.GetBytes("gChat|*|" + uName + " says : " + msg);

                        chat.Add("gChat");
                        chat.Add(uName + " says : " + msg);
                        broadcastBytes = ObjectToByteArray(chat);
                    }
                    else
                    {
                        //broadcastBytes = Encoding.ASCII.GetBytes("gChat|*|" + msg);

                        chat.Add("gChat");
                        chat.Add(msg);
                        broadcastBytes = ObjectToByteArray(chat);

                    }

                    broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);
                    broadcastStream.Flush();
                    chat.Clear();
                }
            }
            catch (Exception er)
            {

            }
        }
        public Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }
        public byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        public void ServerReceive(TcpClient clientn, String username)
        {
            byte[] data = new byte[1000];
            String text = null;
            while (true)
            {
                try
                {
                    NetworkStream stream = clientn.GetStream(); //Gets The Stream of The Connection
                    stream.Read(data, 0, data.Length); //Receives Data 
                    List<string> parts = (List<string>)ByteArrayToObject(data);

                    switch (parts[0])
                    {
                        case "gChat":
                            this.Invoke((MethodInvoker)delegate // To Write the Received data
                            {
                                textBox1.Text += username + ": " + parts[1] + Environment.NewLine;
                            });
                            Announce(parts[1], username, true);
                            break;


                    }

                    parts.Clear();
                }
                catch (Exception r)
                {
                    updateUI("Client Disconnected: " + username);
                    Announce("Client Disconnected: " + username + "$", username, false);
                    clientList.Remove(username);

                    this.Invoke((MethodInvoker)delegate
                    {
                        listBox1.Items.Remove(username);
                    });
                    //SendUsersList();
                    break;
                }
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            try
            {
                listener.Stop();
                updateUI("Server Stopped");
                foreach (var Item in clientList)
                {
                    TcpClient broadcastSocket;
                    broadcastSocket = (TcpClient)Item.Value;
                    broadcastSocket.Close();
                }
            }
            catch (SocketException er)
            {

            }
        }
        //public void SendUsersList()
        //{
        //    try
        //    {
        //        byte[] userList = new byte[1024];
        //        string[] clist = listBox1.Items.OfType<string>().ToArray();
        //        List<string> users = new List<string>();

        //        users.Add("userList");
        //        foreach (String name in clist)
        //        {
        //            users.Add(name);
        //        }
        //        userList = ObjectToByteArray(users);

        //        foreach (var Item in clientList)
        //        {
        //            TcpClient broadcastSocket;
        //            broadcastSocket = (TcpClient)Item.Value;
        //            NetworkStream broadcastStream = broadcastSocket.GetStream();
        //            broadcastStream.Write(userList, 0, userList.Length);
        //            broadcastStream.Flush();
        //            users.Clear();
        //        }
        //    }
        //    catch (SocketException se)
        //    {
        //        MessageBox.Show(se.Message);
        //    }
        //}

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.SelectionStart = textBox1.TextLength;
            textBox1.ScrollToCaret();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            flowLayoutPanel2.Controls.Clear();
            flowLayoutPanel3.Controls.Clear();
            var x = listBox1.SelectedItem as string;
            if (x != "192.168.2.145")
                return;
            string filename = x + "_IDList_Request";
            Random rnd = new Random();
            JsonHelper.CreteJsonFile<KontrolClass>(filename, new List<KontrolClass> { new KontrolClass { ID = Guid.NewGuid().ToString(), Deger = Convert.ToBoolean(rnd.Next(2)) }, new KontrolClass { ID = Guid.NewGuid().ToString(), Deger = Convert.ToBoolean(rnd.Next(2)) }, new KontrolClass { ID = Guid.NewGuid().ToString(), Deger = Convert.ToBoolean(rnd.Next(2)) } }); ;
            JsonHelper.SendJson(x, filename);
            
        }


        

        public List<T> ReadJsonFile<T>(string Path) where T : class
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
                    MessageBox.Show("Dosya Boş");

                }
            }
            catch (Exception)
            {

                MessageBox.Show("Okuma Sırasında Hata Oluştu");
            }
            return null;

        }
        public void Listen()
        {
            FileSystemWatcher watcher = new FileSystemWatcher(@"C:\Json");
            watcher.Created += Watcher_Created;
            watcher.EnableRaisingEvents = true;



        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            Button btna;
            var Response = e.FullPath.Split('_');
            if (Response[2] == "Response.json")
            {
                Thread.Sleep(200);
                flowLayoutPanel1.Controls.Clear();
                switch (Response[1])
                {
                    case "IDList":
                        responseList = ReadJsonFile<KontrolClass>(e.FullPath);
                        requestList = ReadJsonFile<KontrolClass>(Response[0] + "_" + Response[1] + "_Request.json");
                        foreach (var item in responseList)
                        {
                            btna = new Button();
                            btna.Width = 250;
                            btna.Height = 45;
                            btna.Name = item.ID;
                            btna.Tag = item.ToString();
                            btna.Click += Btna_Click;
                            btna.Text = item.ID;
                            if (this.InvokeRequired)
                            {
                                this.Invoke((MethodInvoker)delegate ()
                                {
                                    flowLayoutPanel2.Controls.Add(btna);
                                });
                            }
                        }
                       
                        Thread.Sleep(200);
                        foreach (var item in requestList)
                        {
                            Thread.Sleep(200);
                            var temp = responseList.Where(x => x.ID == item.ID).FirstOrDefault();
                            if (temp != null)
                            {
                                Button btn = new Button();
                                btn.Width = 25;
                                btn.Height = 25;
                                if (temp.Deger == item.Deger)
                                    btn.BackColor = Color.Green;
                                else
                                    btn.BackColor = Color.Red;
                                if (this.InvokeRequired)
                                {
                                    this.Invoke((MethodInvoker)delegate ()
                                   {
                                       flowLayoutPanel1.Controls.Add(btn);
                                   });
                                }

                            }
                        }
                        JsonDeleted();
                        break;
                    default:
                        break;
                }
            }

        }

        private void Btna_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            Label lbl = new Label();
            lbl.Width = 25;
            lbl.Height = 25;
            var res = responseList.Where(a => a.ID == btn.Text).FirstOrDefault();
            var req = requestList.Where(a => a.ID == btn.Text).FirstOrDefault();
            if (rbAnd.Checked == true)
            {
                if (Convert.ToBoolean(res.Deger) == true && Convert.ToBoolean(req.Deger == true))            
                    lbl.BackColor = Color.Green;                
                else
                    lbl.BackColor = Color.Red;
            }
            else if (rbOr.Checked == true)
            {
                if (Convert.ToBoolean(res.Deger) == false && Convert.ToBoolean(req.Deger == false))
                {
                    lbl.BackColor = Color.Red;
                }
                else
                {
                    lbl.BackColor = Color.Green;
                }
            }
            
                    flowLayoutPanel3.Controls.Add(lbl);
            
            
        }

        private void JsonDeleted()
        {
            System.IO.DirectoryInfo klasor = new DirectoryInfo(@"C:\Json");

            foreach (FileInfo dosya in klasor.GetFiles())
            {
                dosya.Delete();
            }
        }



        private void MainPageFrm_Load(object sender, EventArgs e)
        {
            rbAnd.Checked = true;
            btnStop.Enabled = false;
            Listen();
        }
    }
}
