using System.Text.RegularExpressions;
using MiniPdm.Application.Import;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Desktop.Services;

public static partial class UiTextFormatter
{
    public static string FormatObjectType(
        ObjectType objectType)
    {
        return objectType switch
        {
            ObjectType.Assembly =>
                "Сборка",

            ObjectType.Part =>
                "Деталь",

            ObjectType.StandardPart =>
                "Стандартное изделие",

            _ =>
                objectType.ToString()
        };
    }

    public static string FormatVersionState(
        VersionState state)
    {
        return state switch
        {
            VersionState.InWork =>
                "В работе",

            VersionState.Approved =>
                "Утверждено",

            VersionState.Cancelled =>
                "Аннулировано",

            _ =>
                state.ToString()
        };
    }

    public static string FormatImportStatus(
        ImportItemStatus status)
    {
        return status switch
        {
            ImportItemStatus.Success =>
                "Принято",

            ImportItemStatus.Warning =>
                "Предупреждение",

            ImportItemStatus.Error =>
                "Ошибка",

            _ =>
                status.ToString()
        };
    }

    public static string FormatImportMessage(
        string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        if (string.Equals(
                message,
                "Imported successfully.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Файл принят.";
        }

        if (string.Equals(
                message,
                "Mass is not specified.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Не указана масса.";
        }

        if (string.Equals(
                message,
                "Invalid designation.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Некорректное обозначение.";
        }

        if (string.Equals(
                message,
                "File name is required.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Не указано имя файла.";
        }

        if (string.Equals(
                message,
                "Object name is required.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Не указано наименование объекта.";
        }

        if (string.Equals(
                message,
                "Mass must not be negative.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Масса не может быть отрицательной.";
        }

        if (string.Equals(
                message,
                "Assembly must not have material.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Для сборки материал указываться не должен.";
        }

        if (string.Equals(
                message,
                "Assembly mass must be calculated from BOM.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Масса сборки должна рассчитываться по составу.";
        }

        if (string.Equals(
                message,
                "Component file name is required.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Не указано имя файла компонента.";
        }

        if (string.Equals(
                message,
                "Material is required for part.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Для детали необходимо указать материал.";
        }

        if (string.Equals(
                message,
                "Part must not contain BOM components.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Деталь не должна содержать компоненты состава.";
        }

        if (string.Equals(
                message,
                "Standard part must not have a designation.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Стандартное изделие не должно иметь обозначение.";
        }

        if (string.Equals(
                message,
                "Mass is required for standard part.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Для стандартного изделия необходимо указать массу.";
        }

        if (string.Equals(
                message,
                "Standard part must not have material.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Для стандартного изделия материал указываться не должен.";
        }

        if (string.Equals(
                message,
                "Standard part must not contain BOM components.",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Стандартное изделие не должно содержать компоненты состава.";
        }

        var match =
            DuplicateDesignationRegex().Match(
                message);

        if (match.Success)
        {
            return
                $"Дублирующееся обозначение " +
                $"«{match.Groups[1].Value}».";
        }

        match =
            DuplicateStandardPartNameRegex().Match(
                message);

        if (match.Success)
        {
            return
                $"Дублирующееся наименование стандартного изделия " +
                $"«{match.Groups[1].Value}».";
        }

        match =
            InvalidQuantityRegex().Match(
                message);

        if (match.Success)
        {
            return
                $"Недопустимое количество компонента " +
                $"«{match.Groups[1].Value}»: " +
                $"{match.Groups[2].Value}.";
        }

        match =
            MissingComponentRegex().Match(
                message);

        if (match.Success)
        {
            return
                $"Ссылка на отсутствующий файл " +
                $"«{match.Groups[1].Value}».";
        }

        match =
            RejectedComponentRegex().Match(
                message);

        if (match.Success)
        {
            return
                $"Компонент «{match.Groups[1].Value}» отклонён.";
        }

        match =
            DuplicateBomComponentRegex().Match(
                message);

        if (match.Success)
        {
            return
                $"Компонент «{match.Groups[1].Value}» " +
                $"повторяется в составе.";
        }

        match =
            UnsupportedObjectTypeRegex().Match(
                message);

        if (match.Success)
        {
            return
                $"Неподдерживаемый тип объекта " +
                $"«{match.Groups[1].Value}».";
        }

        if (message.Contains(
                "JSON",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Некорректный JSON-файл.";
        }

        if (message.Contains(
                "Unsupported CAD format version",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Неподдерживаемая версия формата CAD.";
        }

        if (message.Contains(
                "does not contain a fileName",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "В CAD-файле отсутствует обязательное поле fileName.";
        }

        if (message.Contains(
                "does not match disk file name",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "Поле fileName не совпадает с именем файла на диске.";
        }

        if (message.Contains(
                "Unknown CAD object type",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Неизвестный тип CAD-объекта.";
        }

        if (message.Contains(
                "is empty or invalid",
                StringComparison.OrdinalIgnoreCase))
        {
            return "CAD-файл пуст или повреждён.";
        }

        return
            $"Ошибка обработки файла: {message}";
    }

    [GeneratedRegex(
        "^Duplicate designation '(.+)'\\.$",
        RegexOptions.IgnoreCase)]
    private static partial Regex DuplicateDesignationRegex();

    [GeneratedRegex(
        "^Duplicate standard part name '(.+)'\\.$",
        RegexOptions.IgnoreCase)]
    private static partial Regex DuplicateStandardPartNameRegex();

    [GeneratedRegex(
        "^Component '(.+)' has invalid quantity (-?\\d+)\\.$",
        RegexOptions.IgnoreCase)]
    private static partial Regex InvalidQuantityRegex();

    [GeneratedRegex(
        "^Referenced component '(.+)' was not found\\.$",
        RegexOptions.IgnoreCase)]
    private static partial Regex MissingComponentRegex();

    [GeneratedRegex(
        "^Referenced component '(.+)' was rejected\\.$",
        RegexOptions.IgnoreCase)]
    private static partial Regex RejectedComponentRegex();

    [GeneratedRegex(
        "^Component '(.+)' occurs more than once in BOM\\.$",
        RegexOptions.IgnoreCase)]
    private static partial Regex DuplicateBomComponentRegex();

    [GeneratedRegex(
        "^Unsupported object type '(.+)'\\.$",
        RegexOptions.IgnoreCase)]
    private static partial Regex UnsupportedObjectTypeRegex();
}