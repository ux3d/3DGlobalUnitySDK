using System;

public class G3D_IndexOutOfRangeException : Exception
{
    public G3D_IndexOutOfRangeException()
    {
    }

    public G3D_IndexOutOfRangeException(string message)
        : base(message)
    {
    }

    public G3D_IndexOutOfRangeException(string message, Exception inner)
        : base(message, inner)
    {
    }
}