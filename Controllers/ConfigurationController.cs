using Microsoft.AspNetCore.Mvc;
using Transport.WebApi.Models;

namespace Transport.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ConfigurationController : ControllerBase
{
  private readonly ILogger<ConfigurationController> _logger;
  private readonly IConfiguration _configuration;

  public ConfigurationController(ILogger<ConfigurationController> logger, IConfiguration configuration)
  {
    _logger = logger;
    _configuration = configuration;
  }

  /// <summary>
  /// Returns the current configuration settings of the API
  /// </summary>
  [HttpGet]
  [ProducesResponseType(200)]
  [ProducesResponseType(500)]
  public ActionResult<ApiConfiguration> GetApiConfiguration()
  {
    var config = new ApiConfiguration();
    _configuration.GetSection("ApiConfiguration").Bind(config);

    return Ok(config);
  }
}
