string? a = null;
string? b = null;
string c = "found";
return (a ?? b ?? c).Length > 0 ? true : false;