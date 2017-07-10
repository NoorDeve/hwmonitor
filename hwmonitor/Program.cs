using System;
using System.IO;
using System.Threading;
using Amazon;
using Amazon.Kinesis.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
//using Amazon.Runtime.Internal.Auth;


class Program
{
    const string PrevReportFile = "HardwareMonitorReportPrev.txt";
    const string ReportFile = "HardwareMonitorReport.txt";
    /* time between we fetch new hardware reports, in miliseconds */
    const int ReportRate = 5000;
    static KinesisLog log = null;

    /*
     * Max length, in bytes, of the report file, before it is trimmed.
     *
     * We assume that we log around 256 bytes for each report line,
     * the formula below caclulates approx number of bytes needed to
     * store 3 hours of data.
     */
    const int MaxReportLength = 256 * (60/(ReportRate/1000)) * 60 * 3;


    static StreamWriter ReportStream;

    /*
     * Make sure we don't generate infinite large report file
     * by trimming away the oldest data when we hit a max length.
     *
     * We perform trimming by keeping around two report files.
     *
     * Current and Previous report. If Current goes over the size
     * limit, move Current to Previous, possibly overwriting the
     * old Previous.
     */
    static void TrimReportFile(string path)
    {
        if (!File.Exists(path))
        {
            /* report file does not exist, nothing to trim */
            return;
        }

        var len = new FileInfo(path).Length;
        if (len < MaxReportLength)
        {
            /* no trimming nessesary, do nothing */
            return;
        }

        /*
         * do the trimming operation
         */
        var DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var prev = Path.Combine(DesktopPath, PrevReportFile);

        /* delete old previous */
        if (File.Exists(prev))
        {
            File.Delete(prev);
        }
        File.Move(path, prev);
    }

    static void OpenReportStream()
    {
        var DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string path = Path.Combine(DesktopPath, ReportFile);
        TrimReportFile(path);
        ReportStream = new StreamWriter(path, true);

        WriteLine(
        "--------------------+-------------+---------------------------------------+-------------+-------------------------------+------------+--------------+-------------+-------------" + Environment.NewLine +
        "     Timestamp      |  M4ATX PSU  |           CPU Temperature             |     GPU     |           CPU Power           |  M4ATX PSU +  M4ATX PSU   +  M4ATX PSU  +  M4ATX PSU- " + Environment.NewLine +
        "  (UTC time zone)   | temperature |   PKG   Core0  Core1  Core02  Core03  | temperature |     PKG    Cores     DRAM     | Voltage IN + VoltageOn12V + VoltageOn3V + VoltageOn5V " + Environment.NewLine +
        "--------------------+-------------+---------------------------------------+-------------+-------------------------------+------------+--------------+-------------+-------------"
                  );
    }

    static void Initki()
    {

        Kinesis x;
        using (StreamReader r = new StreamReader("kinesisLog.json"))
        {
            string json = r.ReadToEnd();
            Console.WriteLine(json);
            x = JsonConvert.DeserializeObject<Kinesis>(json);
            
        }
        //Console.WriteLine(x.awsAccessKeyID);
        log = new KinesisLog(x.awsAccessKeyID, x.awsSecretAccessKey, RegionEndpoint.EUCentral1, "test", "PartitionKey");
        Console.WriteLine("Successfully connect to Kinesis ");


    }
    static void WriteLine(string line)
    {

        /* write report to console for debugging purpuses */
        Console.WriteLine(line);

        /* write report to the file */
        ReportStream.WriteLine(line);
        ReportStream.Flush();

        
    }



    static void LogException(Exception e)
    {
        WriteLine(e.ToString());
    }
   
    static void Init()
    {
        
        OpenReportStream();
        try
        {
            M4ATX.Init();
        }
        catch (Exception e)
        {
            /*
             * the M4ATX USB interface is a bit unstable,
             * handle the case we can't connect to it
             * by logging an error and continuing without it
             */
            LogException(e);
        }
        Motherboard.Init();
        Initki();


    }

    static void SendToKinesis(Record record)
    {
        /* send to kinesis stream */
        Object val;
        DataRecord dr = new DataRecord();
        val= record.Get(Record.DataPoint.CPUCore0Temperature);
        if(val!=null)
        {
            dr.CPUCore0Temperature = (float)val;
        }
        val= record.Get(Record.DataPoint.CPUCore1Temperature);
        if (val != null)
        {
            dr.CPUCore1Temperature = (float)val;
        }
        val= record.Get(Record.DataPoint.CPUCore2Temperature); 
        if(val!=null)
        {
            dr.CPUCore2Temperature = (float)val;
        }
        val= record.Get(Record.DataPoint.CPUCore3Temperature);
        if(val!=null)
        {
            dr.CPUCore3Temperature = (float)val;
        }
        val= record.Get(Record.DataPoint.CPUCoresPower);
        if(val!=null)
        {
            dr.CPUCoresPower = (float)val;
        }
       
        val = record.Get(Record.DataPoint.CPUDRAMPower);
        if (val != null)
        {
            dr.CPUDRAMPower = (float)val;
        }
        
        val= record.Get(Record.DataPoint.CPUPackagePower);
        if(val!=null)
        {
            dr.CPUPackagePower = (float)val;
        }
        
        val= record.Get(Record.DataPoint.CPUPackageTemperature);
        if(val!=null)
        {
            dr.CPUPackageTemperature = (float)val;
        }
        
        val= record.Get(Record.DataPoint.GPUCoreTemperature);
        if(val!=null)
        {
            dr.GPUCoreTemperature = (float)val;
        }

        val = record.Get(Record.DataPoint.M4ATXTemperature);
        if (val != null)
        {
            dr.M4ATXTemperature = (float)val;

        }

        val = record.Get(Record.DataPoint.M4ATXVoltageIn);
        if (val != null)
        {
            dr.M4ATXVoltageIn = (float)val;
        }


        string dataAsJson = JsonConvert.SerializeObject(dr);
        byte[] dataAsBytes = Encoding.UTF8.GetBytes(dataAsJson);

        using (MemoryStream memoryStream = new MemoryStream(dataAsBytes))
            try
            {
                PutRecordRequest requestRecord = new PutRecordRequest();
                requestRecord.StreamName = "test";
                requestRecord.PartitionKey = "PartitionKey";
                requestRecord.Data = memoryStream;
                PutRecordResponse responseRecord = log.KinesisClient.PutRecord(requestRecord);
                Console.WriteLine("Successfully sent record {0} to Kinesis. Sequence number: {1}", dr, responseRecord.SequenceNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send record {0} to Kinesis. Exception: {1}", dr, ex.Message);
            }

    }

    static void FetchAndLogRecord(Record record)
    {
        
        string line;
        try
        {
            M4ATX.Update(record);
        }
        catch (Exception e)
        {
            LogException(e);
        }

        Motherboard.Update(record);
        SendToKinesis(record);


        line = string.Format(
"{0} |     {1}\x00B0     |    {2}\x00B0    {3}\x00B0    {4}\x00B0    {5}\x00B0    {6}\x00B0    |     {7}\x00B0     |    {8,4:#0.0}W    {9,4:#0.0}W    {10,4:#0.0}W    |   {11,4:#0.0}V           {12,4:#0.0}V           {13,4:#0.0}V          {14,4:#0.0}V  ",

            DateTime.UtcNow,
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
            record.Get(Record.DataPoint.M4ATXVoltageIn),
            record.Get(Record.DataPoint.VoltageOn12V),
            record.Get(Record.DataPoint.VoltageOn3V),
            record.Get(Record.DataPoint.VoltageOn5V)
            );
        
        WriteLine(line);

    }

    static void Main(string[] args)
    {
        
        Init();
        
        /* reuse same record object to save a bit on GC */
        Record record = new Record();
        while (true)
        {
            try
            {
                FetchAndLogRecord(record);
            }
            catch (Exception e)
            {
                LogException(e);
            }
            Thread.Sleep(ReportRate);
        }
    }
}
