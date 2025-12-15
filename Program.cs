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
using HtmlAgilityPack;


namespace HttpNewsPAT
{
    public class Program
    {
        private static HttpClient httpClient = new HttpClient();
        private static string Token;

        static async Task Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener("DebugLog.txt"));
            Help();

            while (true)
            {
                await SetComandAsync(); // Ожидаем команду
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
        public static async Task AddNewPostAsync()
        {
            if (!string.IsNullOrEmpty(Token))
            {
                string name;
                string description;
                string image;
                Console.WriteLine("Заголовок новости:");
                name = Console.ReadLine();
                Console.WriteLine("Текст новости:");
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
            Console.Write("/SingIn");
            Console.WriteLine(" (авторизация на сайте)");
            Console.Write("/Posts");
            Console.WriteLine(" (вывод всех постов на сайте)");
            Console.Write("/Add");
            Console.WriteLine(" (добавление новой записи)");
            Console.Write("/prk");
            Console.WriteLine(" (парсинг новостей с Радиотехнического колледжа)");
        }
        static async Task SetComandAsync()
        {
            try
            {
                string Command = Console.ReadLine();

                if (Command.Contains("/SingIn"))
                    await SignIn("user", "user"); 

                if (Command.Contains("/Posts"))
                    ParsingHtml(await GetContent());

                if (Command.Contains("/Add"))
                    await AddNewPostAsync(); 

                if (Command.Contains("/prk"))
                    await ParsePrkRu();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Request error: " + ex.Message);
            }
        }
        public class NewsItem
        {
            public string Date { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
        }
        public static async Task ParsePrkRu()
        {
            try
            {
                string url = "https://prk.perm.ru/novosti-i-sobytiya";
                WriteLog($"Начинается парсинг Prk.ru: {url}");

                HttpResponseMessage response = await httpClient.GetAsync(url);
                WriteLog($"Статус выполнения запроса к Prk.ru: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    string htmlCode = await response.Content.ReadAsStringAsync();
                    HtmlDocument html = new HtmlDocument();
                    html.LoadHtml(htmlCode);

                 
                    string pageText = WebUtility.HtmlDecode(html.DocumentNode.InnerText);

                    
                    string parsedContent = "\n=== Последние новости с Prk.ru ===\n\n";

                   
                    string[] lines = pageText.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    
                    int newsSectionStart = -1;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (line.Equals("Новости и события", StringComparison.OrdinalIgnoreCase))
                        {
                            newsSectionStart = i;
                            break;
                        }
                    }

                    List<NewsItem> newsItems = new List<NewsItem>();

                    if (newsSectionStart >= 0)
                    {
                       
                        for (int i = newsSectionStart + 1; i < lines.Length; i++)
                        {
                            string line = lines[i].Trim();

                            if (string.IsNullOrEmpty(line)) continue;

                           
                            if (line.Contains("Продолжая использовать наш сайт"))
                                break;

                            
                            if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\d{1,2}\s+[а-яА-Я]+\s+\d{4}$"))
                            {
                                
                                string date = line;
                                string title = "";
                                StringBuilder description = new StringBuilder();

                              
                                for (int j = i + 1; j < lines.Length; j++)
                                {
                                    string nextLine = lines[j].Trim();
                                    if (!string.IsNullOrEmpty(nextLine))
                                    {
                                      
                                        if (System.Text.RegularExpressions.Regex.IsMatch(nextLine, @"^\d{1,2}\s+[а-яА-Я]+\s+\d{4}$"))
                                        {
                                            break;
                                        }

                                        title = nextLine;
                                        i = j; 

                                        
                                        for (int k = j + 1; k < lines.Length; k++)
                                        {
                                            string descLine = lines[k].Trim();

                                          
                                            if (string.IsNullOrEmpty(descLine) ||
                                                System.Text.RegularExpressions.Regex.IsMatch(descLine, @"^\d{1,2}\s+[а-яА-Я]+\s+\d{4}$") ||
                                                descLine.Contains("Продолжая использовать наш сайт"))
                                            {
                                                i = k - 1; 
                                                break;
                                            }

                                           
                                            description.AppendLine(descLine);
                                        }
                                        break;
                                    }
                                }

                               
                                string shortDescription = description.ToString().Trim();
                                if (shortDescription.Length > 200)
                                {
                                    shortDescription = shortDescription.Substring(0, 200) + "...";
                                }

                                newsItems.Add(new NewsItem
                                {
                                    Date = date,
                                    Title = title,
                                    Description = shortDescription
                                });
                            }
                        }
                    }

                  
                    int count = 0;
                    foreach (var news in newsItems)
                    {
                        if (count >= 5) break;

                        if (!string.IsNullOrEmpty(news.Title))
                        {
                            parsedContent += $" {news.Date}\n";
                            parsedContent += $" {news.Title}\n";

                            if (!string.IsNullOrEmpty(news.Description))
                            {
                                parsedContent += $" {news.Description}\n";
                            }

                            parsedContent += "────────────────────\n";
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        Console.WriteLine(parsedContent);
                        WriteLog($"Успешно спаршено {count} новостей с описаниями");
                    }
                    else
                    {
                        
                        Console.WriteLine("Пробуем упрощенный метод парсинга...\n");

                        parsedContent = "\n=== Последние новости с Prk.ru ===\n\n";
                        count = 0;

                        if (newsSectionStart >= 0)
                        {
                            for (int i = newsSectionStart; i < Math.Min(lines.Length, newsSectionStart + 100) && count < 5; i++)
                            {
                                string line = lines[i].Trim();

                                // Ищем дату
                                if (line.Length > 0 && char.IsDigit(line[0]) && line.Contains("2025"))
                                {
                                    string date = line;
                                    string title = "";
                                    string description = "";

                                    
                                    for (int j = i + 1; j < Math.Min(lines.Length, i + 10); j++)
                                    {
                                        string nextLine = lines[j].Trim();
                                        if (!string.IsNullOrEmpty(nextLine) && !nextLine.Contains("2025"))
                                        {
                                            title = nextLine;

                                            
                                            StringBuilder descBuilder = new StringBuilder();
                                            for (int k = j + 1; k < Math.Min(lines.Length, j + 10); k++)
                                            {
                                                string descLine = lines[k].Trim();
                                                if (string.IsNullOrEmpty(descLine) ||
                                                    (descLine.Contains("2025") && char.IsDigit(descLine[0])))
                                                    break;

                                                descBuilder.Append(descLine + " ");
                                            }

                                            description = descBuilder.ToString().Trim();
                                            if (description.Length > 150)
                                            {
                                                description = description.Substring(0, 150) + "...";
                                            }

                                            break;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(title))
                                    {
                                        parsedContent += $" {date}\n";
                                        parsedContent += $" {title}\n";

                                        if (!string.IsNullOrEmpty(description))
                                        {
                                            parsedContent += $" {description}\n";
                                        }

                                        parsedContent += "────────────────────\n";
                                        count++;
                                    }
                                }
                            }

                            if (count > 0)
                            {
                                Console.WriteLine(parsedContent);
                            }
                            else
                            {
                                Console.WriteLine("Не удалось найти новости с описаниями.");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Ошибка при запросе к Prk.ru: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге Prk.ru: {ex.Message}");
                WriteLog($"Ошибка в ParsePrkRu: {ex.ToString()}");
            }
        }
    }
}
