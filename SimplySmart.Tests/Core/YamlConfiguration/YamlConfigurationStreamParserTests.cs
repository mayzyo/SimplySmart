using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimplySmart.Core.YamlConfiguration;

namespace SimplySmart.Tests.Core.YamlConfiguration;

public class YamlConfigurationStreamParserTests
{
    [Fact]
    public void Parse_ValidYamlStream_ReturnsExpectedData()
    {
        // Arrange
        var yamlContent = @"
                  Key1: Value1
                  Key2:
                    SubKey1: SubValue1
                    SubKey2: SubValue2
                  Key3:
                    - Item1
                    - Item2
            ";

        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(yamlContent);
        writer.Flush();
        stream.Position = 0;

        var parser = new YamlConfigurationStreamParser();

        // Act
        var result = parser.Parse(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Equal("Value1", result["CONFIG_YAML:Key1"]);
        Assert.Equal("SubValue1", result["CONFIG_YAML:Key2:SubKey1"]);
        Assert.Equal("SubValue2", result["CONFIG_YAML:Key2:SubKey2"]);
        Assert.Equal("Item1", result["CONFIG_YAML:Key3:0"]);
        Assert.Equal("Item2", result["CONFIG_YAML:Key3:1"]);
    }

    [Fact]
    public void Parse_EmptyYamlStream_ReturnsEmptyData()
    {
        // Arrange
        var stream = new MemoryStream();
        var parser = new YamlConfigurationStreamParser();

        // Act
        var result = parser.Parse(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_NullYamlValue_ReturnsNullValue()
    {
        // Arrange
        var yamlContent = @"
                  Key1: ~
                  Key2: null
                  Key3: Null
                  Key4: NULL
            ";

        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(yamlContent);
        writer.Flush();
        stream.Position = 0;

        var parser = new YamlConfigurationStreamParser();

        // Act
        var result = parser.Parse(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.Null(result["CONFIG_YAML:Key1"]);
        Assert.Null(result["CONFIG_YAML:Key2"]);
        Assert.Null(result["CONFIG_YAML:Key3"]);
        Assert.Null(result["CONFIG_YAML:Key4"]);
    }
}