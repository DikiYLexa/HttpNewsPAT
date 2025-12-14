using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Fizzler;

namespace HttpNewsPAT
{
    public class Program
    {
        private static HttpClient httpClient = new HttpClient();
        private static string Token;

        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener("DebugLog.txt"));
            Help();
            while (true)
            {
                SetComand();
            }
        }
        public static void ParsingHtml(string htmlCode)
        {
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(htmlCode);
            HtmlNode document = html.DocumentNode;
            IEnumerable<HtmlNode> divsNews = document.Descendants().Where(n => n.HasClass("news"));
            string content = "";
            foreach (HtmlNode divNews in divsNews)
            {
                string src = divNews.ChildNodes[1].GetAttributeValue("src", "none");
                string name = divNews.ChildNodes[3].InnerText;
                string description = divNews.ChildNodes[5].InnerText;
                content += $"{name}\nИзображение: {src}\nОписание: {description}\n";
            }
            Console.Write(content);
        }
        public static async void AddNewPost()
        {
            if (!string.IsNullOrEmpty(Token))
            {
                string name;
                string description;
                string image;
                Console.WriteLine("Заголовок новости:");
                name = Console.ReadLine();
                Console.WriteLine("Текст новости");
                description = Console.ReadLine();
                Console.WriteLine("Ссылка на изображение:");
                image = Console.ReadLine();
                string url = "http://10.111.20.114/ajax/add.php";
                WriteLog($"Выполнение запроса: {url}");
                var postData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("name", name),
                    new KeyValuePair<string, string>("description", description),
                    new KeyValuePair<string, string>("src", image),
                    new KeyValuePair<string, string>("token", Token)
                });
                HttpResponseMessage response = await httpClient.PostAsync(url, postData);
                WriteLog($"Статус выполнения: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Запрос выполнен успешно");
                }
                else
                {
                    Console.WriteLine($"Ошибка выполнения запроса: {response.StatusCode}");
                }
            }
            else
            {
                Console.WriteLine($"Ошибка выполнения запроса: пользователь не авторизован");
            }
        }
        public static void WriteLog(string debugContent)
        {
            Debug.WriteLine(debugContent);
            Debug.Flush();
        }
        public static async Task SignIn(string Login, string Password)
        {
            string url = "http://10.111.20.114/ajax/login.php";
            WriteLog($"Выполнение запроса: {url}");
            var postData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("login", Login),
                new KeyValuePair<string, string>("password", Password)
            });
            HttpResponseMessage response = await httpClient.PostAsync(url, postData);
            WriteLog($"Статус выполнения: {response.StatusCode}");
            if (response.IsSuccessStatusCode)
            {
                string cookies = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
                if (!string.IsNullOrEmpty(cookies))
                {
                    Token = cookies.Split(';')[0].Split('=')[1];
                    Console.WriteLine("успешная авторизация");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка выполнения запроса: {response.StatusCode}");
            }
        }
        public static async Task<string> GetContent()
        {
            if (!string.IsNullOrEmpty(Token))
            {
                string url = "http://10.111.20.114/main";
                WriteLog($"Выполнение запроса: {url}");
                httpClient.DefaultRequestHeaders.Add("token", Token);
                HttpResponseMessage response = await httpClient.GetAsync(url);
                WriteLog($"Статус выполнения: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"Ошибка выполнения запроса: {response.StatusCode}");
                    return string.Empty;
                }
            }
            else
            {
                Console.WriteLine($"Ошибка выполнения запроса: не авторизован");
                return string.Empty;
            }
        }
        static void Help()
        {
            Console.Write("/SignIn");
            Console.WriteLine(" (авторизация на сайте)");
            Console.Write("/Posts");
            Console.WriteLine(" (вывод всех постов на сайте)");
            Console.Write("/Add");
            Console.WriteLine(" (добавление новой записи)");
            Console.Write("/Lenta");
            Console.WriteLine(" (парсинг новостей с Lenta.ru)");
        }
        static async void SetComand()
        {
            try
            {
                string Command = Console.ReadLine();
                if (Command.Contains("/SignIn")) await SignIn("user", "user");
                if (Command.Contains("/Posts")) ParsingHtml(await GetContent());
                if (Command.Contains("/Add")) AddNewPost();
                if (Command.Contains("/Lenta")) await ParseLentaRu();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Request error: " + ex.Message);
            }
        }
        public static async Task ParseLentaRu()
        {
            try
            {
                string url = "https://lenta.ru/";
                WriteLog($"Начинается парсинг Lenta.ru: {url}");

                HttpResponseMessage response = await httpClient.GetAsync(url);
                WriteLog($"Статус выполнения запроса к Lenta.ru: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    string htmlCode = await response.Content.ReadAsStringAsync();
                    HtmlDocument html = new HtmlDocument();
                    html.LoadHtml(htmlCode);
                    HtmlNode document = html.DocumentNode;

                    var newsNodes = document.SelectNodes("//a[contains(@class, 'card')] | //a[contains(@class, 'item')]");

                    string parsedContent = "";

                    if (newsNodes != null)
                    {
                        foreach (var newsItem in newsNodes.Take(10))
                        {
                            string title = WebUtility.HtmlDecode(newsItem.InnerText?.Trim() ?? "");
                            string link = newsItem.GetAttributeValue("href", "");
                            if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                            {
                                link = new Uri(new Uri(url), link).AbsoluteUri;
                            }

                            parsedContent += $"{title}\nСсылка: {link}\n\n";
                        }

                        Console.WriteLine(" Последние новости с Lenta.ru \n");
                        Console.Write(parsedContent);
                    }
                    else
                    {
                        Console.WriteLine("Не удалось найти новости на странице. Возможно, изменилась структура сайта.");
                        WriteLog("Не удалось найти элементы новостей по заданным селекторам.");
                    }
                }
                else
                {
                    Console.WriteLine($"Ошибка при запросе к Lenta.ru: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге Lenta.ru: {ex.Message}");
                WriteLog($"Ошибка в ParseLentaRu: {ex.ToString()}");
            }
        }
    }
}
