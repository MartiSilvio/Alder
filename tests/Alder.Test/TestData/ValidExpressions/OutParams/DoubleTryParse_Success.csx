bool ok = double.TryParse("3.14", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double result);
return ok && result == 3.14;
