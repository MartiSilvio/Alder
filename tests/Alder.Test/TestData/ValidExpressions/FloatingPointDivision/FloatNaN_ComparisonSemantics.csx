float amount = float.NaN;
float computed = 0.0f / 0.0f;

bool nanAgainstZero =
    (amount < 0f) == false &&
    (amount <= 0f) == false &&
    (amount > 0f) == false &&
    (amount >= 0f) == false &&
    (amount == 0f) == false &&
    (amount != 0f) == true;

bool zeroAgainstNaN =
    (0f < amount) == false &&
    (0f <= amount) == false &&
    (0f > amount) == false &&
    (0f >= amount) == false &&
    (0f == amount) == false &&
    (0f != amount) == true;

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
       (computed < 0f) == false &&
       (computed != computed) == true;
