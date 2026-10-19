using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Xeonstrap;

/// <summary>
/// Flag packs are plain <c>ClientAppSettings.json</c> style files: one flat object that maps a
/// flag name to its value. Community packs contain thousands of flags, so instead of hard
/// coding them Xeonstrap reads every <c>*.json</c> from
/// <c>%LOCALAPPDATA%\Xeonstrap\FastFlagPacks</c> and offers it as one click in the FastFlags
/// page. A pack is applied on top of the current flags; nothing is removed.
/// </summary>
public static partial class FastFlagPacks
{
	public const string FolderName = "FastFlagPacks";

	/// <summary>Roblox rejects names that do not look like a flag, so they are skipped on apply.</summary>
	[GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{2,127}$", RegexOptions.CultureInvariant)]
	private static partial Regex ValidName { get; }

	public sealed record Pack(string Name, string FilePath, IReadOnlyDictionary<string, object> Flags)
	{
		public int Count => Flags.Count;

		public override string ToString() => Name + " (" + Flags.Count + " flags)";
	}

	public static string UserDirectory => Path.Combine(Paths.Config, FolderName);

	public static string EnsureDirectory()
	{
		Directory.CreateDirectory(UserDirectory);
		return UserDirectory;
	}

	public static IReadOnlyList<Pack> Load()
	{
		List<Pack> packs = [];
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

		foreach (string directory in new[] { EnsureDirectory(), BundledDirectory })
		{
			if (!Directory.Exists(directory))
				continue;

			foreach (string file in Directory.EnumerateFiles(directory, "*.json").OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
			{
				string name = Path.GetFileNameWithoutExtension(file);
				if (!seen.Add(name))
					continue;

				Pack? pack = TryLoadFile(file);
				if (pack != null)
					packs.Add(pack);
			}
		}

		return packs;
	}

	/// <summary>Optional folder next to the executable, so a release can ship packs.</summary>
	public static string BundledDirectory => Path.Combine(AppContext.BaseDirectory, FolderName);

	public static Pack? TryLoadFile(string filePath)
	{
		try
		{
			return LoadFile(filePath);
		}
		catch (Exception ex)
		{
			App.Logger?.WriteLine("FastFlagPacks::TryLoadFile", Path.GetFileName(filePath) + " could not be read: " + ex.Message);
			return null;
		}
	}

	/// <summary>
	/// Reads one pack. The returned dictionary keeps the file order and skips anything that is
	/// not a string, number or boolean, plus nulls and nested objects.
	/// </summary>
	public static Pack LoadFile(string filePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
		string text = File.ReadAllText(filePath);
		using JsonDocument document = JsonDocument.Parse(text, new JsonDocumentOptions
		{
			AllowTrailingCommas = true,
			CommentHandling = JsonCommentHandling.Skip
		});

		if (document.RootElement.ValueKind != JsonValueKind.Object)
			throw new InvalidDataException("a flag pack must be a JSON object of flag name to value");

		Dictionary<string, object> flags = new(StringComparer.Ordinal);
		foreach (JsonProperty property in document.RootElement.EnumerateObject())
		{
			if (!ValidName.IsMatch(property.Name))
				continue;

			if (TryReadValue(property.Value, out object value))
				flags[property.Name] = value;
		}

		if (flags.Count == 0)
			throw new InvalidDataException("the pack does not contain any usable flag");

		return new Pack(Path.GetFileNameWithoutExtension(filePath), filePath, flags);
	}

	private static bool TryReadValue(JsonElement element, out object value)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.String:
				value = element.GetString() ?? string.Empty;
				return true;
			case JsonValueKind.Number:
				if (element.TryGetInt64(out long integer))
					value = integer;
				else
					value = element.GetDouble();
				return true;
			case JsonValueKind.True:
				value = true;
				return true;
			case JsonValueKind.False:
				value = false;
				return true;
			default:
				value = string.Empty;
				return false;
		}
	}

	/// <summary>
	/// Writes every flag of the pack into the current profile and saves it. Returns the number
	/// of flags that were applied.
	/// </summary>
	public static int Apply(Pack pack)
	{
		ArgumentNullException.ThrowIfNull(pack);

		int applied = 0;
		foreach (KeyValuePair<string, object> flag in pack.Flags)
		{
			try
			{
				App.FastFlags.SetValue(flag.Key, flag.Value);
				applied++;
			}
			catch (Exception ex)
			{
				App.Logger?.WriteLine("FastFlagPacks::Apply", flag.Key + " was skipped: " + ex.Message);
			}
		}

		App.FastFlags.Save();
		App.Logger?.WriteLine("FastFlagPacks::Apply", "Applied " + applied + " flags from " + pack.Name);
		return applied;
	}

	/// <summary>Copies a pack into the user folder so it shows up in the picker.</summary>
	public static string Import(string sourcePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
		_ = LoadFile(sourcePath);

		string directory = EnsureDirectory();
		string destination = Path.Combine(directory, Path.GetFileName(sourcePath));
		if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
			File.Copy(sourcePath, destination, overwrite: true);

		App.Logger?.WriteLine("FastFlagPacks::Import", "Imported " + Path.GetFileName(destination));
		return destination;
	}
}
