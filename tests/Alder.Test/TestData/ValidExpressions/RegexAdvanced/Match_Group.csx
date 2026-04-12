var m = System.Text.RegularExpressions.Regex.Match("year=2024", @"(\d+)");
return m.Groups[1].Value;
