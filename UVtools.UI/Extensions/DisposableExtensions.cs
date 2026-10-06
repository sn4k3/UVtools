/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using Avalonia.Threading;

namespace UVtools.UI.Extensions;

public static class DisposableExtensions
{
    /// <summary>
    /// Disposes a resource that may still be referenced by a control, like a bitmap bound to an Image, after the UI
    /// had the chance to apply the property change and run the pending layout and render passes.
    /// </summary>
    public static void DisposeDeferred(this IDisposable? disposable)
    {
        if (disposable is null) return;
        Dispatcher.UIThread.Post(disposable.Dispose, DispatcherPriority.Background);
    }
}