var mixed = new object[] { 1, "a", 2, "b", 3 };
return mixed.OfType<int>().Count();
