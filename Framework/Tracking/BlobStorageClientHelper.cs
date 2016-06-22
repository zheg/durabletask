namespace DurableTask.Tracking
{
    using System;

    class BlobStorageClientHelper
    {
        static readonly string DateFormat = "yyyyMMdd";
        static readonly char ContainerDelimiter = '-';

        // the blob storage accesss key is in the format of {DateTime}|{bolbName}
        public static readonly char KeyDelimiter = '|';

        // the delimiter shown in the blob name as the file path
        public static readonly char BlobNameDelimiter = '/';

        public static string GetCurrentDateAsContainerSuffix()
        {
            return DateTime.UtcNow.ToString(DateFormat);
        }

        public static bool IsContainerExpired(string containerName, DateTime thresholdDateTimeUtc)
        {
            string[] segments = containerName.Split(ContainerDelimiter);
            if (segments.Length != 2)
            {
                throw new ArgumentException("container name does not contain required 2 segments.", nameof(containerName));
            }

            DateTime containerDateTime = DateTime.ParseExact(segments[1], DateFormat, System.Globalization.CultureInfo.InvariantCulture);
            return containerDateTime < thresholdDateTimeUtc;
        }

        public static string BuildContainerName(string prefix, string suffix)
        {
            return $"{prefix}{ContainerDelimiter}{suffix}";
        }
    }
}
