using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace Transport.WebApi.Models;

public class ApiConfiguration
{
  public string BaseUrl { get; set; } = string.Empty;
  public Dictionary<string, string> Endpoints { get; set; }

  public ApiConfiguration()
  {
    Endpoints = new Dictionary<string, string>();
    var controllers = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

    foreach (var controller in controllers)
    {
      var controllerName = controller.Name.Replace("Controller", "");

      var controllerRouteAttr = controller.GetCustomAttribute<RouteAttribute>();
      string controllerRoute;
      if (controllerRouteAttr != null && controllerRouteAttr.Template != null)
      {
        controllerRoute = controllerRouteAttr.Template.Replace("[controller]", controllerName);
      }
      else
      {
        controllerRoute = $"api/{controllerName}";
      }

      var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
          .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());

      foreach (var method in methods)
      {
        // Find the first HttpMethodAttribute (e.g., HttpGet, HttpPost, etc.)
        var httpMethodAttr = method.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault();
        string actionRoute = string.Empty;

        if (httpMethodAttr != null && httpMethodAttr.Template != null)
        {
          actionRoute = httpMethodAttr.Template.Replace("[action]", method.Name);
        }
        else
        {
          var actionRouteAttr = method.GetCustomAttribute<RouteAttribute>();
          if (actionRouteAttr != null && actionRouteAttr.Template != null)
          {
            actionRoute = actionRouteAttr.Template.Replace("[action]", method.Name);
          }
        }

        // If no route template, just leave actionRoute empty (so controllerRoute is used)
        var key = $"{controllerName}_{method.Name}";
        string route;

        if (!string.IsNullOrEmpty(actionRoute))
        {
          // Avoid double slashes
          if (controllerRoute.EndsWith("/"))
            route = $"{controllerRoute}{actionRoute}";
          else
            route = $"{controllerRoute}/{actionRoute}";
        }
        else
        {
          route = controllerRoute;
        }

        Endpoints[key] = route;
      }
    }
  }
}
