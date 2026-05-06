var lastItem = 0; foreach (var item in new[] { 1, 2, 3, 4, 5 }) { lastItem = item; if (item == 3) { break; } } return lastItem;
