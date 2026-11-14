namespace KiranaHub.DSA.Algorithms;

// =========================================================================================
// [KYA KARTA HAI]:
// MergeSort aur QuickSort algorithms ka implementation.
// KiranaHub inventory me hazaro products ko price ya expiry date ke according fast sort karne ke liye.
//
// [KYU ZAROORI HAI]:
// Bubble sort ya Selection sort O(N^2) time lete hain jo 10,000 items par bohot slow ho jate hain.
// MergeSort guaranteed O(N log N) stable sorting deta hai, aur QuickSort memory-efficient in-place sorting deta hai.
//
// [KAISE CHALTA HAI]:
// - MergeSort: Divide and Conquer (Array ko aadha karo, recursively sort karo, fir merge karo).
// - QuickSort: Pivot chuno, elements ko partition karo (chote left me, bade right me), aur recursively sort karo.
// =========================================================================================
public static class SortingAlgorithms
{
    // -------------------------------------------------------------------------------------
    // MERGE SORT: O(N log N) Stable Sort
    // -------------------------------------------------------------------------------------
    public static void MergeSort<T>(T[] array) where T : IComparable<T>
    {
        if (array.Length <= 1) return;
        T[] temp = new T[array.Length];
        MergeSortInternal(array, temp, 0, array.Length - 1);
    }

    private static void MergeSortInternal<T>(T[] array, T[] temp, int left, int right) where T : IComparable<T>
    {
        if (left >= right) return;

        int mid = left + (right - left) / 2;
        MergeSortInternal(array, temp, left, mid);
        MergeSortInternal(array, temp, mid + 1, right);
        Merge(array, temp, left, mid, right);
    }

    private static void Merge<T>(T[] array, T[] temp, int left, int mid, int right) where T : IComparable<T>
    {
        int i = left;
        int j = mid + 1;
        int k = left;

        while (i <= mid && j <= right)
        {
            if (array[i].CompareTo(array[j]) <= 0)
            {
                temp[k++] = array[i++];
            }
            else
            {
                temp[k++] = array[j++];
            }
        }

        while (i <= mid) temp[k++] = array[i++];
        while (j <= right) temp[k++] = array[j++];

        for (int m = left; m <= right; m++)
        {
            array[m] = temp[m];
        }
    }

    // -------------------------------------------------------------------------------------
    // QUICK SORT: O(N log N) Average In-Place Sort
    // -------------------------------------------------------------------------------------
    public static void QuickSort<T>(T[] array) where T : IComparable<T>
    {
        if (array.Length <= 1) return;
        QuickSortInternal(array, 0, array.Length - 1);
    }

    private static void QuickSortInternal<T>(T[] array, int low, int high) where T : IComparable<T>
    {
        if (low < high)
        {
            int pivotIndex = Partition(array, low, high);
            QuickSortInternal(array, low, pivotIndex - 1);
            QuickSortInternal(array, pivotIndex + 1, high);
        }
    }

    private static int Partition<T>(T[] array, int low, int high) where T : IComparable<T>
    {
        T pivot = array[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (array[j].CompareTo(pivot) <= 0)
            {
                i++;
                (array[i], array[j]) = (array[j], array[i]); // Swap
            }
        }

        (array[i + 1], array[high]) = (array[high], array[i + 1]);
        return i + 1;
    }
}

