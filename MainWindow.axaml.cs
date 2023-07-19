using Avalonia.Controls;
using System;
using NetCoreServer;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Threading;
using TcpClient = NetCoreServer.TcpClient;
using System.Text.Json;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Collections.Generic;
using System.IO;

namespace app
{
    public partial class MainWindow : Window
    {
        // Server Address
        string address = "192.168.43.226";

        // Server Port
        int port = 1111;
        public static TextBox Mainbox = new TextBox();
        public static TextBox Idbox = new TextBox();
        public static TextBox usernamebox = new TextBox();
        //
        int i;
        ChatClient client = new("192.168.43.226", 1111, Mainbox, Idbox, usernamebox);
        public MainWindow()
        {
            InitializeComponent();
            i = 0;
            Mainbox = this.Find<TextBox>("MainBox");
            DataContext = this;
            Idbox = this.Find<TextBox>("IdBox");
            DataContext = this;
            usernamebox = this.Find<TextBox>("UsernameBox");
            DataContext = this;
        }

        private bool isConnected = false;
        private void cont(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                Connect.Content = "Connecting...";
                client.ConnectAsync();
            }
            else
            {
                MainBox.Text = "Client disconnecting...";
                client.DisconnectAndStop();
                MainBox.Text += "\nDone";
            }

            isConnected = !isConnected; // Toggle the connection status
            Connect.Content = isConnected ? "Disconnect" : "Connect";

            /* if (i == 0)
             {
                 Connect.Content = "Connecting";
                 client.ConnectAsync();
               /* // Connect.Content = "Connected";
                // MainBox.Text = "CONNECTED TO THE SERVER";
                  //   person user = new person()a
                 //    {
                 //        id = 1,
                //         username = "Faseeh",
                //         chaty = "",
               //          act = 1,
              //           to = 0,
                  //       from = 0
              //       };
                     // Serialize the person object to JSON
              //       string json = JsonSerializer.Serialize(user);
                     // Send the JSON string to the server
             //        client.SendAsync(json);
                 i = 1;
             }
             else
             {
                 MainBox.Text = "Client disconnecting...";
                 client.DisconnectAndStop();
                 MainBox.Text = MainBox.Text + "\nDoneeeeeeeeeeeeee";
                 i = 0;
                 Connect.Content = "Disconnecting";
             }*/
        }
        public void sendbtn(object sender, RoutedEventArgs e)
        {
            Mainbox.Text = Mainbox.Text + "\n" + "YOU : " + SendBox.Text;
            person user = new person()
            {
                id = 12,
                username = "Faseeh",
                chaty = SendBox.Text,
                act = 3,
                to = "Wasif",
                from = "o"
            };
            // Serialize the person object to JSON
            string json = JsonSerializer.Serialize(user);
            // Send the JSON string to the server
            client.SendAsync(json);
            SendBox.Text = " ";
        }
    }

    class ChatClient : TcpClient
    {
        private bool _stop;
        TextBox Mainbox, Idbox, usernamebox;

        private List<string> chatHistory = new List<string>();
        private object chatHistoryLock = new object();
        public ChatClient(string address, int port, TextBox mainbox, TextBox Idbox, TextBox usernamebox) : base(address, port)
        {
            this.Mainbox = mainbox;
            this.Idbox = Idbox;
            this.usernamebox = usernamebox;
            LoadChatHistory();
        }

        private void LoadChatHistory()
        {
            try
            {
                if (File.Exists("chat_history.json"))
                {
                    var jsonString = File.ReadAllText("chat_history.json");
                    chatHistory = JsonSerializer.Deserialize<List<string>>(jsonString);
                    UpdateChatHistoryUI();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading chat history: " + ex.Message);
            }
        }
        private void SaveChatHistory()
        {
            try
            {
                lock (chatHistoryLock)
                {

                    var jsonString = JsonSerializer.Serialize(chatHistory);
                    File.WriteAllText("chat_history.json", jsonString);
                }
            }
            catch (Exception ex)
            {
                Mainbox.Text = Mainbox.Text + ("Error saving chat history: " + ex.Message);
            }

        }

        private void UpdateChatHistoryUI()
        {
            lock (chatHistoryLock)
            {
                Dispatcher.UIThread.Invoke(new Action(() =>
                {
                    MainWindow.Mainbox.Text = string.Join("\n", chatHistory);
                }));
            }
        }
        public void write_screen(int id, string username, string msg)
        {
            Dispatcher.UIThread.Invoke(new Action((() =>
            {
                MainWindow.usernamebox.Text = username;
                MainWindow.Mainbox.Text = MainWindow.Mainbox.Text + " \n" + username + " : " + msg;
                MainWindow.Idbox.Text = id.ToString();
            }
             )));
        }

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
            Mainbox.Text = Mainbox.Text + "\nServer Disconnected";
        }

        protected override void OnConnected()
        {
            //Console.WriteLine($"Chat TCP client connected a new session with Id {Id}");
            //File.Create("chat_history.json");
            Dispatcher.UIThread.Invoke(new Action((() =>
            {
                Mainbox.Text = "FILE CREATED";
                //Connect.Content = "Connected";
                Mainbox.Text = "CONNECTED TO THE SERVER";
            }
             )));
            person user = new person()
            {
                id = 1,
                username = "Faseeh",
                chaty = "",
                act = 1,
                to = "Wasif",
                from = "0"
            };
            // Serialize the person object to JSON
            string json = JsonSerializer.Serialize(user);
            // Send the JSON string to the server
            SendAsync(json);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat TCP client disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {

            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            try
            {
                var jsonObject = JsonSerializer.Deserialize<person>(message);
                int id = jsonObject.id;
                string username = jsonObject.username;
                string msg = jsonObject.chaty;
                int act = jsonObject.act;
                string to = jsonObject.to;
                string ftom = jsonObject.from;

                lock (chatHistoryLock)
                {
                    chatHistory.Add(username + " : " + msg);
                    SaveChatHistory();
                }
                write_screen(id, username, msg);
            }
            catch (Exception e)
            {
                write_screen(0, message, message);
            }
        }

        /*
            public void sendImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filters.Add(new FileDialogFilter() { Name = "Image Files", Extensions = { "jpg", "png", "gif" } });
            var selectedFile = openFileDialog.ShowAsync(this);
            selectedFile.ContinueWith(t =>
            {
                var path = t.Result;
                if (!string.IsNullOrEmpty(path))
                {
                    using (FileStream fileStream = File.OpenRead(path))
                    {
                        byte[] buffer = new byte[fileStream.Length];
                        fileStream.Read(buffer, 0, (int)fileStream.Length);

                        person user = new person()
                        {
                            id = 12,
                            username = "Faseeh",
                            imageData = buffer,
                            act = 2,
                            to = 0,
                            from = 0
                        };
                        // Serialize the person object to JSON
                        string json = JsonSerializer.Serialize(user);

                        // Send the JSON string to the server
                        client.SendAsync(json);
                    }
                }
            }, TaskScheduler.FromCurrentSynchronizationContext()); 
         */
        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP client caught an error with code {error}");
        }


    }

    public class person
    {
        public int id { get; set; }
        public string username { get; set; }
        public string chaty { get; set; }

        public string from { get; set; }
        public int act { get; set; }

        public string to { get; set; }
    }
}

