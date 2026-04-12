var parts = "a,b;c|d".Split(new[] { ',', ';', '|' });
return parts.Length == 4 && parts[3] == "d";
