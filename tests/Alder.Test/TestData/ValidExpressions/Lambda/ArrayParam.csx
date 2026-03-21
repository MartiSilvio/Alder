Func<int[], int[]> reverse = (arr) => {
    var result = new int[arr.Length];
    for (var i = 0; i < arr.Length; i++)
        result[arr.Length - 1 - i] = arr[i];
    return result;
};
var data = new int[] { 1, 2, 3, 4, 5 };
var reversed = reverse(data);
reversed[0] + "," + reversed[1] + "," + reversed[2] + "," + reversed[3] + "," + reversed[4]
