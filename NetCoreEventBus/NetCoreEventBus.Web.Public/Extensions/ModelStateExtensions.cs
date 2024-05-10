using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NetCoreEventBus.Web.Public.Extensions;

public static class ModelStateExtensions
{
	public static string GetErrorMessage(this ModelStateDictionary dictionary)
	{
		var formattedError = dictionary
			.SelectMany(m => m.Value.Errors)
			.Select(m => m.ErrorMessage)
			.Aggregate((a, b) => string.Concat(a, " - ", b));

		return formattedError;
	}
}