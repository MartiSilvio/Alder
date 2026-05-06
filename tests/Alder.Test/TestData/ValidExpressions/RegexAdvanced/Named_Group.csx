var m = System.Text.RegularExpressions.Regex.Match("name=Alice", @"(?<key>\w+)=(?<value>\w+)");
return m.Groups["value"].Value;
