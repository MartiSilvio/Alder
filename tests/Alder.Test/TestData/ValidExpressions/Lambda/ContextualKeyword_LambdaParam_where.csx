// §6.4.4: 'where' as lambda parameter name
var words = new[] { "alpha", "beta", "gamma" };
return words.Select(where => where.Length).Sum();
