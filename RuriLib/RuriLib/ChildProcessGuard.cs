using System;
using System.Runtime.InteropServices;
using Diag = System.Diagnostics;

namespace RuriLib;

/// <summary>
/// Assigns child processes to a Windows Job Object with KILL_ON_JOB_CLOSE so they are
/// automatically terminated whenever SilverBullet exits — even via Task Manager or crash.
/// </summary>
internal static class ChildProcessGuard
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(
        IntPtr hJob,
        int    JobObjectInfoClass,
        ref    JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpInfo,
        int    cbInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long  PerProcessUserTimeLimit;
        public long  PerJobUserTimeLimit;
        public uint  LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint  ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint  PriorityClass;
        public uint  SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount;
        public ulong ReadTransferCount,  WriteTransferCount,  OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    private const uint   JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
    private const int    JobObjectExtendedLimitInformation   = 9;

    private static readonly IntPtr _job;

    static ChildProcessGuard()
    {
        _job = CreateJobObject(IntPtr.Zero, null);
        if (_job == IntPtr.Zero) return;

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
        SetInformationJobObject(_job, JobObjectExtendedLimitInformation,
            ref info, Marshal.SizeOf(info));
    }

    /// <summary>
    /// Call immediately after Process.Start() to enroll the process in the kill-on-close job.
    /// Safe to call on non-Windows (no-op).
    /// </summary>
    public static void Track(Diag.Process process)
    {
        if (_job == IntPtr.Zero || process == null) return;
        try { AssignProcessToJobObject(_job, process.Handle); }
        catch { /* ignore — process may have already exited */ }
    }
}
