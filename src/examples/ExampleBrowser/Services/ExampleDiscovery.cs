using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VtkSharp.ExampleBrowser;

public static class ExampleDiscovery
{
    public static Dictionary<string, List<ExampleInfo>> DiscoverAll()
    {
        var examples = new Dictionary<string, List<ExampleInfo>>();

        var types = Assembly.GetExecutingAssembly().GetTypes();

        foreach (var type in types)
        {
            if (!typeof(IExample).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                continue;

            var attr = type.GetCustomAttribute<ExampleAttribute>();
            if (attr is null)
                continue;

            var info = new ExampleInfo
            {
                Name = attr.Name,
                Category = attr.Category,
                Description = attr.Description,
                ExampleType = type,
                SourceFiles = attr.SourceFiles ?? []
            };

            if (!examples.TryGetValue(info.Category, out var list))
            {
                list = [];
                examples[info.Category] = list;
            }

            list.Add(info);
        }

        return examples;
    }
}
