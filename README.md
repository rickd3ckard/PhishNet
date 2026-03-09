
# PhishNet 

<p align="justify">
PhishNet is an opensource dotnet application written in c# which purpose is to mass-harvest email from the internet by crawling the website content fetched with HTTP request. 
</p>
  
## Installation and usage
Fetch the github repository and navigate to the PhishNet project folder, then run the console app using dotnet :
```bash
git clone https://github.com/rickd3ckard/PhishNet.git
```
```bash
cd PhishNet -> cd PhishNet
```
```bash
dotnet run -- domain https://www.era.be/fr 
```

### Commands
Here find  a list of the different possible commands:
| Command | Description | Argument |Type |
|-----|---------------------------|--------|---|
| `domain` | Scrape a single domain url | Domain URL | string |
| `domains` | Scrape multiple domains url from a text file list | Text file path | string |
| `help` | Display help for the application |   |

### Modifiers 
Here find  a list of the different possible modifiers:
| Modifier | Description | Argument | Type |
|-----|---------------------------|--------|---|
| `-d` | Max depth on a single website for the crawler | Max depth | int |
| `-m` | Max mails for a single website | Text file path | int |
| `-o` | Custom path of the output file | Path | string |
| `-sd` | Allow the crawling of sub domains | Allowed? | bool |
| `-t` | Number of threads in the thread pool| Number of threads | int |
| `-f` | Filter for the allowed domains exentions | Domain extension | string |
| `-username` | Username for the SQL database | Username | string |
| `-password` | Password for the SQL database | Password | string |
| `-database` | SQL Database name | Database | string |
| `-address ` | Address of the SQL server | Address | string |

<p align="justify">
Max depth represents the depth at which the crawler will dig into the website. A provided depth of 1 will only scrape the landing page of the domain. During the scraping of this page, internal links will be saved, but not used. If a depth of 2 is provided, the crawler will visit each internal link saved on the first page, and gather a new list of internal links that will not be used. 
A provided depth of <em>n</em> will recursively scrape the links provided in <em>n-1</em> until <em>n</em> is reached. 
A provided depth of -1 will recursively crawl links in <em>n-1</em> until the internal links count returned in <em>n-1</em> is 0. 
</p>

<p align="justify">
A sub domain is a part of a website that branches off from the main domain and functions as a separate host name. For example, given the domain <em>era.be</em>, the addresses <em>blog.era.be</em>, <em>shop.era.be</em>, and <em>dev.era.be</em> are all subdomains because they are derived from and associated with the main domain.
</p>

<p align="justify">
The number of threads roughly represents the maximum of domains being crawled in parallel. The crawler will detect and store external links found on the domain inside a domain queue. Once the crawling of the domain is completed, another domain will be dequeued from the list and crawled - this process is repeated indefinitely (until memory overflows). Each domain crawling routine is executed on a single thread. Increasing the number of threads in the thread pool allows multiple domains to be dequeued and crawled simultaneously until the thread pool is exhausted. When a thread finishes its task, it is returned to the thread pool and becomes available for another domain crawling routine.
</p>

<p align="justify">
The filter restrains the crawler on a specific domain extension. For example, if a filter value .be is provided, all the external links crawled will not be enqueued if the extension does not match the filter value.
</p>

## Command Examples
## Sql Database set up

