// Calling .Value on null nullable throws InvalidOperationException
double? d = null;
return d.Value;
