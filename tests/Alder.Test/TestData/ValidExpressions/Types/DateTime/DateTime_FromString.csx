string s = "2023-06-20";
DateTime dt = DateTime.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
return dt.Year + dt.Month + dt.Day;
