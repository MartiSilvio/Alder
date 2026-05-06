double amount = double.NaN;
double computed = 0.0 / 0.0;

bool nanAgainstZero =
    (amount < 0) == false &&
    (amount <= 0) == false &&
    (amount > 0) == false &&
    (amount >= 0) == false &&
    (amount == 0) == false &&
    (amount != 0) == true;

bool zeroAgainstNaN =
    (0 < amount) == false &&
    (0 <= amount) == false &&
    (0 > amount) == false &&
    (0 >= amount) == false &&
    (0 == amount) == false &&
    (0 != amount) == true;

bool nanAgainstNaN =
    (amount < amount) == false &&
    (amount <= amount) == false &&
    (amount > amount) == false &&
    (amount >= amount) == false &&
    (amount == amount) == false &&
    (amount != amount) == true;

return nanAgainstZero &&
       zeroAgainstNaN &&
       nanAgainstNaN &&
       (computed < 0) == false &&
       (computed != computed) == true;
