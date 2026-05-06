var arr = new int?[] {1, null, 3};
arr.Where(x => x != null).Select(x => x.Value).ToList()