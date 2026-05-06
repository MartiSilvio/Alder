// §19: Enum.TryParse<T>(string, out T) returns true on success.
return Enum.TryParse<DayOfWeek>("Friday", out var day) && day == DayOfWeek.Friday;
