using System;
using System.IO;
using System.Threading;


class Program
{
    const string ReportFile = "HardwareMonitorReport.txt";

    static StreamWriter ReportStream;

    /* time between we fetch new hardware reports, in miliseconds */
    const int ReportRate = 5000;

    static void OpenReportStream()
    {
        var DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string path = Path.Combine(DesktopPath, ReportFile);
        ReportStream = new StreamWriter(path, true);

        WriteLine(
        "  M4ATX PSU  |           CPU Temperature             |     GPU     |           CPU Power           |  M4ATX PSU" + Environment.NewLine +
        " temperature |   PKG   Core0  Core1  Core02  Core03  | temperature |    PKG    Cores      DRAM     | Voltage IN" + Environment.NewLine +
        "-------------+---------------------------------------+-------------+-------------------------------+------------"
                  );
    }

    static void WriteLine(string line)
    {
        /* write report to console for debugging purpuses */
        Console.WriteLine(line);

        /* write report to the file */
        ReportStream.WriteLine(line);
        ReportStream.Flush();
    }

    static void Init()
    {
        OpenReportStream();
        M4ATX.Init();
        Motherboard.Init();
    }

    static void Main(string[] args)
    {
        Init();

        string line;
        Record record = new Record();

        while (true)
        {
            M4ATX.Update(record);
            Motherboard.Update(record);

            line = string.Format(
"     {0}\x00B0     |    {1}\x00B0    {2}\x00B0    {3}\x00B0    {4}\x00B0    {5}\x00B0    |     {6}\x00B0     |    {7,4:#0.0}W    {8,4:#0.0}W    {9,4:#0.0}W    |   {10:#0.0}V",
                record.Get(Record.DataPoint.M4ATXTemperature),

                record.Get(Record.DataPoint.CPUPackageTemperature),
                record.Get(Record.DataPoint.CPUCore0Temperature),
                record.Get(Record.DataPoint.CPUCore1Temperature),
                record.Get(Record.DataPoint.CPUCore2Temperature),
                record.Get(Record.DataPoint.CPUCore3Temperature),

                record.Get(Record.DataPoint.GPUCoreTemperature),

                record.Get(Record.DataPoint.CPUPackagePower),
                record.Get(Record.DataPoint.CPUCoresPower),
                record.Get(Record.DataPoint.CPUDRAMPower),
                record.Get(Record.DataPoint.M4ATXVoltageIn)
                );

            WriteLine(line);
            Thread.Sleep(ReportRate);
        }
    }
}
