using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UVtools.Core.Extensions;

public static class ObservableObjectExtensions
{
    extension(ObservableObject obj)
    {
        public bool ClearPropertyChangedHandlers()
        {
            var removed = false;
            var field = typeof(ObservableObject).GetField(
                "PropertyChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field is not null)
            {
                field.SetValue(obj, null);
                removed = true;
            }

            field = typeof(ObservableObject).GetField(
                "PropertyChanging",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field is not null)
            {
                field.SetValue(obj, null);
                removed = true;
            }

            return removed;
        }
    }
}