// §8.3.12: GetValueOrDefault returns the wrapped value when present.
int? x = 7;
return x.GetValueOrDefault() == 7;
