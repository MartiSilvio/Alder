var total = 0; foreach (var i in new[] { 1, 2, 3 }) { foreach (var j in new[] { 1, 2, 3, 4, 5 }) { if (j == 3) { continue; } total = total + 1; } } return total;
