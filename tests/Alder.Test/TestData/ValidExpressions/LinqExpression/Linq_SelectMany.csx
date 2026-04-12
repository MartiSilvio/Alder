// §12.8.9.3: extension method — SelectMany flattens nested collections
var lists = new List<List<int>>
{
    new List<int> { 1, 2 },
    new List<int> { 3, 4 },
    new List<int> { 5 }
};
return lists.SelectMany(l => l).Count();
