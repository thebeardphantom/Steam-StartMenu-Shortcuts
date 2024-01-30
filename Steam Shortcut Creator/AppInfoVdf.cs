﻿using System.Text;

namespace SteamShortcutCreator;

public class AppInfoVdf
{
    #region Types

    private enum ScannerState
    {
        LookingForAppInfo,
        LookingForClientIcon
    }

    #endregion

    #region Fields

    // appinfo
    private static readonly byte[] _appInfoHeader =
    {
        0x61,
        0x70,
        0x70,
        0x69,
        0x6E,
        0x66,
        0x6F,
        0x00,
        0x02,
        0x61,
        0x70,
        0x70,
        0x69,
        0x64,
        0x00
    };

    private static readonly byte[] _clientIconHeader =
    {
        0x01,
        0x63,
        0x6C,
        0x69,
        0x65,
        0x6E,
        0x74,
        0x69,
        0x63,
        0x6F,
        0x6E,
        0x00
    };

    private readonly Dictionary<int, string> _appIdToGuid = new();

    #endregion

    #region Constructors

    public AppInfoVdf(string path)
    {
        var state = ScannerState.LookingForAppInfo;
        var bytes = File.ReadAllBytes(path);
        var lastAppId = -1;
        using var memoryStream = new MemoryStream(bytes);
        using var reader = new BinaryReader(memoryStream);
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            /*
             * Always look for a new app info header there might not be a client icon,
             * so we need to see if we've entered a new app info section.
             */
            if (TryReadHeader(_appInfoHeader, reader))
            {
                if (state == ScannerState.LookingForClientIcon)
                {
                    Console.Error.WriteLine($"No client icon in section for appID {lastAppId}");
                }

                state = ScannerState.LookingForClientIcon;
                lastAppId = reader.ReadInt32();
                Console.WriteLine(@$"Found appinfo header for appID {lastAppId}.");
            }
            else if (state == ScannerState.LookingForClientIcon)
            {
                reader.BaseStream.Position--;
                if (TryReadHeader(_clientIconHeader, reader))
                {
                    var guidBytes = reader.ReadBytes(40);
                    var guid = Encoding.Default.GetString(guidBytes);
                    Console.WriteLine(@$"Found client icon guid {guid} for appID {lastAppId}.");
                    _appIdToGuid.Add(lastAppId, guid);
                    state = ScannerState.LookingForAppInfo;
                }
            }
        }
    }

    #endregion

    #region Methods

    public string? this[int appId] => _appIdToGuid.TryGetValue(appId, out var clientIconGuid) ? clientIconGuid : default;

    private static bool TryReadHeader(IReadOnlyList<byte> header, BinaryReader reader)
    {
        for (var i = 0; i < header.Count; i++)
        {
            var expectedByte = header[i];
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                return false;
            }

            if (reader.ReadByte() == expectedByte)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    #endregion
}