using System;

public class G3D_AlreadyInitializedException : Exception
{
    public G3D_AlreadyInitializedException()
    {
    }

    public G3D_AlreadyInitializedException(string message)
        : base(message)
    {
    }

    public G3D_AlreadyInitializedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}