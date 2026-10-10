namespace PortailInterne.Api.Services;

/// Stockage des photos des employés, hors de wwwroot (jamais servies publiquement).
public class PhotoStorage
{
	private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
	private const long MaxSizeBytes = 2 * 1024 * 1024; // 2 Mo

	private readonly string _folder;
	
	public PhotoStorage(IConfiguration config, IWebHostEnvironment env)
	{
		var photoPath = config["App:PhotosPath"];
		if (string.IsNullOrWhiteSpace(photoPath))
            throw new InvalidOperationException("Le réglage App:PhotosPath est manquant dans appsettings.json.");
		
		_folder = Path.Combine(env.ContentRootPath, photoPath);
		Directory.CreateDirectory(_folder);
	}

    /// Renvoie un message d'erreur, ou null si la photo est valide.
	public string? Validate(IFormFile photo)
	{
		var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();

		if (!AllowedExtensions.Contains(ext))
			return "Format d'image non supporté (JPEG, PNG ou WebP).";

		if (photo.Length > MaxSizeBytes)
			return "L'image ne doit pas dépasser 2 Mo.";

		return null;
	}

    /// Enregistre la photo et renvoie le nom de fichier généré.
	public async Task<string> SaveAsync(IFormFile photo)
	{
		var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName).ToLowerInvariant()}";
		
		await using var stream = File.Create(Path.Combine(_folder, fileName));
        await photo.CopyToAsync(stream);
		
		return fileName;
	}

	/// Supprime une photo, si elle existe
	public void Delete(string? fileName)
	{
		var path = fileName is null ? null : GetPath(fileName);
		if (path is not null)
			File.Delete(path);
	}

    /// Chemin complet d'une photo, ou null si le nom est invalide ou si le fichier n'existe pas.
	public string? GetPath(string fileName)
	{
		// Le nom vient de l'URL : on n'accepte que des noms que nous avons généré nous-mêmes.
		var isSafeName =
			fileName == Path.GetFileName(fileName)
			&& Guid.TryParse(Path.GetFileNameWithoutExtension(fileName), out _)
			&& AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
		
		if (!isSafeName)
			return null;
		
		var path = Path.Combine(_folder, fileName);
		return File.Exists(path) ? path : null;
	}

	/// Adresse relative de la photo pour le frontend, ou null s'il n'y en a pas.
	public static string? UrlFor(string? fileName) =>
    	fileName is null ? null : $"/api/photos/{fileName}";
}