using System;

public delegate void TNewErrorMessageCallback(
    EMessageSeverity severity,
    string sender,
    string caption,
    string cause,
    string remedy
);

public interface ITNewErrorMessageCallback
{
    void NewErrorMessageCallback(
        EMessageSeverity severity,
        string sender,
        string caption,
        string cause,
        string remedy
    );
}
