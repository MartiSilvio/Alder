var date = new DateTime(2026, 1, 1);
var later = date + TimeSpan.FromDays(30);
return later.Month;
