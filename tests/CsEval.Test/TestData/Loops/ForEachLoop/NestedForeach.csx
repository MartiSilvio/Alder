var total = 0; foreach (var i in new[] { 1, 2, 3 }) { foreach (var j in new[] { 10, 20, 30 }) { total = total + i * j; } } return total;
