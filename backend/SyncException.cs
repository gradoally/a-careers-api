namespace SomeDAO.Backend
{
    public class SyncException : Exception
    {
        public SyncException(string? message)
            : base(message)
        {
            // Nothing
        }
    }
}
