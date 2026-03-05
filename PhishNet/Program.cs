/* 
* Public domain software, no restrictions. 
* Released by rickd3ckard: https://github.com/rickd3ckard 
* See: https://unlicense.org/
*/


using E_Crawl_CSharp;
using MySql.Data.MySqlClient;
using System.Text;
using System.Text.Json;

class Program
{
    private static async Task Main(string[] args)
    {
        ApplicationOptions options = ValidateArguments(args);
        switch (options.Type)
        {
            case "help": return; //DisplayHelp();
            case "domain": await ScrapeSingleDomain(options); return;
            case "domains": await ScrapeMultipleDomains(options); return;
            default: throw new InvalidOperationException();
        }     
    }

    private static ApplicationOptions ValidateArguments(string[] args)
    {
        if (args.Length < 1) { throw new ArgumentException(); }
        if (string.IsNullOrWhiteSpace(args[0])) { throw new ArgumentException(); }
        if (args[0] != "help" && args[0] != "domain" && args[0] != "domains") { throw new ArgumentException(); }      
        ApplicationOptions options = new ApplicationOptions(args[0]); if (options.Type == "help") { return options; }

        if (args.Length % 2 != 0) { throw new ArgumentException(); }
        switch (options.Type)
        {
            case "domain":              
                if (string.IsNullOrWhiteSpace(args[1])) { throw new ArgumentException(); }
                options.Domain = args[1]; break;
            case "domains":
                if (string.IsNullOrWhiteSpace(args[1])) { throw new ArgumentException(); }               
                if (File.Exists(args[1])) { throw new FileNotFoundException(); }
                options.DomainList = args[1]; break;
        }
    
        for (int i = 2; i <= args.Length - 1; i += 2)
        {
            switch (args[i])
            {
                case "-depth":
                case "-d":
                    if (string.IsNullOrWhiteSpace(args[i + 1])) { throw new ArgumentException(); }
                    if (!int.TryParse(args[i + 1], out int _)) { throw new ArgumentException(); }
                    if (int.Parse(args[i + 1]) == 0) { throw new ArgumentException(); }
                    options.Depth = int.Parse(args[i + 1]); break;
                
                case "-maxemails":
                case "-m":
                    if (string.IsNullOrWhiteSpace(args[i + 1])) { throw new ArgumentException(); }
                    if (!int.TryParse(args[i + 1], out int _)) { throw new ArgumentException(); }
                    if (int.Parse(args[i + 1]) == 0) { throw new ArgumentException(); }
                    options.MaxMails = int.Parse(args[i + 1]); break;
               
                case "-outfile":
                case "-of":
                    if (string.IsNullOrWhiteSpace(args[i + 1])) { throw new ArgumentException(); }
                    options.OutputFile = args[i + 1]; break;
               
                case "-subdomains":
                case "-sd":
                    if (string.IsNullOrWhiteSpace(args[i + 1])) { throw new ArgumentException(); }
                    if (!bool.TryParse(args[i + 1], out bool _)) { throw new ArgumentException(); }
                    options.SubDomains = bool.Parse(args[i + 1]);  break;
               
                case "-threads":
                case "-t":
                    if (string.IsNullOrWhiteSpace(args[i + 1])) { throw new ArgumentException(); }
                    if (!int.TryParse(args[i + 1], out int _)) { throw new ArgumentException(); }
                    if (int.Parse(args[i + 1]) == 0) { throw new ArgumentException(); }
                    options.Threads = int.Parse(args[i + 1]); break;
               
                case "-filter":
                case "-f":
                    if (string.IsNullOrWhiteSpace(args[i + 1])) { throw new ArgumentException(); }
                    if (!args[i + 1].StartsWith(".")) { throw new ArgumentException(); }
                    options.Filter = args[i + 1]; break;

                case "-username":
                    if (string.IsNullOrWhiteSpace(args[i + 1])) { throw new ArgumentException(); }
                    options.UserName = args[i + 1]; break;

                case "-password":
                    if (string.IsNullOrWhiteSpace(args[i + 1])) { throw new ArgumentException(); }
                    options.Password = args[i + 1]; break;

                case "-database":
                    if (string.IsNullOrWhiteSpace(args[i + 1])) { throw new ArgumentException(); }
                    options.Database = args[i + 1]; break;

                case "-address":
                    if (string.IsNullOrWhiteSpace(args[i + 1])) { throw new ArgumentException(); }
                    options.Address = args[i + 1]; break;

                default: throw new ArgumentException();   
            }
        }

        bool allSqlNullOrWhiteSpace = string.IsNullOrWhiteSpace(options.Address) && string.IsNullOrWhiteSpace(options.UserName) && string.IsNullOrWhiteSpace(options.Password) && string.IsNullOrWhiteSpace(options.Database);
        bool noneSqlNullOrWhiteSpace = !string.IsNullOrWhiteSpace(options.Address) && !string.IsNullOrWhiteSpace(options.UserName) && !string.IsNullOrWhiteSpace(options.Password) && !string.IsNullOrWhiteSpace(options.Database);
        if (!(allSqlNullOrWhiteSpace || noneSqlNullOrWhiteSpace)) { throw new ArgumentException(); }

        return options;
    }

    private static async Task ScrapeSingleDomain(ApplicationOptions Options)
    {
        string domain = Options.Domain;
        string? formatedDomainName = FormatDomainName(domain);
        if (formatedDomainName == null) { return; }

        try { await ScrapeDomains(formatedDomainName, Options); }
        catch { Console.WriteLine("An error occured while attempting to scrape the domain: " + formatedDomainName); }
    }

    private static async Task ScrapeMultipleDomains(ApplicationOptions Options)
    { 
        int totalLines = File.ReadLines(Options.DomainList).Count(); int currentline = 1;
        using (StreamReader reader = new StreamReader(Options.DomainList))
        {
            while (reader.EndOfStream == false)
            {              
                string domainName = reader.ReadLine() ?? string.Empty;
                string? formatedDomainName = FormatDomainName(domainName);
                if (formatedDomainName == null ) { return; }

                try { await ScrapeDomains(formatedDomainName, Options); }
                catch { Console.WriteLine("An error occured while attempting to scrape the domain: " + formatedDomainName); }

                Console.WriteLine($"Progress: {currentline} of {totalLines} websites scraped so far...");
                currentline += 1;
            }
        };
    }

    private static List<WebsiteEmail> _sqlBufferMailList = new List<WebsiteEmail>();
    private static List<string> _sqlBufferDomainsList = new List<string>();
    private static async Task ScrapeDomains(string StartingDomain, ApplicationOptions Options)
    {
        Queue<string> domainQueue = new Queue<string>(); domainQueue.Enqueue(StartingDomain);
        List<string> visitedDomains = new List<string>(); visitedDomains.Add(StartingDomain);

        List<Task<Crawler>> taskPool = new List<Task<Crawler>>();
        for (byte i = 0; domainQueue.Count > 0; i ++)
        {
            string domain = domainQueue.Dequeue();
            taskPool.Add(CrawlDomain(domain, Options.Depth, Options.MaxMails, Options.SubDomains, Options.Filter));
        }

        while (taskPool.Count > 0)
        {
            Task<Crawler> finishedTask = await Task.WhenAny(taskPool);
            taskPool.Remove(finishedTask);

            foreach (string externalDomain in finishedTask.Result.ExternalDomains)
            {
                if (visitedDomains.Contains(externalDomain)) { continue; }
                visitedDomains.Add(externalDomain);
                domainQueue.Enqueue(externalDomain);
            }

            if (finishedTask.Result.EMails.Count > 0 && !string.IsNullOrWhiteSpace(Options.OutputFile)) { AppendToFile(Options.OutputFile, finishedTask.Result.EMails); }

            if (finishedTask.Result.EMails.Count > 0 && !string.IsNullOrWhiteSpace(Options.Address))
            {
                _sqlBufferMailList.AddRange(finishedTask.Result.EMails);
                _sqlBufferDomainsList.AddRange(finishedTask.Result.VisitedURLs);

                if (_sqlBufferMailList.Count > Options.Threads * 20)
                {
                    AppendToSQLDtb(Options.UserName, Options.Password, Options.Database, Options.Address, _sqlBufferMailList, _sqlBufferDomainsList);
                    _sqlBufferMailList.Clear();
                    _sqlBufferDomainsList.Clear();
                }
            }

            while (taskPool.Count < Options.Threads && domainQueue.Count > 0)
            {
                string domain = domainQueue.Dequeue();
                taskPool.Add(CrawlDomain(domain, Options.Depth, Options.MaxMails, Options.SubDomains, Options.Filter));
            }

            Console.WriteLine("Scraping completed for: " + finishedTask.Result.DomainName);
            Console.WriteLine("Found e-mails         : " + finishedTask.Result.EMails.Count);
            Console.WriteLine("Found new domains     : " + finishedTask.Result.ExternalDomains.Count);
            Console.WriteLine("Active threads count  : " + taskPool.Count);
            Console.WriteLine("Domain queue count    : " + domainQueue.Count + "\r\n");
        }
    }

    static async Task<Crawler> CrawlDomain(string DomainName, int Depth, int TargetMailCount, bool IncludeSubDomains, string Filter)
    {
        Crawler crawler = new Crawler(DomainName, Depth, TargetMailCount, IncludeSubDomains, Filter);

        try
        {       
            await crawler.Execute();       
        } 
        catch { }

        return crawler;
    }

    private static string? FormatDomainName(string DomainName) 
    {
        try
        {
            DomainName = DomainName.Replace("\\", "/");
            Uri newUri = new Uri(DomainName); string root = newUri.Host;
            if (!root.StartsWith("www.")) { root = "www." + root; }
            DomainName = "https://" + root + "/";
            return DomainName;
        }
        catch (Exception ex)        
        {
            Console.WriteLine(ex.Message);
            return null; 
        }
    }

    private static void AppendToFile(string FullFileName, List<WebsiteEmail> EmailsList)
    {
        Dictionary<string, List<string>>? jsonMails = new Dictionary<string, List<string>>();
        if (File.Exists(FullFileName))
        {
            jsonMails = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(FullFileName));
            if (jsonMails == null) throw new NullReferenceException(nameof(jsonMails));
        }

        foreach (WebsiteEmail wsMail in EmailsList)
        {
            Uri domainUri = new Uri(wsMail.URL);
            string domainRoot = $"{domainUri.Scheme}://{domainUri.Host}/";

            if (!jsonMails.Keys.Contains(domainRoot)) { jsonMails.Add(domainRoot, new List<string> { wsMail.Email }); }           
            else { if (!jsonMails[domainRoot].Contains(wsMail.Email)) { jsonMails[domainRoot].Add(wsMail.Email); } }
        }

        JsonSerializerOptions options = new JsonSerializerOptions(); options.WriteIndented = true;
        File.WriteAllText(FullFileName, JsonSerializer.Serialize(jsonMails, options)); 
    }

    private static void AppendToSQLDtb(string Username, string Password, string Database,  string Address, List<WebsiteEmail> EmailsList, List<string> VisitedDomains)
    {
        string connectionString = $"server={Address};uid={Username};pwd={Password};database={Database};Convert Zero Datetime=True";
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();
            StringBuilder command = new StringBuilder();
            command.AppendLine($"INSERT INTO mails (mail, website) VALUES");

            for (int i = 0; i < EmailsList.Count; i++)
            {
                command.Append($"(@mail{i}, @website{i})");
                if (i == EmailsList.Count - 1) { command.Append(";"); }
                else { command.Append(","); }
            }
            MySqlCommand cmd = new MySqlCommand(command.ToString(), conn);
            for (int i = 0; i < EmailsList.Count; i++)
            {
                cmd.Parameters.AddWithValue($"@mail{i}", EmailsList[i].Email);
                cmd.Parameters.AddWithValue($"@website{i}", EmailsList[i].URL);
            }
            try { cmd.ExecuteNonQuery(); }
            catch (Exception ex) { Console.WriteLine(ex); }

            foreach (string domain in VisitedDomains)
            {
                cmd = new MySqlCommand("INSERT INTO visiteddomains VALUES (@domain, @date);", conn);
                cmd.Parameters.AddWithValue("@domain", domain);
                cmd.Parameters.AddWithValue("@date", DateTime.Now);
                try { cmd.ExecuteNonQuery(); }
                catch (Exception ex) { Console.WriteLine(ex); }
            }
        }
    }

    public class ApplicationOptions
    {
        public ApplicationOptions(string Type) { this.Type = Type; }

        public string Type { get; set; }
        public string Domain { get; set; } = string.Empty;
        public string DomainList { get; set; } = string.Empty;
        public int Depth { get; set; } = -1;
        public int MaxMails { get; set; } = -1;
        public string OutputFile { get; set; } = string.Empty;
        public bool SubDomains { get; set; } = true;
        public int Threads { get; set; } = 1;
        public string Filter { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}