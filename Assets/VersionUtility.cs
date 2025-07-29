/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/

public static class VersionUtility
{
    public static int MAJOR_VERSION = 0;
    public static int MINOR_VERSION = 4;
    public static int BUILD_VERSION = 0;
    public static string TEST_VERSION = "a";
    public static string VERSION => MAJOR_VERSION + "." + MINOR_VERSION + "." + BUILD_VERSION + TEST_VERSION;
}
