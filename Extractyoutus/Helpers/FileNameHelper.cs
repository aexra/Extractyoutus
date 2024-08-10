using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Extractyoutus.Helpers;
public static class FileNameHelper
{
    public static string MakeValidFileName(string input)
    {
        // Убираем недопустимые символы
        var invalidChars = new string(Path.GetInvalidFileNameChars());
        var invalidRegStr = string.Format("[{0}]", Regex.Escape(invalidChars));
        var validFileName = Regex.Replace(input, invalidRegStr, "");

        // Проверяем на зарезервированные имена
        string[] reservedFileNames = { "CON", "PRN", "AUX", "NUL",
                                       "COM1", "COM2", "COM3", "COM4",
                                       "COM5", "COM6", "COM7", "COM8",
                                       "COM9", "LPT1", "LPT2", "LPT3",
                                       "LPT4", "LPT5", "LPT6", "LPT7",
                                       "LPT8", "LPT9" };

        foreach (var reservedName in reservedFileNames)
        {
            if (validFileName.Equals(reservedName, StringComparison.OrdinalIgnoreCase))
            {
                validFileName = $"_{validFileName}_"; // Изменяем имя, чтобы избежать конфликта
                break;
            }
        }

        return validFileName;
    }
}
