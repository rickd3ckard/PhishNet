/* 
* Public domain software, no restrictions. 
* Released by rickd3ckard: https://github.com/rickd3ckard 
* See: https://unlicense.org/
*/

using System.Text.RegularExpressions;

namespace E_Crawl_CSharp
{
    public class Crawler
    {
        public Crawler(string DomainName, int Depth, int TargetMailCount, bool IncludeSubDomains, string Filter)
        {
            this.DomainName = DomainName;
            this.Depth = Depth;          
            this.TargetMailCount = TargetMailCount;
            this.IncludeSubDomains = IncludeSubDomains;
            this.Filter = Filter;

            this.ExternalDomains = new List<string>();
            this.EMails = new List<WebsiteEmail>();
            this.VisitedURLs = new List<string>();
            this.Completed = false;

            this.ExternalDomains.Add(DomainName);
        }

        public List<WebsiteEmail> EMails { get; }
        public bool Completed  { get; }
        public string DomainName { get; }
        public int Depth { get; }
        public List<string> VisitedURLs { get; }
        public int TargetMailCount { get; }
        public bool IncludeSubDomains { get; }
        public List<string> ExternalDomains { get; }
        public string Filter { get; }

        public async Task<List<WebsiteEmail>> Execute()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("image/webp,*/*;q=0.8"); ;
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            client.DefaultRequestHeaders.Referrer = new Uri("https://www.maisonsmoches.be/");

            await recursivequeue(client, this.Depth, this.DomainName);
            return this.EMails;
        }

        private Queue<WebsiteURL> _queue = new Queue<WebsiteURL>();
        private async Task recursivequeue(HttpClient Client, int MaxDepth, string targetDomain)
        {
            _queue.Enqueue(new WebsiteURL(targetDomain, 1));
            this.VisitedURLs.Add(targetDomain);

            while (_queue.Count > 0)
            {
                WebsiteURL targetURL = _queue.Dequeue();                                      
                string queryresponse = string.Empty;
                try 
                {
                    CancellationTokenSource token = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    queryresponse = await Client.GetStringAsync(targetURL.URL, token.Token);
                }
                catch { continue; }

                if (targetURL.Depth + 1 <= MaxDepth) {getURLsFromPage(queryresponse, targetURL.Depth); }
                
                // Scrape e-mails on the page
                CancellationTokenSource timeOutToken = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                Task<string[]> getMailsTask = Task.Run(() => getMailsFromPage(queryresponse));
                Task completedTask = await Task.WhenAny(getMailsTask, Task.Delay(Timeout.Infinite, timeOutToken.Token));
                if (completedTask != getMailsTask) 
                {
                    //Console.ForegroundColor = ConsoleColor.Red;
                    //Console.WriteLine("[ERR0R] Mail regex function timed out (10s).");
                    //Console.ResetColor(); continue;
                }
               
                string[] mails = getMailsTask.Result;
                if (mails.Length == 0) { continue; }

                foreach (string mail in mails)
                {
                    if (string.IsNullOrWhiteSpace(mail)) { continue; }
                    string cleanmail = mail;
                    if (cleanmail.StartsWith("mailto:")) { cleanmail = cleanmail.Substring(7); }
                    string[] allowedExtentions = [".be", ".com", ".eu", ".net", ".org", ".fr", ".ch", ".de", ".nl", ".it"];
                    string mailExtension = cleanmail.Substring(cleanmail.LastIndexOf('.'));
                    if (!allowedExtentions.Contains(mailExtension)) { continue; }
                    if (!this.EMails.Any(e => e.Email == cleanmail)) { this.EMails.Add(new WebsiteEmail(cleanmail, targetURL.URL)); }
                    if (this.TargetMailCount == EMails.Count) { return; }
                }
            }
        }

        private void getURLsFromPage(string Text, int Depth) {
            Regex hrefregex = new Regex("href=[\"\\'](.*?)[\"\\']");
            MatchCollection hrefs = hrefregex.Matches(Text);

            foreach (Match match in hrefs)
            {
                string href = match.Value;
                if (href.StartsWith("//")) { continue; } // invalid
                if (href.StartsWith("href=")) { href = match.Value.ToString().Substring("href=".Length + 1, match.Value.ToString().Length - 1 - ("href=".Length + 1)); } // remove href=
                if ((href.StartsWith("https://") || href.StartsWith("http://")) && !href.StartsWith(this.DomainName)) { // correct format + not in the current domain (external link)
                    if (!string.IsNullOrWhiteSpace(this.Filter)) { if (!href.Contains(this.Filter)) { continue; } }
                    appendNewDomain(href); continue; } // append to external links
                if (href.StartsWith("/")) { href = this.DomainName.Substring(0, this.DomainName.Length - 1) + href; } // format properly        
                if (!href.StartsWith(this.DomainName)) { continue; } // invalid
                if (!this.VisitedURLs.Contains(href)) {
                    VisitedURLs.Add(href);
                    _queue.Enqueue(new WebsiteURL(href, Depth + 1));             
                }
            }
        }

        private string[] getMailsFromPage(string text)
        {
            Regex emailregex = new Regex(@"(?:mailto:)?([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})");
            MatchCollection mails = emailregex.Matches(text);

            List<string> emails = new List<string>();
            foreach (Match mail in mails) { emails.Add(mail.Value); }
            return emails.ToArray();
        }

        private void appendNewDomain(string href)
        {
            try
            {
                Uri domainUri = new Uri(href);
                string domainroot = $"{domainUri.Scheme}://{domainUri.Host}/";

                if (!this.IncludeSubDomains) 
                { 
                    if (domainUri.Host.Split('.').Length > 3) { return; }
                    string domain = domainUri.Host.Split('.')[1];
                    if (this.ExternalDomains.Any(e => e.Contains(domain))) { return; };
                }

                if (!this.ExternalDomains.Contains(domainroot)) { this.ExternalDomains.Add(domainroot); }
            }
            catch { }               
        }
    } 

    public class WebsiteURL
    {
        public WebsiteURL(string URL, int Depth)
        {
            this.URL = URL;
            this.Depth = Depth;
        }

        public string URL { get; set; }
        public int Depth { get; set; }
    }

    public class WebsiteEmail
    {
        public WebsiteEmail(string Email, string URL)
        {
            this.Email = Email;
            this.URL = URL;
        }
        
        public string Email { get; }
        public string URL { get; }   
    
    }
}