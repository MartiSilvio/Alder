{ object x = "hello"; return x switch { int i => i * 2, string s => s.Length, _ => -1 }; }
