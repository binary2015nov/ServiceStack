using System;
using System.ComponentModel;

namespace Funq
{
    /// <summary>
    /// Helper interface used to hide the base <see cref="Object"/> 
    /// members from the fluent API to make for much cleaner 
    /// Visual Studio intellisense experience.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFluentInterface
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        Type GetType();

        [EditorBrowsable(EditorBrowsableState.Never)]
        int GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        string ToString();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Equals(object obj);
    }
}
