using System;

namespace PointCloud.Exceptions
{
    class PCDReaderException : PointCloudException
    {
        public PCDReaderException()
        {
        }

        public PCDReaderException(String message) : base(message)
        {
        }
    }
}