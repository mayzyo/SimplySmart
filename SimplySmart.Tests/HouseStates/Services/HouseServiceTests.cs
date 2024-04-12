using Moq;
using SimplySmart.HouseStates.Factories;
using SimplySmart.HouseStates.Features;
using SimplySmart.HouseStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.Tests.HouseStates.Services;

public class HouseServiceTests
{
    readonly Mock<IAutoLightFactory> autoLightFactoryMock;
    readonly Mock<IHouseSecurityFactory> houseSecurityFactoryMock;
    readonly HouseService houseService;

    public HouseServiceTests()
    {
        autoLightFactoryMock = new Mock<IAutoLightFactory>();
        houseSecurityFactoryMock = new Mock<IHouseSecurityFactory>();
        houseService = new HouseService(autoLightFactoryMock.Object, houseSecurityFactoryMock.Object);
    }

    [Fact]
    public void Security_ReturnsHouseSecurity()
    {
        // Arrange
        var houseSecurity = new Mock<IHouseSecurity>();
        houseSecurityFactoryMock.Setup(x => x.CreateHouseSecurity()).Returns(houseSecurity.Object);

        // Act
        var result = houseService.Security;

        // Assert
        Assert.Equal(houseSecurity.Object, result);
    }

    [Fact]
    public void AutoLight_ReturnsAutoLight()
    {
        // Arrange
        var autoLight = new Mock<IAutoLight>();
        autoLightFactoryMock.Setup(x => x.CreateAutoLight()).Returns(autoLight.Object);

        // Act
        var result = houseService.AutoLight;

        // Assert
        Assert.Equal(autoLight.Object, result);
    }

    [Fact]
    public async Task PublishAll_PublishesAllFeatures()
    {
        // Arrange
        var houseSecurity = new Mock<IHouseSecurity>();
        var autoLight = new Mock<IAutoLight>();
        houseSecurityFactoryMock.Setup(x => x.CreateHouseSecurity()).Returns(houseSecurity.Object);
        autoLightFactoryMock.Setup(x => x.CreateAutoLight()).Returns(autoLight.Object);

        // Act
        await houseService.PublishAll();

        // Assert
        houseSecurity.Verify(x => x.Publish(), Times.Once);
        autoLight.Verify(x => x.Publish(), Times.Once);
    }
}
