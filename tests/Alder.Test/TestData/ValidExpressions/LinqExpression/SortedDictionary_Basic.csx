var sd = new SortedDictionary<string, int>();
sd["banana"] = 2;
sd["apple"] = 1;
sd["cherry"] = 3;
return sd.Keys.First() + ":" + sd["apple"].ToString();
