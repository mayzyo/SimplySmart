using SimplySmart.Core.Models;
using SimplySmart.DeviceStates.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplySmart.DeviceStates.Devices;

public interface IFob
{
    void Trigger(string button);
}

public class Fob : IFob
{
    readonly IGarageDoorService garageDoorService;
    readonly Dictionary<string, string> buttonMapping = [];

    public Fob(IGarageDoorService accessPointService, IList<FobButton> fobButtons)
    {
        this.garageDoorService = accessPointService;

        foreach (var fobButton in fobButtons)
        {
            buttonMapping.Add(fobButton.Name, fobButton.Command);
        }
    }

    public void Trigger(string button)
    {
        switch (button)
        {
            case "001":
                garageDoorService[buttonMapping[button]]?.Toggle();
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