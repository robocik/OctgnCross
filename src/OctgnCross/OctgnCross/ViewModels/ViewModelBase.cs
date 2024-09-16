using System;
using System.Linq.Expressions;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Octgn.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
    {
        string propertyName = GetPropertyName(propertyExpression);
        if (!string.IsNullOrEmpty(propertyName))
        {
            OnPropertyChanged(propertyName);
        }
    }
    
    protected static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
    {
        if (propertyExpression == null)
        {
            throw new ArgumentNullException(nameof(propertyExpression));
        }

        return ((((propertyExpression.Body as MemberExpression) ?? throw new ArgumentException("Invalid argument", nameof(propertyExpression))).Member as PropertyInfo) ?? throw new ArgumentException("Argument is not a property", nameof(propertyExpression))).Name;
    }
}