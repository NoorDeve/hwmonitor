using System;
using System.Linq;

public class Record
{
    public enum DataPoint
    {
        M4ATXTemperature ,
        M4ATXVoltageIn,
        CPUCore0Temperature,
        CPUCore1Temperature,
        CPUCore2Temperature,
        CPUCore3Temperature,
        CPUPackageTemperature,
        GPUCoreTemperature,
        CPUPackagePower,
        CPUCoresPower,
        CPUDRAMPower,
        VoltageOn12V,
        VoltageOn3V,
        VoltageOn5V
    }

    /* this magic gives us number of enums defined */
    Object[] Values = new Object[Enum.GetValues(typeof(DataPoint)).Cast<int>().Max() + 1];

    public void Set(DataPoint Type, Object Value)
    {
        Values[(int)Type] = Value;
    }

    public Object Get(DataPoint Type)
    {
        return Values[(int)Type];
    }
}
