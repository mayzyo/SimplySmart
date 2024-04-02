using SimplySmart.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Services;

public interface IFob
{
    void Trigger(string button);
}

public class Fob : IFob
{
    readonly IAccessPointService accessPointService;
    readonly Dictionary<string, string> buttonMapping = [];
    
    public Fob(IAccessPointService accessPointService, IList<FobButton> fobButtons)
    {
        this.accessPointService = accessPointService;

        foreach (var fobButton in fobButtons)
        {
            buttonMapping.Add(fobButton.name, fobButton.command);
        }
    }

    public void Trigger(string button)
    {
        switch (button)
        {
            case "001":
                var garageDoor = (IGarageDoor)accessPointService[buttonMapping[button]];
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