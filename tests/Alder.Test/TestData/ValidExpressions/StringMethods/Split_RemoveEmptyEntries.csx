var parts = "a,,b,,c".Split(',', StringSplitOptions.RemoveEmptyEntries);
return parts.Length == 3 && parts[0] == "a" && parts[2] == "c";
