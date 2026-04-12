// §8.3.12: .Value on a null nullable throws InvalidOperationException
int? x = null;
return x.Value;
