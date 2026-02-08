{ var r = 0; try { throw new ArgumentException(); } catch { r = 1; } return r; }
