bool ok = decimal.TryParse("123.45", System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal result);
return ok && result == 123.45m;
