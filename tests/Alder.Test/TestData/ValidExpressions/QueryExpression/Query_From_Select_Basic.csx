var list = new[] { 1, 2, 3, 4, 5 };
return (from x in list where x > 3 select x).ToList();
