namespace Code.Model
{
    public class Jitter
    {
        public enum JitterRate
        {
            Low,
            Medium,
            High
        }

        public readonly JitterRate Rate;
        public readonly long DelayMilliseconds;

        public Jitter(long delayMilliseconds)
        {
            Rate = delayMilliseconds switch
            {
                < 30 => JitterRate.Low,
                > 150 => JitterRate.High,
                _ => JitterRate.Medium
            };
            DelayMilliseconds = delayMilliseconds;
        }
    }
}