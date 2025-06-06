[System.Serializable]
public struct MinMaxFloat       // QUESTION: Is this your personal class? If so how did you make the UI in the editor?
{
    public float min;
    public float max;
    public float value => UnityEngine.Random.Range(min, max);
}
