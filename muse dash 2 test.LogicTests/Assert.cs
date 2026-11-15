namespace muse_dash_2_test.LogicTests
{
    internal sealed class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    // 외부 패키지 없이 돌아가는 최소 단언 헬퍼입니다.
    internal static class Assert
    {
        private const float FloatTolerance = 1e-4f;

        public static void True(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new AssertionException(message ?? "조건이 true가 아닙니다.");
            }
        }

        public static void False(bool condition, string message = null)
        {
            if (condition)
            {
                throw new AssertionException(message ?? "조건이 false가 아닙니다.");
            }
        }

        public static void NotNull(object value, string message = null)
        {
            if (value is null)
            {
                throw new AssertionException(message ?? "값이 null입니다.");
            }
        }

        public static void Null(object value, string message = null)
        {
            if (value is not null)
            {
                throw new AssertionException(message ?? $"null을 기대했지만 '{Describe(value)}' 입니다.");
            }
        }

        public static void Same(object expected, object actual, string message = null)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new AssertionException(message ?? "같은 인스턴스를 기대했지만 다른 인스턴스입니다.");
            }
        }

        public static void Equal(float expected, float actual, string message = null)
        {
            if (Math.Abs(expected - actual) > FloatTolerance)
            {
                throw new AssertionException(message ?? $"기대값 {expected}, 실제값 {actual}");
            }
        }

        public static void Equal(double expected, double actual, string message = null)
        {
            if (Math.Abs(expected - actual) > FloatTolerance)
            {
                throw new AssertionException(message ?? $"기대값 {expected}, 실제값 {actual}");
            }
        }

        public static void Equal<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertionException(message ?? $"기대값 '{Describe(expected)}', 실제값 '{Describe(actual)}'");
            }
        }

        public static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new AssertionException($"{typeof(TException).Name}을 기대했지만 {ex.GetType().Name}이 발생했습니다.");
            }

            throw new AssertionException($"{typeof(TException).Name}을 기대했지만 아무 예외도 발생하지 않았습니다.");
        }

        private static string Describe(object value) => value is null ? "null" : value.ToString();
    }
}
