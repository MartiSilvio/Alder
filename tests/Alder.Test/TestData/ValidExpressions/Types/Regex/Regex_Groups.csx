var match = System.Text.RegularExpressions.Regex.Match("2024-01-15", @"(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})");
return match.Groups["month"].Value;
