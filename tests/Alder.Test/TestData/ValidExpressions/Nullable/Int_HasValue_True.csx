// §8.3.12: A non-null int? has HasValue = true.
int? x = 42;
return x.HasValue && x.Value == 42;
