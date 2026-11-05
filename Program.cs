using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        // URL can be passed through command-line arguments.
        string url = args.Length > 0
            ? args[0]
            : "https://app.cloudqa.io/home/AutomationPracticeForm";

        using IWebDriver driver = new ChromeDriver();

        try
        {
            driver.Manage().Window.Size =
                new System.Drawing.Size(1280, 900);

            driver.Navigate().GoToUrl(url);

            WebDriverWait wait = new WebDriverWait(
                driver, TimeSpan.FromSeconds(15));

            wait.Until(d =>
                d.FindElements(By.TagName("body")).Count > 0);

            Console.WriteLine("Page loaded: " + url);

            // =====================================
            // 1. FIRST NAME TEXTBOX
            // =====================================

            IWebElement firstName =
                SmartLocator.FindTextBox(driver, "First Name");

            firstName.Clear();
            firstName.SendKeys("Suchi");

            bool namePassed =
                firstName.GetAttribute("value") == "Suchi";

            Console.WriteLine("First Name entered: Suchi");


            // =====================================
            // 2. GENDER RADIO BUTTON
            // =====================================

            SmartLocator.SelectRadio(driver, "Female");

            bool genderPassed =
                SmartLocator.IsRadioSelected(driver, "Female");

            Console.WriteLine("Gender selected: Female");


            // =====================================
            // 3. STATE DROPDOWN
            // =====================================

            bool statePassed = false;
            string selectedState = "Not selected";

            try
            {
                // Find the dropdown by the visible label "State"
                IWebElement stateDropdown =
                    SmartLocator.FindDropdown(driver, "State");

                SelectElement stateSelect =
                    new SelectElement(stateDropdown);

                // Find a valid option, excluding placeholder options
                var validOption = stateSelect.Options
                    .FirstOrDefault(option =>
                        !string.IsNullOrWhiteSpace(option.Text) &&
                        !SmartLocator.IsPlaceholder(option.Text));

                if (validOption != null)
                {
                    stateSelect.SelectByText(validOption.Text);

                    // Re-find the dropdown before verification
                    stateDropdown =
                        SmartLocator.FindDropdown(driver, "State");

                    stateSelect = new SelectElement(stateDropdown);

                    selectedState =
                        stateSelect.SelectedOption.Text.Trim();

                    statePassed =
                        !string.IsNullOrWhiteSpace(selectedState) &&
                        !SmartLocator.IsPlaceholder(selectedState);

                    Console.WriteLine(
                        "State dropdown option selected: " +
                        selectedState);
                }
                else
                {
                    Console.WriteLine(
                        "State: FAIL - no valid options found");
                }
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine(
                    "State: FAIL - labelled dropdown not found");
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine(
                    "State: FAIL - dropdown or option not found");
            }


            // =====================================
            // 4. TEST RESULTS
            // =====================================

            Console.WriteLine("\n----- Test Results -----");

            Console.WriteLine(
                "First Name: " +
                (namePassed ? "PASS" : "FAIL"));

            Console.WriteLine(
                "Gender: " +
                (genderPassed ? "PASS" : "FAIL"));

            Console.WriteLine(
                "State: " +
                (statePassed ? "PASS" : "FAIL"));

            bool allPassed =
                namePassed &&
                genderPassed &&
                statePassed;

            Console.WriteLine(
                "\nOverall Result: " +
                (allPassed ? "PASS" : "FAIL"));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed: " + ex.Message);
        }
        finally
        {
            Console.WriteLine("\nPress ENTER to close the browser.");
            Console.ReadLine();
        }
    }
}


// =====================================================
// SMART LOCATOR HELPER
// =====================================================

static class SmartLocator
{
    // -------------------------------------
    // Find textbox using label and fallback attributes
    // -------------------------------------

    public static IWebElement FindTextBox(
        IWebDriver driver,
        string fieldName)
    {
        WebDriverWait wait = new WebDriverWait(
            driver, TimeSpan.FromSeconds(10));

        return wait.Until(d =>
        {
            // Strategy 1: visible label
            IWebElement? field =
                FindByLabel(d, fieldName, "input");

            if (IsUsable(field))
                return field!;

            // Strategy 2: accessible name or placeholder
            var inputs = d.FindElements(By.TagName("input"));

            foreach (var input in inputs)
            {
                if (!IsUsable(input))
                    continue;

                string type =
                    input.GetAttribute("type") ?? "";

                if (type.Equals("hidden",
                    StringComparison.OrdinalIgnoreCase) ||
                    type.Equals("radio",
                    StringComparison.OrdinalIgnoreCase) ||
                    type.Equals("checkbox",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string ariaLabel =
                    input.GetAttribute("aria-label") ?? "";

                string placeholder =
                    input.GetAttribute("placeholder") ?? "";

                if (Matches(ariaLabel, fieldName) ||
                    Matches(placeholder, fieldName))
                {
                    return input;
                }
            }

            return null;
        });
    }


    // -------------------------------------
    // Find dropdown using label and fallback attributes
    // -------------------------------------

    public static IWebElement FindDropdown(
        IWebDriver driver,
        string fieldName)
    {
        WebDriverWait wait = new WebDriverWait(
            driver, TimeSpan.FromSeconds(10));

        return wait.Until(d =>
        {
            // Strategy 1: visible label association
            IWebElement? dropdown =
                FindByLabel(d, fieldName, "select");

            if (IsUsable(dropdown))
                return dropdown!;

            // Strategy 2: accessible attributes
            var selects = d.FindElements(By.TagName("select"));

            foreach (var select in selects)
            {
                if (!IsUsable(select))
                    continue;

                string ariaLabel =
                    select.GetAttribute("aria-label") ?? "";

                string title =
                    select.GetAttribute("title") ?? "";

                if (Matches(ariaLabel, fieldName) ||
                    Matches(title, fieldName))
                {
                    return select;
                }
            }

            // Strategy 3: nearby container text
            foreach (var select in selects)
            {
                if (!IsUsable(select))
                    continue;

                try
                {
                    IWebElement parent =
                        select.FindElement(By.XPath(".."));

                    if (Matches(parent.Text, fieldName))
                        return select;
                }
                catch (NoSuchElementException)
                {
                    // Continue searching other candidates
                }
            }

            return null;
        });
    }


    // -------------------------------------
    // Select radio by visible label
    // -------------------------------------

    public static void SelectRadio(
        IWebDriver driver,
        string optionName)
    {
        WebDriverWait wait = new WebDriverWait(
            driver, TimeSpan.FromSeconds(10));

        bool selected = wait.Until(d =>
        {
            var labels = d.FindElements(By.TagName("label"));

            foreach (var label in labels)
            {
                if (!Matches(label.Text, optionName))
                    continue;

                string? inputId =
                    label.GetAttribute("for");

                // Strategy 1: label's "for" association
                if (!string.IsNullOrWhiteSpace(inputId))
                {
                    var radio = d.FindElements(By.Id(inputId))
                        .FirstOrDefault();

                    if (radio != null &&
                        radio.GetAttribute("type") == "radio")
                    {
                        if (!radio.Selected)
                            label.Click();

                        return radio.Selected;
                    }
                }

                // Strategy 2: radio nested inside label
                var nestedRadio = label.FindElements(
                    By.XPath(".//input[@type='radio']"))
                    .FirstOrDefault();

                if (nestedRadio != null)
                {
                    if (!nestedRadio.Selected)
                        label.Click();

                    return nestedRadio.Selected;
                }
            }

            // Strategy 3: radio accessible attributes
            var radios = d.FindElements(
                By.XPath("//input[@type='radio']"));

            foreach (var radio in radios)
            {
                string value =
                    radio.GetAttribute("value") ?? "";

                string ariaLabel =
                    radio.GetAttribute("aria-label") ?? "";

                if (Matches(value, optionName) ||
                    Matches(ariaLabel, optionName))
                {
                    if (!radio.Selected)
                        radio.Click();

                    return radio.Selected;
                }
            }

            return false;
        });

        if (!selected)
        {
            throw new NoSuchElementException(
                "Could not select radio option: " + optionName);
        }
    }


    // -------------------------------------
    // Verify specific radio option
    // -------------------------------------

    public static bool IsRadioSelected(
        IWebDriver driver,
        string optionName)
    {
        var labels = driver.FindElements(By.TagName("label"));

        foreach (var label in labels)
        {
            if (!Matches(label.Text, optionName))
                continue;

            string? inputId =
                label.GetAttribute("for");

            if (!string.IsNullOrWhiteSpace(inputId))
            {
                var radio = driver.FindElements(By.Id(inputId))
                    .FirstOrDefault();

                if (radio != null)
                    return radio.Selected;
            }

            var nestedRadio = label.FindElements(
                By.XPath(".//input[@type='radio']"))
                .FirstOrDefault();

            if (nestedRadio != null)
                return nestedRadio.Selected;
        }

        return driver.FindElements(
                By.XPath("//input[@type='radio']"))
            .Any(radio =>
                radio.Selected &&
                Matches(
                    radio.GetAttribute("value"),
                    optionName));
    }


    // -------------------------------------
    // Find field through visible label
    // -------------------------------------

    private static IWebElement? FindByLabel(
        IWebDriver driver,
        string labelText,
        string tagName)
    {
        var labels = driver.FindElements(By.TagName("label"));

        foreach (var label in labels)
        {
            if (!Matches(label.Text, labelText))
                continue;

            // Strategy 1: label "for" attribute
            string? inputId =
                label.GetAttribute("for");

            if (!string.IsNullOrWhiteSpace(inputId))
            {
                var field = driver.FindElements(By.Id(inputId))
                    .FirstOrDefault();

                if (field != null &&
                    field.TagName.Equals(
                        tagName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return field;
                }
            }

            // Strategy 2: field nested inside label
            var nestedField = label.FindElements(
                By.XPath(".//" + tagName))
                .FirstOrDefault();

            if (nestedField != null)
                return nestedField;

            // Strategy 3: field in the label's nearby container
            try
            {
                IWebElement parent =
                    label.FindElement(By.XPath(".."));

                var nearbyField = parent.FindElements(
                    By.TagName(tagName))
                    .FirstOrDefault();

                if (nearbyField != null)
                    return nearbyField;
            }
            catch (NoSuchElementException)
            {
                // Continue searching
            }
        }

        return null;
    }


    // -------------------------------------
    // Utility: element usable?
    // -------------------------------------

    private static bool IsUsable(IWebElement? element)
    {
        return element != null &&
               element.Displayed &&
               element.Enabled;
    }


    // -------------------------------------
    // Utility: compare text flexibly
    // -------------------------------------

    private static bool Matches(
        string? actual,
        string expected)
    {
        if (string.IsNullOrWhiteSpace(actual))
            return false;

        string normalizedActual =
            Regex.Replace(actual.Trim(), @"\s+", " ");

        string normalizedExpected =
            Regex.Replace(expected.Trim(), @"\s+", " ");

        return string.Equals(
            normalizedActual,
            normalizedExpected,
            StringComparison.OrdinalIgnoreCase);
    }


    // -------------------------------------
    // Utility: ignore placeholder options
    // -------------------------------------

    public static bool IsPlaceholder(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        string normalized =
            text.Trim().ToLowerInvariant();

        return normalized.Contains("select") ||
               normalized.Contains("choose") ||
               normalized.Contains("please select") ||
               normalized == "--";
    }
}