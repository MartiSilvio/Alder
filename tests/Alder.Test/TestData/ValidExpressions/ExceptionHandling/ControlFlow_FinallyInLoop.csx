{ var x = 0; for (var i = 0; i < 3; i++) { try { x += i; } finally { x += 10; } } return x; }
