int[] arr = new[] { 1, 2, 3, 4, 5, 6 };
int[] evens = Array.FindAll(arr, x => x % 2 == 0);
return evens.Length;
