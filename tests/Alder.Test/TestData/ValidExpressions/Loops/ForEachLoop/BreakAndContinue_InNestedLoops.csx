var total = 0; foreach (var i in new[] { 1, 2, 3, 4, 5 }) { if (i == 3) { continue; } foreach (var j in new[] { 1, 2, 3, 4, 5 }) { if (j == 2) { break; } total = total + 1; } } return total;
