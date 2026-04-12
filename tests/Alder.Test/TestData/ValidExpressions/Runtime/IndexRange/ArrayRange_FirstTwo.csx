// §12.8.11: arr[0..2] element access with a range selects the first two elements
int[] arr = new int[] { 5, 6, 7, 8, 9 };
int[] slice = arr[0..2];
return slice[0] + slice[1];
