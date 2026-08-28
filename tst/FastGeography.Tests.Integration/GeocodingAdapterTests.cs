namespace FastGeography.IntegrationTests;

using FastGeography.Server.Services;
using FastGeography.Shared;

/// <summary>
/// Pure unit tests for the Nominatim and GeoNames type-matching logic.
/// These tests exercise <see cref="NominatimGeocodingService.LocationMatchesType"/> and
/// <see cref="GeoNamesGeocodingService.LocationMatchesType"/> without any HTTP calls or
/// WebApplicationFactory overhead.
/// </summary>
public sealed class NominatimTypeMatchingTests
{
    // ── City / Village ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("city",    LocationType.City)]
    [InlineData("town",    LocationType.City)]
    [InlineData("village", LocationType.City)]
    [InlineData("hamlet",  LocationType.City)]
    [InlineData("city",    LocationType.Village)]
    [InlineData("village", LocationType.Village)]
    [InlineData("suburb",  LocationType.Village)]
    public void PopulatedPlace_MatchesCityAndVillage(string addressType, LocationType locationType)
    {
        var result = MakeResult(addressType: addressType);
        Assert.True(NominatimGeocodingService.LocationMatchesType(result, locationType));
    }

    [Theory]
    [InlineData("country",  LocationType.City)]
    [InlineData("peak",     LocationType.Village)]
    [InlineData("river",    LocationType.City)]
    public void NonPopulatedPlace_DoesNotMatchCityOrVillage(string addressType, LocationType locationType)
    {
        var result = MakeResult(addressType: addressType);
        Assert.False(NominatimGeocodingService.LocationMatchesType(result, locationType));
    }

    // ── Country ───────────────────────────────────────────────────────────────

    [Fact]
    public void Country_AddressType_MatchesCountry()
    {
        var result = MakeResult(addressType: "country");
        Assert.True(NominatimGeocodingService.LocationMatchesType(result, LocationType.Country));
    }

    [Theory]
    [InlineData("city")]
    [InlineData("administrative")]
    public void NonCountry_DoesNotMatchCountry(string addressType)
    {
        var result = MakeResult(addressType: addressType);
        Assert.False(NominatimGeocodingService.LocationMatchesType(result, LocationType.Country));
    }

    // ── Mountain ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("peak")]
    [InlineData("ridge")]
    [InlineData("mountain_range")]
    [InlineData("hill")]
    public void NaturalPeak_MatchesMountain(string type)
    {
        var result = MakeResult(cls: "natural", type: type);
        Assert.True(NominatimGeocodingService.LocationMatchesType(result, LocationType.Mountain));
    }

    [Fact]
    public void Waterway_DoesNotMatchMountain()
    {
        var result = MakeResult(cls: "waterway", type: "river");
        Assert.False(NominatimGeocodingService.LocationMatchesType(result, LocationType.Mountain));
    }

    // ── River ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("river")]
    [InlineData("stream")]
    [InlineData("canal")]
    public void WaterwayRiver_MatchesRiver(string type)
    {
        var result = MakeResult(cls: "waterway", type: type);
        Assert.True(NominatimGeocodingService.LocationMatchesType(result, LocationType.River));
    }

    [Fact]
    public void NaturalPeak_DoesNotMatchRiver()
    {
        var result = MakeResult(cls: "natural", type: "peak");
        Assert.False(NominatimGeocodingService.LocationMatchesType(result, LocationType.River));
    }

    private static NominatimResult MakeResult(
        string? addressType = null, string? cls = null, string? type = null) =>
        new() { AddressType = addressType, Class = cls, Type = type, Lat = "0", Lon = "0" };
}

public sealed class GeoNamesTypeMatchingTests
{
    // ── City / Village ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("P", "PPLC")]   // capital city
    [InlineData("P", "PPLA")]   // seat of first-order admin division
    [InlineData("P", "PPL")]    // populated place
    [InlineData("P", "PPLH")]   // historical populated place
    public void PopulatedPlace_MatchesCityAndVillage(string fcl, string fcode)
    {
        var g = MakeGeoname(fcl, fcode);
        Assert.True(GeoNamesGeocodingService.LocationMatchesType(g, LocationType.City));
        Assert.True(GeoNamesGeocodingService.LocationMatchesType(g, LocationType.Village));
    }

    [Theory]
    [InlineData("A", "PCLI")]
    [InlineData("T", "MT")]
    [InlineData("H", "STM")]
    public void NonPopulatedPlace_DoesNotMatchCityOrVillage(string fcl, string fcode)
    {
        var g = MakeGeoname(fcl, fcode);
        Assert.False(GeoNamesGeocodingService.LocationMatchesType(g, LocationType.City));
        Assert.False(GeoNamesGeocodingService.LocationMatchesType(g, LocationType.Village));
    }

    // ── Country ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("A", "PCLI")]
    [InlineData("A", "PCLIX")]
    [InlineData("A", "PCLF")]
    [InlineData("A", "PCLD")]
    public void AdminPCLI_MatchesCountry(string fcl, string fcode)
    {
        var g = MakeGeoname(fcl, fcode);
        Assert.True(GeoNamesGeocodingService.LocationMatchesType(g, LocationType.Country));
    }

    [Theory]
    [InlineData("A", "ADM1")]   // admin division, not a country
    [InlineData("P", "PPLC")]   // city
    public void NonCountry_DoesNotMatchCountry(string fcl, string fcode)
    {
        var g = MakeGeoname(fcl, fcode);
        Assert.False(GeoNamesGeocodingService.LocationMatchesType(g, LocationType.Country));
    }

    // ── Mountain ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("T", "MT")]
    [InlineData("T", "MTS")]
    [InlineData("T", "PK")]
    [InlineData("T", "PKS")]
    [InlineData("T", "MNTN")]
    [InlineData("T", "HILL")]
    public void TerrainMountain_MatchesMountain(string fcl, string fcode)
    {
        var g = MakeGeoname(fcl, fcode);
        Assert.True(GeoNamesGeocodingService.LocationMatchesType(g, LocationType.Mountain));
    }

    [InlineData("T", "ISL")]    // island — terrain, not a mountain
    [Theory]
    public void TerrainNonMountain_DoesNotMatchMountain(string fcl, string fcode)
    {
        var g = MakeGeoname(fcl, fcode);
        Assert.False(GeoNamesGeocodingService.LocationMatchesType(g, LocationType.Mountain));
    }

    // ── River ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("H", "STM")]
    [InlineData("H", "STMI")]
    [InlineData("H", "STMS")]
    [InlineData("H", "RVN")]
    public void HydrographyStream_MatchesRiver(string fcl, string fcode)
    {
        var g = MakeGeoname(fcl, fcode);
        Assert.True(GeoNamesGeocodingService.LocationMatchesType(g, LocationType.River));
    }

    [Theory]
    [InlineData("H", "LK")]   // lake, not a river
    [InlineData("T", "MT")]
    public void NonRiver_DoesNotMatchRiver(string fcl, string fcode)
    {
        var g = MakeGeoname(fcl, fcode);
        Assert.False(GeoNamesGeocodingService.LocationMatchesType(g, LocationType.River));
    }

    private static GeoNamesGeoname MakeGeoname(string fcl, string fcode) =>
        new() { Fcl = fcl, Fcode = fcode, Lat = "0", Lng = "0" };
}
