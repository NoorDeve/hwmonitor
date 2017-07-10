using Amazon;
using Amazon.Kinesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



   public class KinesisLog
    {

        public AmazonKinesisClient KinesisClient;
        string StreamName;
        string PartitionKey;
        public KinesisLog(string AWSAccessKeyId, string AWSSecretAccessKey, RegionEndpoint AWSRegion, string Stream, string Partition)
        {
            KinesisClient = new AmazonKinesisClient(AWSAccessKeyId, AWSSecretAccessKey, AWSRegion);
            StreamName = Stream;
            PartitionKey = Partition;
        }

    

}

