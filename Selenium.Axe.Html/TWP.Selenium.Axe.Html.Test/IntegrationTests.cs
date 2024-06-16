using Deque.AxeCore.Commons;
using Deque.AxeCore.Selenium;
using HtmlAgilityPack;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Events;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;

using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

// Setup parallelization
[assembly: Parallelizable(ParallelScope.All)]

namespace TWP.Selenium.Axe.Html.Test
{
    [TestFixture]
    [Category("Integration")]
    public class IntegrationTests
    {
        private readonly ConcurrentDictionary<string, IWebDriver> localDriver = new ConcurrentDictionary<string, IWebDriver>();
        private readonly ConcurrentDictionary<string, WebDriverWait> localWaitDriver = new ConcurrentDictionary<string, WebDriverWait>();
        private static string ChromeDriverPath = null;

        public IWebDriver WebDriver
        {
            get
            {
                return localDriver[GetFullyQualifiedTestName()];
            }

            set
            {
                localDriver.AddOrUpdate(GetFullyQualifiedTestName(), value, (oldkey, oldvalue) => value);
            }
        }

        public WebDriverWait Wait
        {
            get
            {
                return localWaitDriver[GetFullyQualifiedTestName()];
            }

            set
            {
                localWaitDriver.AddOrUpdate(GetFullyQualifiedTestName(), value, (oldkey, oldvalue) => value);
            }
        }

        private static readonly string TestFileRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string IntegrationTestTargetSimpleFile = Path.Combine(TestFileRoot, @"integration-test-simple.html");
        private static readonly string IntegrationTestTargetComplexTargetsFile = Path.Combine(TestFileRoot, @"integration-test-target-complex.html");

        private const string mainElementSelector = "main";

        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                WebDriver?.Close();
            }
            finally
            {
                WebDriver?.Quit();
            }

        }

        [Test]
        public void ReportFullPage()
        {
            string path = CreateReportPath();
            InitDriver();
            LoadSimpleTestPage();

            Wait.Until(drv => drv.FindElement(By.TagName(mainElementSelector)));

            WebDriver.CreateAxeHtmlReport(path);

            ValidateReport(path, 4, 27, 0, 70);
        }

        [Test]
        public void ReportFullPageViolationsOnly()
        {
            string path = CreateReportPath();
            InitDriver();
            LoadSimpleTestPage();

            Wait.Until(drv => drv.FindElement(By.TagName(mainElementSelector)));

            WebDriver.CreateAxeHtmlReport(path, ReportTypes.Violations);

            ValidateReport(path, 4, 0);
            ValidateResultNotWritten(path, ReportTypes.Passes | ReportTypes.Incomplete | ReportTypes.Inapplicable);
        }

        [Test]
        public void ReportFullPagePassesInapplicableViolationsOnly()
        {
            string path = CreateReportPath();
            InitDriver();
            LoadSimpleTestPage();

            Wait.Until(drv => drv.FindElement(By.TagName(mainElementSelector)));
            WebDriver.CreateAxeHtmlReport(path, ReportTypes.Passes | ReportTypes.Inapplicable | ReportTypes.Violations);

            ValidateReport(path, 4, 27, 0, 70);
            ValidateResultNotWritten(path, ReportTypes.Incomplete);
        }

        [Test]
        public void ReportOnElement()
        {
            string path = CreateReportPath();
            InitDriver();
            LoadSimpleTestPage();

            var mainElement = Wait.Until(drv => drv.FindElement(By.CssSelector(mainElementSelector)));
            WebDriver.CreateAxeHtmlReport(mainElement, path);

            ValidateReport(path, 3, 15, 0, 76);
        }

        [Test]
        public void ReportOnElementEventFiring()
        {
            string path = CreateReportPath();
            InitDriver();
            LoadSimpleTestPage();

            WebDriver = new EventFiringWebDriver(WebDriver);
            Wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(20));

            var mainElement = Wait.Until(drv => drv.FindElement(By.CssSelector(mainElementSelector)));
            WebDriver.CreateAxeHtmlReport(mainElement, path);

            ValidateReport(path, 3, 15, 0, 76);
        }

        [Test]
        public void ReportRespectRules()
        {
            string path = CreateReportPath();
            InitDriver();
            LoadSimpleTestPage();
            Wait.Until(drv => drv.FindElement(By.CssSelector(mainElementSelector)));

            var builder = new AxeBuilder(WebDriver).DisableRules("color-contrast");
            WebDriver.CreateAxeHtmlReport(builder.Analyze(), path);

            ValidateReport(path, 3, 22, 0, 70);
        }

        [Test]
        public void ReportRespectsIframeImplicitTrue()
        {
            string path = CreateReportPath();
            string filename = new Uri(Path.GetFullPath(IntegrationTestTargetComplexTargetsFile)).AbsoluteUri;

            InitDriver();
            WebDriver.Navigate().GoToUrl(filename);
            Wait.Until(drv => drv.FindElement(By.CssSelector(mainElementSelector)));

            var builder = new AxeBuilder(WebDriver);

            WebDriver.CreateAxeHtmlReport(builder.Analyze(), path);

            ValidateReport(path, 4, 44, 0, 66);
        }

        [Test]
        public void ReportRespectsIframeTrue()
        {
            string path = CreateReportPath();
            string filename = new Uri(Path.GetFullPath(IntegrationTestTargetComplexTargetsFile)).AbsoluteUri;

            InitDriver();
            WebDriver.Navigate().GoToUrl(filename);
            Wait.Until(drv => drv.FindElement(By.CssSelector(mainElementSelector)));

            AxeRunOptions runOptions = new AxeRunOptions
            {
                Iframes = true
            };

            var builder = new AxeBuilder(WebDriver).WithOptions(runOptions);

            WebDriver.CreateAxeHtmlReport(builder.Analyze(), path);

            ValidateReport(path, 4, 44, 0, 66);
        }

        [Test]
        public void ReportRespectsIframeFalse()
        {
            string path = CreateReportPath();
            string filename = new Uri(Path.GetFullPath(IntegrationTestTargetComplexTargetsFile)).AbsoluteUri;

            InitDriver();
            WebDriver.Navigate().GoToUrl(filename);
            Wait.Until(drv => drv.FindElement(By.CssSelector(mainElementSelector)));

            AxeRunOptions runOptions = new AxeRunOptions
            {
                Iframes = false
            };

            var builder = new AxeBuilder(WebDriver).WithOptions(runOptions);

            WebDriver.CreateAxeHtmlReport(builder.Analyze(), path);

            ValidateReport(path, 1, 24, 1, 67);
        }

        private string CreateReportPath()
        {
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().Location;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.Combine(Path.GetDirectoryName(path), Guid.NewGuid() + ".html");
        }

        private void ValidateReport(string path, int violationCount, int passCount, int incompleteCount = 0, int inapplicableCount = 0)
        {
            string text = File.ReadAllText(path);
            var doc = new HtmlDocument();
            doc.LoadHtml(text);

            // Check violations 
            string xpath = ".//*[@id=\"ViolationsSection\"]//*[contains(concat(\" \",normalize-space(@class),\" \"),\"htmlTable\")]";
            ValidateElementCount(doc, violationCount, xpath, ResultType.Violations);

            // Check passes
            xpath = ".//*[@id=\"PassesSection\"]//*[contains(concat(\" \",normalize-space(@class),\" \"),\"htmlTable\")]";
            ValidateElementCount(doc, passCount, xpath, ResultType.Passes);

            // Check inapplicables
            xpath = ".//*[@id=\"InapplicableSection\"]//*[contains(concat(\" \",normalize-space(@class),\" \"),\"findings\")]";
            ValidateElementCount(doc, inapplicableCount, xpath, ResultType.Inapplicable);

            // Check incompletes
            xpath = ".//*[@id=\"IncompleteSection\"]//*[contains(concat(\" \",normalize-space(@class),\" \"),\"htmlTable\")]";
            ValidateElementCount(doc, incompleteCount, xpath, ResultType.Incomplete);

            // Check header data
            Assert.IsTrue(text.Contains("Using: axe-core"), "Expected to find 'Using: axe-core'");

            if (!violationCount.Equals(0))
            {
                ValidateResultCount(text, violationCount, ResultType.Violations);
            }

            if (!passCount.Equals(0))
            {
                ValidateResultCount(text, passCount, ResultType.Passes);
            }

            if (!inapplicableCount.Equals(0))
            {
                ValidateResultCount(text, inapplicableCount, ResultType.Inapplicable);
            }

            if (!incompleteCount.Equals(0))
            {
                ValidateResultCount(text, incompleteCount, ResultType.Incomplete);
            }
        }

        private void ValidateElementCount(HtmlDocument doc, int count, string xpath, ResultType resultType)
        {
            HtmlNodeCollection liNodes = doc.DocumentNode.SelectNodes(xpath) ?? new HtmlNodeCollection(null);
            Assert.AreEqual(count, liNodes.Count, $"Expected {count} {resultType}");
        }

        private void ValidateResultCount(string text, int count, ResultType resultType)
        {
            Assert.IsTrue(text.Contains($"{resultType}: {count}"), $"Expected to find '{resultType}: {count}'");
        }

        private void ValidateResultNotWritten(string path, ReportTypes ReportType)
        {
            string text = File.ReadAllText(path);

            foreach (string resultType in ReportType.ToString().Split(','))
            {
                Assert.IsFalse(text.Contains($"{resultType}: "), $"Expected to not find '{resultType}: '");
            }
        }

        private void LoadSimpleTestPage()
        {
            var filename = new Uri(IntegrationTestTargetSimpleFile).AbsoluteUri;
            WebDriver.Navigate().GoToUrl(filename);

            Wait.Until(drv => drv.FindElement(By.TagName(mainElementSelector)));
        }

        private void InitDriver()
        {
            LazyInitializer.EnsureInitialized(ref ChromeDriverPath, () => new DriverManager().SetUpDriver(new ChromeConfig()));

            ChromeOptions options = new ChromeOptions
            {
                UnhandledPromptBehavior = UnhandledPromptBehavior.Accept,
            };
            options.AddArgument("no-sandbox");
            options.AddArgument("--log-level=3");
            options.AddArgument("--silent");
            options.AddArgument("--allow-file-access-from-files");

            ChromeDriverService service = ChromeDriverService.CreateDefaultService(Path.GetDirectoryName(ChromeDriverPath));
            service.SuppressInitialDiagnosticInformation = true;
            WebDriver = new ChromeDriver(ChromeDriverService.CreateDefaultService(), options);

            Wait = new WebDriverWait(WebDriver, TimeSpan.FromSeconds(20));
            WebDriver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(20);
            WebDriver.Manage().Window.Maximize();
        }

        private static string GetFullyQualifiedTestName()
        {
            return TestContext.CurrentContext.Test.FullName;
        }
    }
}
