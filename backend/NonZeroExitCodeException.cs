namespace SomeDAO.Backend
{
	public class NonZeroExitCodeException : Exception
	{
		public NonZeroExitCodeException(long exitCode, string address, string methodName)
			: base($"Non-zero exit_code ({exitCode}) when calling method '{methodName}' on '{address}'")
		{
			// Nohting
		}
	}
}
