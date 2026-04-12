TimeSpan a = TimeSpan.FromHours(3);
TimeSpan b = TimeSpan.FromMinutes(30);
return a.Subtract(b).TotalMinutes;
