using System;
using System.Runtime.InteropServices;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void TNewErrorMessageCallback(
    EMessageSeverity severity,
    byte[] sender,
    byte[] caption,
    byte[] cause,
    byte[] remedy,
    IntPtr listener
);

public interface ITNewErrorMessageCallback
{
    void NewErrorMessageCallback(
        EMessageSeverity severity,
        byte[] sender,
        byte[] caption,
        byte[] cause,
        byte[] remedy,
        IntPtr listener
    );
}
