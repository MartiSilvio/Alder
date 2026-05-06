// §12.8.16.3: object initializer nested inside collection initializer
var list = new List<System.Text.StringBuilder>
{
    new System.Text.StringBuilder { Capacity = 16 },
    new System.Text.StringBuilder { Capacity = 32 }
};
return list[1].Capacity;
