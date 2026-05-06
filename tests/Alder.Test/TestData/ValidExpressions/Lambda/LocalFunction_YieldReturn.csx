IEnumerable<int> Gen() {
    yield return 1;
    yield return 2;
    yield return 3;
}
Gen().ToList()