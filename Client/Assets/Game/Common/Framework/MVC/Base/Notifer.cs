// author:蓝涣
// create time:20231225

using System;
using System.Collections.Concurrent;
using CNC.MVC.Base.Interfaces;

namespace CNC.MVC.Base
{
    public class Notifer: INotifier
    {
        public virtual ConcurrentDictionary<string, Delegate> ListNotification()
        {
            return null;
        }

        public void SendNotification(string identifier)
        {
            Facade.SendNotification(identifier);
        }
        public void SendNotification<T1>(string identifier, T1 arg1)
        {
            Facade.SendNotification(identifier, arg1);
        }

        public void SendNotification<T1, T2>(string identifier, T1 arg1, T2 arg2)
        {
            Facade.SendNotification(identifier, arg1, arg2);
        }

        public void SendNotification<T1, T2, T3>(string identifier, T1 arg1, T2 arg2, T3 arg3)
        {
            Facade.SendNotification(identifier, arg1, arg2, arg3);
        }

        protected Facade Facade
        {
            get
            {
                return Facade.GetInstance();
            }
        }
    }
}
