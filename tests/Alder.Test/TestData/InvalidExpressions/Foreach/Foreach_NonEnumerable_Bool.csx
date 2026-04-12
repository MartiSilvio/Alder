// §13.9.5: foreach requires IEnumerable — bool is not enumerable
bool b = true;
int count = 0;
foreach (var item in b)
    count++;
return count;
