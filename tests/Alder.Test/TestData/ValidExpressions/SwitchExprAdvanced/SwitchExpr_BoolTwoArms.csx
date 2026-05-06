// §11.2: switch expression on bool with both arms
bool flag = true;
return flag switch
{
    true => "yes",
    false => "no"
};
