namespace RepeatList.Models
{
    public class CreditIsInsufficientError:Exception
    {
        public int ErrorCode { get; } = 777;

        public CreditIsInsufficientError(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
