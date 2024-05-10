namespace NetCoreEventBus.Web.Public.Resources;

public class ErrorResource
{
	public bool Success => false;
	public string Message { get; private set; }

	public ErrorResource(string message)
	{
		Message = message ?? string.Empty;
	}
}