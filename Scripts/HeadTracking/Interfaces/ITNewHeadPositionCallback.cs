using System;
using System.Runtime.InteropServices;

public delegate void TNewHeadPositionCallback(
    bool headDetected,
    bool imagePosIsValid,
    int imagePosX,
    int imagePosY,
    double worldPosX,
    double worldPosY,
    double worldPosZ
);

public interface ITNewHeadPositionCallback
{
    public void NewHeadPositionCallback(
        bool headDetected,
        bool imagePosIsValid,
        int imagePosX,
        int imagePosY,
        double worldPosX,
        double worldPosY,
        double worldPosZ
    );
}
