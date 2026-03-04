var list = new[] { 3, 1, 4, 1, 5, 9, 2, 6, 5 };
return (from x in list orderby x % 2, x select x).ToList();
