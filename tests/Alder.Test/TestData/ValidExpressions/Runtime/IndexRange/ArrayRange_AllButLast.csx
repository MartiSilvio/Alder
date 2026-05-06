// §12.8.11: [..^1] element access drops the last element of an array
int[] arr = new int[] { 10, 20, 30, 40 };
int[] slice = arr[..^1];
int total = 0;
for (int i = 0; i < slice.Length; i++) total += slice[i];
return total;
