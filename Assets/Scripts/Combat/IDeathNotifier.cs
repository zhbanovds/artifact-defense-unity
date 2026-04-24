using System;

public interface IDeathNotifier
{
    event Action Died;
}
