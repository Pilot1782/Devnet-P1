using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

IWebDriver driver = new ChromeDriver();

driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(500);

driver.Navigate().GoToUrl("https://www.selenium.dev/selenium/web/web-form.html");
