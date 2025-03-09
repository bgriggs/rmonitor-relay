using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using RedMist.Relay.ViewModels;
using System.Collections.Generic;

namespace RedMist.Relay.DataTemplateSelectors;

public class ControlLogSelector : IDataTemplate
{
    public bool SupportsRecycling => false;

    [Content]
    public Dictionary<string, IDataTemplate> Templates { get; } = [];

    public Control? Build(object? param)
    {
        if (param is ControlLogTypeViewModel vm)
        {
            if (string.IsNullOrEmpty(vm.Value))
                return new TextBlock { Text = "" };
            return Templates[((ControlLogTypeViewModel)param).Value].Build(param);
        }
        return new TextBlock { Text = "Unknown type" };
    }

    public bool Match(object? data)
    {
        return data is ControlLogTypeViewModel;
    }
}
