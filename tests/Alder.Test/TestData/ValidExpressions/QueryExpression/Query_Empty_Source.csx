var list = new int[0];
return (from x in list where x > 0 select x * 2).ToList();
