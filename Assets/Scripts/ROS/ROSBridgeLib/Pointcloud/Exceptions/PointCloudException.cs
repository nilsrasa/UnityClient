using System;

namespace PointCloud.Exceptions
{
    public class PointCloudException : Exception
    {
        public PointCloudException()
        {
        }

        public PointCloudException(String message) : base(message)
        {
        }
    }
}