// §13.14: using statement — disposes at end of block
bool disposed = false;
using (var e = new List<int> { 1, 2, 3 }.GetEnumerator())
{
    disposed = true;
}
return disposed;
