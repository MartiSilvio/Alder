var items = new[] {
    "user@example.com",
    "https://github.com/project",
    "+1 555-867-5309",
    "2024-03-15",
    "42.7",
    "hello world",
    "admin@corp.co.uk",
    "http://localhost:3000",
    "not-a-date",
    "-100",
    "test@test",
    "ftp://files.example.org"
};

var emailCount = 0;
var urlCount = 0;
var phoneCount = 0;
var dateCount = 0;
var numberCount = 0;
var textCount = 0;
var classifications = "";

foreach (var str in items)
{
    var category = "text";

    if (System.Text.RegularExpressions.Regex.IsMatch(str, @"^[\w.+-]+@[\w-]+\.[\w.]+$"))
        category = "email";
    else if (System.Text.RegularExpressions.Regex.IsMatch(str, @"^https?://"))
        category = "url";
    else if (System.Text.RegularExpressions.Regex.IsMatch(str, @"^\+?\d[\d\s-]{7,}\d$"))
        category = "phone";
    else if (System.Text.RegularExpressions.Regex.IsMatch(str, @"^\d{4}-\d{2}-\d{2}$"))
        category = "date";
    else if (System.Text.RegularExpressions.Regex.IsMatch(str, @"^-?\d+(\.\d+)?$"))
        category = "number";

    if (category == "email") emailCount++;
    else if (category == "url") urlCount++;
    else if (category == "phone") phoneCount++;
    else if (category == "date") dateCount++;
    else if (category == "number") numberCount++;
    else textCount++;

    classifications += category + ";";
}

var nonTextCount = items.Length - textCount;
var hasUrls = urlCount > 0;
var noPhones = phoneCount == 0;

var result = $"total={items.Length}|email={emailCount}|url={urlCount}|";
result += $"phone={phoneCount}|date={dateCount}|number={numberCount}|text={textCount}|";
result += $"nonText={nonTextCount}|hasUrls={hasUrls}|";
result += $"classes={classifications}";

return result;
