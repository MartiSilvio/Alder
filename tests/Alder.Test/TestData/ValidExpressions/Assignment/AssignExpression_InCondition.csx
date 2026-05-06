// §12.21.2: assignment-as-expression used inside a while condition
int[] values = [3, 2, 1, 0, -1];
int i = 0;
int x = 1;
int sum = 0;
while ((x = values[i]) > 0)
{
    sum += x;
    i++;
}
return sum;
