var parts = "a,b,c".Split(',');
return parts.Length == 3 && parts[0] == "a" && parts[1] == "b" && parts[2] == "c";
