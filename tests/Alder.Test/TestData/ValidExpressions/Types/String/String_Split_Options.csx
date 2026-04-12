var parts = "a,,b,,c".Split(',', StringSplitOptions.RemoveEmptyEntries);
return string.Join("|", parts);
