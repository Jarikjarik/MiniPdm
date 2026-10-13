using System.Globalization;
using System.Windows.Data;
using MiniPdm.Domain.Enums;

namespace MiniPdm.Desktop.Services;

public sealed class PdmEnumToRussianConverter
    : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value switch
        {
            ObjectType objectType =>
                UiTextFormatter.FormatObjectType(
                    objectType),

            VersionState state =>
                UiTextFormatter.FormatVersionState(
                    state),

            null =>
                string.Empty,

            _ =>
                value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}