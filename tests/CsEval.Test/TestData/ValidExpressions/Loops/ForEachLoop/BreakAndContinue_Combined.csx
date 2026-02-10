var sum = 0; foreach (var item in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }) { if (item % 2 == 0) { continue; } if (item > 10) { break; } sum = sum + item; } return sum;
