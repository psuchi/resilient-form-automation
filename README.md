# Resilient Form Automation

A C# Selenium automation project that interacts with form elements on the CloudQA Automation Practice Form.

## Objective

Automate the following form interactions while avoiding reliance on fixed element IDs, names, classes, or page positions:

- Enter a first name
- Select a gender radio button
- Select an option from the State dropdown
- Verify that each interaction was successful

## Technologies Used

- C#
- .NET 8
- Selenium WebDriver
- Google Chrome

## Resilient Locator Strategy

The automation uses multiple ways to identify form elements, including:

- Visible labels and associated form controls
- Accessible attributes such as `aria-label`
- Placeholder and title attributes
- Nearby field text
- Fallback matching when a preferred locator is unavailable

This approach aims to reduce dependence on a single HTML attribute or fixed page structure.

## Configuration

The target URL can be supplied as a command-line argument. If no URL is provided, the application uses its default practice-form URL.

## Run the Project

Install the .NET 8 SDK and Google Chrome.

```bash
dotnet restore
dotnet run
