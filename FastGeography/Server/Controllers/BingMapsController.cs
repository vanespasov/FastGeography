namespace FastGeography.Server.Controllers
{
    using BingMapsRESTToolkit;

    using Microsoft.AspNetCore.Mvc;



    [ApiController]
    [Route("bingmaps")]
    public class BingMapsController : ControllerBase
    {
        private string bingMapsKey = "AvgAK8EVgx50WkOB6cyA8ckUM5ku4U3kGJvxthKwE75_S4-c-XlTP82kUom8baQk";

        [HttpGet("{location}/{locationType}")]
        public async Task<IActionResult> GetLocationType(string location, string locationType)
        {
            // Create a geocode request
            var request = new GeocodeRequest()
            {
                Query = location,
                IncludeIso2 = true,
                MaxResults = 1,
                BingMapsKey = bingMapsKey
            };
            var response = request.Execute().Result;

            if (!(response != null && response.ResourceSets != null &&
                response.ResourceSets.Length > 0 &&
                response.ResourceSets[0].Resources != null &&
                response.ResourceSets[0].Resources.Length > 0))

                throw new Exception("No results found.");

            var resource = response.ResourceSets[0].Resources[0];

            // Get the location type (e.g. city, river, mountain)
            var result = response.ResourceSets[0].Resources[0] as Location;

            // Check if the location is a city
            if (LocationExists(result, locationType))
            {
                // add points for city.
                return Ok($"{locationType}:20");
            }
            else
            {
                //return fail response
                return Ok($"{locationType}:0");
            }

            return BadRequest();
        }

        private bool LocationExists(Location location, string locationType)
        {
            return location != null && location.EntityType == locationType;
        }
    }
}
