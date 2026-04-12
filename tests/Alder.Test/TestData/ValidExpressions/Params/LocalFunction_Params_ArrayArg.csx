// §15.6.2.6: passing an actual int[] is also legal for params int[]
int Sum(params int[] nums)
{
    int total = 0;
    for (int i = 0; i < nums.Length; i++) total += nums[i];
    return total;
}
int[] arr = new int[] { 10, 20, 30, 40 };
return Sum(arr);
