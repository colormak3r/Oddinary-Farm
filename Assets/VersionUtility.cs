using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VersionUtility
{
    public static int MAJOR_VERSION = 0;
    public static int MINOR_VERSION = 3;
    public static int BUILD_VERSION = 9;
    public static string TEST_VERSION = "k";
    public static string VERSION => MAJOR_VERSION + "." + MINOR_VERSION + "." + BUILD_VERSION + TEST_VERSION;
}
