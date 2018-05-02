using System;

namespace PointCloud.Exceptions
{
    class IsNotDenseException : PointCloudException
    {
        public IsNotDenseException()
        {
        }

        public IsNotDenseException(String message) : base(message)
        {
        }
    }
}