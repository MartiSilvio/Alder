return new[] { 1, 2, 3 }.Select<int, string>(x => x.ToString()).First();
