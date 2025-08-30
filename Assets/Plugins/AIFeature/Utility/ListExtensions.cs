public static class ListExtensions
{
    public static void Swap<T>(this System.Collections.Generic.List<T> list, int index1, int index2)
    {
        (list[index1], list[index2]) = (list[index2], list[index1]);
    }
}
