using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace SimplySmart.Core.YamlConfiguration;

internal class YamlConfigurationStreamParser
{
    readonly SortedDictionary<string, string> _data = new(StringComparer.OrdinalIgnoreCase);
    readonly Stack<string> _context = new();
    string? _currentPath;

    public IDictionary<string, string> Parse(Stream input)
    {
        _data.Clear();
        _context.Clear();

        // https://dotnetfiddle.net/rrR2Bb
        var yaml = new YamlStream();
        yaml.Load(new StreamReader(input, detectEncodingFromByteOrderMarks: true));

        if (yaml.Documents.Any())
        {
            var newRootNode = new YamlMappingNode
            {
                { "CONFIG_YAML", (YamlMappingNode)yaml.Documents[0].RootNode }
            };
            var mapping = newRootNode;

            // The document node is a mapping node
            VisitYamlMappingNode(mapping);
        }

        return _data;
    }

    void VisitYamlNodePair(KeyValuePair<YamlNode, YamlNode> yamlNodePair)
    {
        var context = ((YamlScalarNode)yamlNodePair.Key).Value;
        VisitYamlNode(context, yamlNodePair.Value);
    }

    void VisitYamlNode(string context, YamlNode node)
    {
        if (node is YamlScalarNode scalarNode)
        {
            VisitYamlScalarNode(context, scalarNode);
        }
        if (node is YamlMappingNode mappingNode)
        {
            VisitYamlMappingNode(context, mappingNode);
        }
        if (node is YamlSequenceNode sequenceNode)
        {
            VisitYamlSequenceNode(context, sequenceNode);
        }
    }

    void VisitYamlScalarNode(string context, YamlScalarNode yamlValue)
    {
        //a node with a single 1-1 mapping 
        EnterContext(context);
        var currentKey = _currentPath;

        _data[currentKey] = IsNullValue(yamlValue) ? null : yamlValue.Value;
        ExitContext();
    }

    void VisitYamlMappingNode(YamlMappingNode node)
    {
        foreach (var yamlNodePair in node.Children)
        {
            VisitYamlNodePair(yamlNodePair);
        }
    }

    void VisitYamlMappingNode(string context, YamlMappingNode yamlValue)
    {
        //a node with an associated sub-document
        EnterContext(context);

        VisitYamlMappingNode(yamlValue);

        ExitContext();
    }

    void VisitYamlSequenceNode(string context, YamlSequenceNode yamlValue)
    {
        //a node with an associated list
        EnterContext(context);

        VisitYamlSequenceNode(yamlValue);

        ExitContext();
    }

    void VisitYamlSequenceNode(YamlSequenceNode node)
    {
        for (int i = 0; i < node.Children.Count; i++)
        {
            VisitYamlNode(i.ToString(), node.Children[i]);
        }
    }

    void EnterContext(string context)
    {
        _context.Push(context);
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }

    void ExitContext()
    {
        _context.Pop();
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }

    static bool IsNullValue(YamlScalarNode yamlValue)
    {
        return yamlValue.Style == YamlDotNet.Core.ScalarStyle.Plain
            && (
                yamlValue.Value == "~"
                || yamlValue.Value == "null"
                || yamlValue.Value == "Null"
                || yamlValue.Value == "NULL"
            );
    }
}