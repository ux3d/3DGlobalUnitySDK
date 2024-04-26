
using System;

public class G3D_NotInitializedException : Exception
{
    public G3D_NotInitializedException()
    {
    }

    public G3D_NotInitializedException(string message)
        : base(message)
    {
    }

    public G3D_NotInitializedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}