return DateTime.TryParse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var result) && result.Year == 2024;
