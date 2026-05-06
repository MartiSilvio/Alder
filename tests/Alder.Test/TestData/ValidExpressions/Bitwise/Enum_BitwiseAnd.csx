var flags = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
return (flags & StringSplitOptions.TrimEntries) == StringSplitOptions.TrimEntries;
