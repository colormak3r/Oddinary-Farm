[System.Serializable]
public struct MinMaxInt
{
    public int min;
    public int max;

    public MinMaxInt(int min, int max)
    {
        this.min = min;
        this.max = max;
    }

    public int Value()
    {
        return UnityEngine.Random.Range(min, max);
    }
}
