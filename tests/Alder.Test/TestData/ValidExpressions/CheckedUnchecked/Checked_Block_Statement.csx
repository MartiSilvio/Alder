// §12.8.19: checked statement block scope
int a = 100;
int b = 200;
int result = 0;
checked
{
    result = a + b;
}
return result;
