// §8.3.7: float precision loss; compare with tolerance
float sum = 0.1f + 0.2f;
return Math.Abs(sum - 0.3f) < 0.0001f;
