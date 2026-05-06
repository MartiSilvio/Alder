// §11.2: list pattern with slice captured via var
int[] arr = { 1, 2, 3, 4 };
if (arr is [1, .. var rest])
    return rest.Length;
return -1;
