// Reads the Editor console directly (also entries logged before the CLI connected).
using System;
using System.Reflection;
using System.Text;
public static class ReadConsole {
  public static string Errors() {
    var asm = typeof(UnityEditor.Editor).Assembly;
    var le = asm.GetType("UnityEditor.LogEntries");
    var entryType = asm.GetType("UnityEditor.LogEntry");
    int count = (int)le.GetMethod("StartGettingEntries").Invoke(null, null);
    var sb = new StringBuilder();
    try {
      for (int i = 0; i < count; i++) {
        var e = Activator.CreateInstance(entryType);
        le.GetMethod("GetEntryInternal").Invoke(null, new object[] { i, e });
        int mode = (int)entryType.GetField("mode").GetValue(e);
        string msg = (string)entryType.GetField("message").GetValue(e);
        if ((mode & (1 | 2 | 16 | 256 | 2048 | 131072 | 1048576)) != 0 || msg.Contains("rror"))
          sb.AppendLine("- " + msg.Substring(0, Math.Min(msg.Length, 400)));
      }
    } finally { le.GetMethod("EndGettingEntries").Invoke(null, null); }
    return sb.Length == 0 ? "no errors" : sb.ToString();
  }
}
