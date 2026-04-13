// Known limitation: `ref` arguments to method calls are not supported.
int[] arr = new[] { 1, 2, 3 };
Array.Resize(ref arr, 5);
return arr.Length;
