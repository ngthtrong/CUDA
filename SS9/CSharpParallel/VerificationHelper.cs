namespace CSharpParallel
{
    /// <summary>
    /// Các phương thức kiểm tra kết quả đúng sai.
    /// </summary>
    static class VerificationHelper
    {
        public static string VerifyHistogram(int[] expected, int[] actual)
        {
            if (expected.Length != actual.Length) return "FAIL ✗ (length mismatch)";
            for (int i = 0; i < expected.Length; i++)
                if (expected[i] != actual[i])
                    return $"FAIL ✗ at index {i}: expected {expected[i]}, got {actual[i]}";
            return "PASS ✓";
        }

        public static string VerifyArray(int[] expected, int[] actual)
        {
            if (expected.Length != actual.Length) return "FAIL ✗ (length mismatch)";
            for (int i = 0; i < expected.Length; i++)
                if (expected[i] != actual[i])
                    return $"FAIL ✗ at index {i}: expected {expected[i]}, got {actual[i]}";
            return "PASS ✓";
        }
    }
}
