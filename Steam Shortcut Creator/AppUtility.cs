using System.Text.RegularExpressions;

namespace SteamShortcutCreator;

public static partial class AppUtility
{
    #region Fields

    private static readonly char[] _invalidFileNameChars;

    #endregion

    #region Constructors

    static AppUtility()
    {
        _invalidFileNameChars = Path.GetInvalidFileNameChars();
    }

    #endregion

    #region Methods

    public static bool GetReachedEndOfStream(this BinaryReader reader)
    {
        return reader.BaseStream.Position >= reader.BaseStream.Length;
    }

    public static string SanitizePath(this string path)
    {
        path = path.Replace('/', '\\');
        path = MultiBackslashRegex().Replace(path, "\\");
        path = path.Trim();
        return path;
    }

    public static string SanitizeFileName(this string filename)
    {
        foreach (var invalidChar in _invalidFileNameChars)
        {
            filename = filename.Replace(invalidChar.ToString(), "");
        }

        return filename;
    }

    [GeneratedRegex(@"\\{2,}")]
    private static partial Regex MultiBackslashRegex();

    #endregion
}