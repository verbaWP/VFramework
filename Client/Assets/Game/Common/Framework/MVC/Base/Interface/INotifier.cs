// author:蓝涣
// create time:20231225

using System;
using System.Collections.Concurrent;

namespace CNC.MVC.Base.Interfaces
{
    public interface INotifier
    {
        void SendNotification(string identifier);
        void SendNotification<T1>(string identifier, T1 arg1);
        void SendNotification<T1, T2>(string identifier, T1 arg1, T2 arg2);

        ConcurrentDictionary<string, Delegate> ListNotification();
    }
}
