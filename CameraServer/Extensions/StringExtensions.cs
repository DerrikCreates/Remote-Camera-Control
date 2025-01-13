using System.Text;

namespace CameraServer.Extensions
{
    public static class StringExtensions
    {
        public static string GetFileSafeString(this string str)
        {
            StringBuilder sb = new(str);
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                sb.Replace(character, '-');
            }
            return sb.ToString();
        }
    }
}
