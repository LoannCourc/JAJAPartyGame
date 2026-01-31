// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("6ljb+OrX3NPwXJJcLdfb29vf2tl+JT4FKqOB7JYeT6/nm3xJmKx3GpuYIW/xaT6ZkIFjML8xNdADzDSxWNvV2upY29DYWNvb2nyHwakPuVwzupi/TegWQOP4xaFrZRVabC35Vhqn6KMPadK0ZYg1jWE1pmrE7MZ6XgbzVPmdOgKe9mHHXwf73wNnV0KC3iAScuAeLajIdr80FJ15xfnvJa2gZUQ17D87Ie9MSGFh+JehDL72vEdFlk/Z76BQHis+V2tPe+oC3B0l1F+6PjhUAZXxwSkyENAuH6eVFg2bZHOIMLrpCFUpOxqNxX9hw0q8FV6X9/MqaC+G6NNsUe8xYvtBHaHQfEyzZNfH05hkeQIlIoZoJUqxh3GQj6YwgzBTK9jZ29rb");
        private static int[] order = new int[] { 0,2,4,4,7,10,8,8,9,11,12,12,13,13,14 };
        private static int key = 218;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
