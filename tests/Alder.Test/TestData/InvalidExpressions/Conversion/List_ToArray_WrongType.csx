// §10.2.8: int[] cannot implicitly convert to string[] (element types are unrelated)
string[] s = new List<int>().ToArray();
return s;
