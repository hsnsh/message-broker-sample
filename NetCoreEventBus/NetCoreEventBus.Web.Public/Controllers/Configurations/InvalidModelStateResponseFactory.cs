using Microsoft.AspNetCore.Mvc;
using NetCoreEventBus.Web.Public.Extensions;
using NetCoreEventBus.Web.Public.Resources;

namespace NetCoreEventBus.Web.Public.Controllers.Configurations;

public static class InvalidModelStateResponseFactory
{
	public static IActionResult ProduceErrorResponse(ActionContext context)
	{
		var error = context.ModelState.GetErrorMessage();
		var response = new ErrorResource(message: error);

		return new BadRequestObjectResult(response);
	}
}