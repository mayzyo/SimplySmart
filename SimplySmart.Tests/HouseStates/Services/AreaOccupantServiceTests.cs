using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SimplySmart.Core.Models;
using SimplySmart.HouseStates.Areas;
using SimplySmart.HouseStates.Factories;
using SimplySmart.HouseStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Tests.HouseStates.Services;

public class AreaOccupantServiceTests
{
    readonly Mock<IOptions<ApplicationConfig>> optionsMock;
    readonly Mock<IAreaOccupantFactory> areaOccupantFactoryMock;
    readonly AreaOccupantService areaOccupantService;

    public AreaOccupantServiceTests()
    {
        optionsMock = new Mock<IOptions<ApplicationConfig>>();
        var loggerMock = new Mock<ILogger>();
        areaOccupantFactoryMock = new Mock<IAreaOccupantFactory>();
        areaOccupantService = new AreaOccupantService(optionsMock.Object, loggerMock.Object, areaOccupantFactoryMock.Object);
    }

    [Fact]
    public void Indexer_WithValidKey_ReturnsAreaOccupant()
    {
        // Arrange
        var key = "areaOccupant1";
        var camera = new Camera { Name = key };
        optionsMock.Setup(x => x.Value.Cameras).Returns([camera]);
        var areaOccupant = new Mock<IAreaOccupant>();
        areaOccupantFactoryMock.Setup(x => x.CreateAreaOccupant(camera)).Returns(areaOccupant.Object);

        // Act
        var result = areaOccupantService[key];

        // Assert
        Assert.Equal(areaOccupant.Object, result);
    }

    [Fact]
    public void Indexer_WithInvalidKey_ReturnsNull()
    {
        // Arrange
        var key = "invalidKey";
        optionsMock.Setup(x => x.Value.Cameras).Returns([]);

        // Act
        var result = areaOccupantService[key];

        // Assert
        Assert.Null(result);
    }
}
