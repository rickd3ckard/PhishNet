
# PhishNet 

PhishNet is an opensource dotnet application written in c# which purpose is to mass-harvest email from the internet by crawling the website content fetched with HTTP request. 
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
| `-o` | Number of threads in the thread pool| Number of threads | int |
| `-f` | Filter for the allowed domains exentions | Domain extension | string |
| `-username` | Username for the SQL database | Username | string |
| `-password` | Password for the SQL database | Password | string |
| `-database` | SQL Database name | Database | string |
| `-address ` | Address of the SQL server | Address | string |

## Command Examples
## Sql Database set up

