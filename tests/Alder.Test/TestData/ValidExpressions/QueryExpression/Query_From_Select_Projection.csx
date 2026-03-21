var list = new[] { 1, 2, 3 };
return (from x in list select x * 2).ToList();
