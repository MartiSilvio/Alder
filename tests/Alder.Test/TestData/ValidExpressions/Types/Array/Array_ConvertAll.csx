int[] ints = new[] { 1, 2, 3 };
string[] strs = Array.ConvertAll(ints, x => x.ToString());
return strs[2];
