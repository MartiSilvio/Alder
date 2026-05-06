var total = 0; foreach (var item in new[] { 1, 2, 3 }) { var count = 0; while (count < 3) { total = total + item; count = count + 1; } } return total;
