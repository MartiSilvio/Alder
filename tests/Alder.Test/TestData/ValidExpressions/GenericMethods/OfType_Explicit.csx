object[] mixed = new object[] { 1, "two", 3, "four", 5 };
return mixed.OfType<int>().Count();
