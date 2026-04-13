// Known limitation: LINQ query syntax `let` clause is not supported — the parser only handles
// `from`, `where`, `select`, `orderby`, `group by`, and `join`.
var list = new List<int> { 1, 2, 3, 4, 5 };
var result = from x in list let doubled = x * 2 where doubled > 4 select doubled;
return result.Sum();
