using System;
using System.Collections.Generic;
using System.Text;

namespace Singingway.Windows.UiHelpers;
internal abstract class UiTab
{
    public abstract string Name { get; }
    public Action? OnConfigChanged { get; set; }

    public abstract void Initialize();
    public abstract void Draw();
}
