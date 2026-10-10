using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/photos")]
public class PhotosController : ControllerBase
{
	private static readonly FileExtensionContentTypeProvider ContentTypes = new();

	private readonly PhotoStorage _photos;

	public PhotosController(PhotoStorage photos)
	{
		_photos = photos;
	}

	[HttpGet("{fileName}")]
	public IActionResult Get(string fileName)
	{
		var path = _photos.GetPath(fileName);
		if (path is null)
			return NotFound();

		if (!ContentTypes.TryGetContentType(path, out var contentType))
			contentType = "application/octet-stream";
		
		// Photos protégées : jamais gardées dans le cache du navigateur (poste partagé).
		// Le frontend les garde déjà en mémoire pendant la session (loadPhoto).
		Response.Headers.CacheControl = "no-store";

		return PhysicalFile(path, contentType);
	}
}