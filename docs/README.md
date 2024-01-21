# TWP.Selenium.Axe.Html for .NET

Automated web accessibility testing with .NET, C#, and Selenium. Wraps the [axe-core](https://github.com/dequelabs/axe-core) accessibility scanning engine and the [Selenium.WebDriver](https://www.seleniumhq.org/) browser automation framework.

Compatible with .NET Standard 2.0+, .NET Framework 4.5+, and .NET Core 2.0+.

## Getting Started

Install via NuGet:

```powershell
PM> Install-Package TWP.Selenium.Axe.Html
# or, use the Visual Studio "Manage NuGet Packages" UI
```

Import this namespace:

```csharp
using TWP.Selenium.Axe.Html;
```


## Creating a HTML report

Axe results can be published as a standalone HTML file.  The file contain fail, pass, incomplete and inapplicable test results.  It also contains a screenshot and scan meta data.

Run full report:

```csharp
IWebDriver webDriver = new ChromeDriver();
// Navigate to page
AxeResult results = new AxeBuilder(webDriver).Analyze();
string path = Path.Combine(GetDestFolder(), "AxeReport.html");

webDriver.CreateAxeHtmlReport(results, path);
```

Report on an element and all its children:

```csharp
IWebDriver webDriver = new ChromeDriver();
// Navigate to page
var mainElement = _wait.Until(drv => drv.FindElement(By.CssSelector("main")));
string path = Path.Combine(GetDestFolder(), "AxeReport.html");

webDriver.CreateAxeHtmlReport(mainElement, path);
```

Report with custom rules:

```csharp
IWebDriver webDriver = new ChromeDriver();
// Navigate to page
string path = Path.Combine(GetDestFolder(), "AxeReport.html");
var builder = new AxeBuilder(webDriver).DisableRules("color-contrast");

webDriver.CreateAxeHtmlReport(builder.Analyze(), path);
```

Report with only violations and passes:

```csharp
IWebDriver webDriver = new ChromeDriver();
// Navigate to page
AxeResult results = new AxeBuilder(webDriver).Analyze();
string path = Path.Combine(GetDestFolder(), "AxeReport.html");

webDriver.CreateAxeHtmlReport(path, ReportTypes.Violations | ReportTypes.Passes);
```

## Contributing

*Please note that this project is released with a [Contributor Code of Conduct](https://github.com/TroyWalshProf/SeleniumAxeHtmlDotnet/blob/master/CODE_OF_CONDUCT.md). By participating in this project you agree to abide by its terms.*

This project builds against a combination of .NET Standard, .NET Core, and .NET Framework targets.

Prerequisites to build:

* Install the [.NET Core SDK](https://dotnet.microsoft.com/download)
* Install either Visual Studio 2017+ *OR* the [.NET Framework Dev Pack](https://dotnet.microsoft.com/download)

Prerequisites to run integration tests:

* Install [Chrome](https://www.google.com/chrome/) (the version should match the version of the `Selenium.WebDriver.ChromeDriver` PackageReference in [TWP.Selenium.Axe.Html.Test.csproj](./Selenium.Axe.Html/TWP.Selenium.Axe.Html.Test/TWP.Selenium.Axe.Html.Test.csproj))

To build and run all tests:

```sh
cd ./Selenium.Axe.Html
dotnet restore
dotnet build
dotnet test
```
