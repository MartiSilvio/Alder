int[] src = new[] { 1, 2, 3, 4, 5 };
int[] dst = new int[5];
Array.Copy(src, dst, 5);
return dst[2];
