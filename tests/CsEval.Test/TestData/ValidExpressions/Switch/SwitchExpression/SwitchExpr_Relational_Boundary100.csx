{ object x = 100; return x switch { > 100 => "high", > 50 => "medium", >= 0 => "low", _ => "negative" }; }
