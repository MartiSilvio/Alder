// §13.6.4: local function returning a Func<int,int> adder factory
Func<int, int> MakeAdder(int n) => x => x + n;
var addFive = MakeAdder(5);
return addFive(10);
