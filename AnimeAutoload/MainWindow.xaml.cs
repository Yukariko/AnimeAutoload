using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using KeyValueLite;
using System.Windows.Controls;
using HtmlAgilityPack;

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
            AnimeListBox_Initialized();
            MyAnimeListBox_Initialized();
        }

        private void AnimeListBox_Initialized()
        {
            List<Anime> items = new List<Anime>();

            string url = "http://www.anissia.net/anitime/list?w=";
            for (int i = 0; i < 7; i++)
            {
                var json = GetAnimeData(url + i);
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

        private void MyAnimeListBox_Initialized()
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
                var json = GetSubtitlesData(ani);

                if (json.Count != 0)
                {
                    int temp = json[0]["s"].ToObject<int>();
                    if (temp == ani.recentView)
                    {
                        ani.color = "Green";
                    }
                }
                else
                {
                    ani.color = "Green";
                }

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

        public JArray GetAnimeData(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            var json = JArray.Parse(responseFromServer);
            return json;
        }

        public JArray GetSubtitlesData(Anime ani)
        {
            string url = "http://www.anissia.net/anitime/cap?i=" + ani.no;
            return GetAnimeData(url);
        }

        public Tuple<string, string> GetMagnetUrl(Anime ani)
        {
            if (ani.prefix.Length == 0)
            {
                return new Tuple<string, string>("접두사를 설정해주세요", "");
            }
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            string url = "https://nyaa.si/?f=0&c=0_0&q=" + ani.prefix + "%20" + ani.suffix;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 70.0.3538.102 Safari / 537.36";
            request.Accept = "*/*";
            request.Headers.Add("Accept-Encoding", "gzip");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.KeepAlive = false;
            request.Timeout = 5000;
            string responseFromServer = "";
            try
            {
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                responseFromServer = reader.ReadToEnd();
                response.Close();
            } catch
            {
                return new Tuple<string, string>("연결 시간 초과", "잠시 후 다시 시도해보세요");
            }
            

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(responseFromServer);

            var first = doc.DocumentNode.SelectNodes("/html/body/div/div/table/tbody/tr/td");
            if (first == null)
            {
                return new Tuple<string, string>("검색 결과가 없습니다.", "");
            }
            var find = first.Elements();
            string title = "";
            string magnet = "";

            foreach (var elem in find)
            {
                
                var value = elem.Name;

                if (value == "a")
                {
                    if (elem.GetAttributeValue("title", "").StartsWith(ani.prefix))
                    {
                        title = elem.GetAttributeValue("title", "");
                    }
                    else if (elem.GetAttributeValue("href", "").StartsWith("magnet"))
                    {
                        magnet = elem.GetAttributeValue("href", "");
                        break;
                    }
                }

            }
            
            return new Tuple<string, string>(title, magnet);
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

        public string ToSeries(int series)
        {
            if (series == -1)
            {
                return "미시청";
            }
            if (series % 10 == 0)
            {
                return (series / 10).ToString();
            }
            return ((double)series / 10.0).ToString();
        }
        
        private void MyAnimeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyAnimeListBox.SelectedItem == null)
            {
                return;
            }

            SubtitleListBox.Items.Clear();
            TorrentListBox.Items.Clear();

            Anime ani = (Anime)MyAnimeListBox.SelectedItem;

            TorrentListBox.Items.Add("접두사");
            TorrentListBox.Items.Add(ani.prefix);
            TorrentListBox.Items.Add("접미사");
            TorrentListBox.Items.Add(ani.suffix);

            var data = GetMagnetUrl(ani);
            TorrentListBox.Items.Add(data.Item1);
            TorrentListBox.Items.Add(data.Item2);

            SubtitleListBox.Items.Add("최근 확인 : " + ToSeries(ani.recentView) + "화");

            var json = GetSubtitlesData(ani);

            if (json.Count == 0)
            {
                ani.color = "Green";
                MyAnimeListBox.Items.Refresh();
                return;
            }
            int temp = json[0]["s"].ToObject<int>();
            if (temp != ani.recentView)
            {
                ani.color = "Green";
                MyAnimeListBox.Items.Refresh();
                ani.recentView = temp;
                kvs.Set(ani.no.ToString(), ani.data.ToString());
            }

            foreach (JObject elem in json)
            {
                int series = elem["s"].ToObject<int>();
                // temp = Math.Max(temp, series);
                SubtitleListBox.Items.Add(ToSeries(series) + "화 " + elem["n"].ToString());
                SubtitleListBox.Items.Add(elem["a"].ToString());
            }
        }

        private void SubtitleListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SubtitleListBox.SelectedItem == null)
            {
                return;
            }

            string url = (string)SubtitleListBox.SelectedItem;

            if (url.StartsWith("http"))
            {
                System.Diagnostics.Process.Start(url);
            }

        }

        private void TorrentListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TorrentListBox.SelectedItem == null)
            {
                return;
            }

            int idx = TorrentListBox.SelectedIndex;
            string item = (string)TorrentListBox.SelectedItem;

            Anime ani = (Anime)MyAnimeListBox.SelectedItem;

            if (idx == 0)
            {
                InputBox win = new InputBox(item);
                win.Owner = this;
                win.ShowDialog();
                TorrentListBox.Items[1] = win.TextBox.Text;
                ani.prefix = win.TextBox.Text;
                kvs.Set(ani.no.ToString(), ani.data.ToString());
            }

            else if (idx == 2)
            {
                InputBox win = new InputBox(item);
                win.Owner = this;
                win.ShowDialog();
                TorrentListBox.Items[3] = win.TextBox.Text;
                ani.suffix = win.TextBox.Text;
                kvs.Set(ani.no.ToString(), ani.data.ToString());
            }

            else if (item.StartsWith("magnet"))
            {
                System.Diagnostics.Process.Start(item);
            }
        }

 
    }

    public class Anime
    {
        public Anime(JObject elem)
        {
            data = elem;
            no = elem["i"].ToObject<int>();
            title = elem["s"].ToString();
            genre = elem["g"].ToString();
            if (elem.ContainsKey("r"))
            {
                recentView = elem["r"].ToObject<int>();
            }
            else
            {
                recentView = -1;
            }
            if (elem.ContainsKey("pre"))
            {
                prefix = elem["pre"].ToString();
            }
            if (elem.ContainsKey("suf"))
            {
                suffix = elem["suf"].ToString();
            }
        }
        public int no { get; set; }
        public string title { get; set; }
        public string genre { get; set; }
        public int _recentView;
        public int recentView
        {
            get
            {
                return _recentView;
            }
            set
            {
                _recentView = value;
                data["r"] = value;
            }
        }

        public string _prefix = "";
        public string prefix
        {
            get
            {
                return _prefix;
            }
            set
            {
                _prefix = value;
                data["pre"] = value;
            }
        }

        public string _suffix = "";
        public string suffix
        {
            get
            {
                return _suffix;
            }
            set
            {
                _suffix = value;
                data["suf"] = value;
            }
        }

        public string color { get; set; } = "Red";

        public JObject data;
    }
    
}
