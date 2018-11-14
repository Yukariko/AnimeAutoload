using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using KeyValueLite;

namespace AnimeAutoload
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<int, bool> database = new Dictionary<int, bool>();
        KeyValueStore kvs = new KeyValueStore();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void AnimeListBox_Initialized(object sender, EventArgs e)
        {
            List<Anime> items = new List<Anime>();

            string url = "http://www.anissia.net/anitime/list?w=";
            for (int i = 0; i < 7; i++)
            {
                WebRequest request = WebRequest.Create(url + i);
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();

                var json = JArray.Parse(responseFromServer);
                foreach (JObject elem in json)
                {
                    Anime ani = new Anime(elem);
                    items.Add(ani);
                }

            }
            AnimeListBox.ItemsSource = items;
        }

        private void AnimeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AnimeListBox.SelectedItem == null)
            {
                return;
            }

            Anime ani = (Anime)AnimeListBox.SelectedItem;
            AddAnime(ani);
        }

        private void MyAnimeListBox_Initialized(object sender, EventArgs e)
        {
            kvs.Initialize(options =>
            {
                options.DatabaseName = "anime.dat";
            });
            kvs.Open();
            foreach (var kv in kvs.QueryAllKeys())
            {
                JObject elem = JObject.Parse(kvs.Get(kv));
                Anime ani = new Anime(elem);
                MyAnimeListBox.Items.Add(ani);
                database.Add(ani.no, true);
            }
        }

        private void MyAnimeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MyAnimeListBox.SelectedItem == null)
            {
                return;
            }

            Anime ani = (Anime)MyAnimeListBox.SelectedItem;
            RemoveAnime(ani);
        }

        public void AddAnime(Anime ani)
        {
            if (!database.ContainsKey(ani.no))
            {
                database.Add(ani.no, true);
                kvs.Set(ani.no.ToString(), ani.data.ToString());
                MyAnimeListBox.Items.Add(ani);
            }
        }


        public void RemoveAnime(Anime ani)
        {
            if (database.ContainsKey(ani.no))
            {
                database.Remove(ani.no);
                kvs.Clear(ani.no.ToString());
                MyAnimeListBox.Items.Remove(ani);
            }
        }

        private void MyAnimeListBox_Selected(object sender, RoutedEventArgs e)
        {

        }
    }

    public class Anime
    {
        public Anime(JObject elem)
        {
            no = elem["i"].ToObject<int>();
            title = elem["s"].ToString();
            genre = elem["g"].ToString();
            data = elem;
        }
        public int no { get; set; }
        public string title { get; set; }
        public string genre { get; set; }
        public JObject data;
    }
    
}
