namespace FastGeography.Server.Controllers
{
    using BingMapsRESTToolkit;
    using FastGeography.Shared;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("bingmaps")]
    public class BingMapsController : ControllerBase
    {
        private string bingMapsKey = "AvgAK8EVgx50WkOB6cyA8ckUM5ku4U3kGJvxthKwE75_S4-c-XlTP82kUom8baQk";

        [HttpGet("{location}/{locationType}")]
        public async Task<IActionResult> GetLocationType(string location, LocationType locationType)
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

            if (!IsValid(response))
            {
                //throw new Exception("No results found.");

                return Ok($"{locationType}:-5");
            }

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
                return Ok($"{locationType}:-5");
            }
        }

        private static bool IsValid(Response? response)
        {
            //TODO: use FluentValidation!!!
            return (response != null && response.ResourceSets != null &&
                            response.ResourceSets.Length > 0 &&
                            response.ResourceSets[0].Resources != null &&
                            response.ResourceSets[0].Resources.Length > 0);
        }

        private bool LocationExists(Location? location, LocationType locationType)
        {
            if (location == null)
                return false;

            switch (locationType)
            {
                case LocationType.City:
                    return location.EntityType.Contains("PopulatedPlace");
                case LocationType.Village:
                    return location.EntityType.Contains("PopulatedPlace");
                case LocationType.State:
                    return location.EntityType.Contains("CountryRegion") || location.EntityType.Contains("AdminDivision1");
                case LocationType.Mountain:
                    return location.EntityType.Contains("Mountain") || location.EntityType.Contains("MountainRange");
                case LocationType.River:
                    return location.EntityType.Contains("River");

                default:
                    return false;
            }

        }
    }
}
