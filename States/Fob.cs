using Microsoft.Extensions.DependencyInjection;
using SimplySmart.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.States;

public interface IFob
{
    void Trigger(string button);
}

public class Fob : IFob
{
    private readonly IDictionary<string, string> buttonMapping = new Dictionary<string, string>();
    private IServiceProvider? serviceProvider;
    public void Initialise(IServiceProvider serviceProvider, List<FobButton> fobButtons)
    {
        this.serviceProvider = serviceProvider;

        foreach (var fobButton in fobButtons)
        {
            buttonMapping.Add(fobButton.name, fobButton.command);
        }
    }

    public void Trigger(string button)
    {
        if(serviceProvider == null)
        {
            throw new Exception("Fob class not initialised");
        }

        using var scope = serviceProvider.CreateScope();

        switch (button)
        {
            case "001":
                IAccessPointManager accessPointManager = scope.ServiceProvider.GetRequiredService<IAccessPointManager>();
                var garageDoor = (IGarageDoor)accessPointManager[buttonMapping[button]];
                garageDoor.Toggle();
                break;
            case "002":
                break;
            case "003":
                break;
            case "004":
                break;
            case "005":
                break;
            case "006":
                break;
            default:
                throw new Exception("Nonexistent fob button triggered");
        }
    }
}