using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkUnitTests
{
    public static class TestUtils
    {
        public static string GenerateRandomString(int length)
        {
            var result = new StringBuilder(length);
            while (result.Length < length)
            {
                // Use Guids so these don't compress well
                result.Append(Guid.NewGuid().ToString("N"));
            }

            return result.ToString(0, length);
        }
    }
}
