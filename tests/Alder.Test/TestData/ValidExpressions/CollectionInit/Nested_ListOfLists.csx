// §12.8.16.4: nested collection initializer
var grid = new List<List<int>>
{
    new List<int> { 1, 2 },
    new List<int> { 3, 4 }
};
return grid[1][1];
