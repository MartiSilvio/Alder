var list = new[] { 3, 1, 4, 1, 5, 9 };
return (from x in list orderby x select x).ToList();
