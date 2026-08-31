namespace FastGeography.Server.Controllers;

using System.Security.Claims;

using FastGeography.Server.Services;
using FastGeography.Shared;
using FastGeography.Shared.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/rooms")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _rooms;

    public RoomsController(IRoomService rooms) => _rooms = rooms;

    [HttpPost]
    public IActionResult CreateRoom([FromQuery] string? lang)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? "Player";
        var language = GameLanguageExtensions.Parse(lang);
        var room = _rooms.CreateRoom(userId, displayName, language);
        return Ok(new CreateRoomResponse(room.Code, room.LanguageCode));
    }

    [HttpGet("{code}/exists")]
    [AllowAnonymous]
    public IActionResult RoomExists(string code)
    {
        var room = _rooms.GetRoom(code);
        return room is null ? NotFound() : Ok();
    }
}
