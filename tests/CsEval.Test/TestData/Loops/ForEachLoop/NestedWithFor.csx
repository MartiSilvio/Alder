var total = 0; foreach (var item in new[] { 1, 2, 3 }) { for (var i = 0; i < 3; i = i + 1) { total = total + item; } } return total;
