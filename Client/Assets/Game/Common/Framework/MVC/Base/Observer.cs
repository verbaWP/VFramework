// author:蓝涣
// create time:20231225

using System;
using System.Collections.Concurrent;

using CNC.MVC.Base.Interfaces;

namespace CNC.MVC.Base
{
    public class Observer: IObserver
    {
        public Observer(ConcurrentDictionary<string, Delegate> dicInterest, object notifyContext)
        {
            DicInterest = dicInterest;
            NotifyContext = notifyContext;
        }

        public void NotifyObserver(string identifier)
        {
            CNC.Logger.Log($"---- mvc test [Observer] NotifyObserver context>>>{NotifyContext.GetType().Name}---args>>>empty");
            Action tCallback = (Action)DicInterest[identifier];
            tCallback();
        }

        public void NotifyObserver<T1>(string identifier, T1 arg1)
        {
            CNC.Logger.Log($"---- mvc test [Observer] NotifyObserver context>>>{NotifyContext.GetType().Name}---args>>>[arg1:{arg1}]");
            Action<T1> tCallback = (Action<T1>)DicInterest[identifier];
            tCallback(arg1);
        }

        public void NotifyObserver<T1, T2>(string identifier, T1 arg1, T2 arg2)
        {
            CNC.Logger.Log($"---- mvc test [Observer] NotifyObserver context>>>{NotifyContext.GetType().Name}---args>>>[arg1:{arg1}, arg2:{arg2}]");
            Action<T1, T2> tCallback = (Action<T1, T2>)DicInterest[identifier];
            tCallback(arg1, arg2);
        }

        public void NotifyObserver<T1, T2, T3>(string identifier, T1 arg1, T2 arg2, T3 arg3)
        {
            CNC.Logger.Log($"---- mvc test [Observer] NotifyObserver context>>>{NotifyContext.GetType().Name}---args>>>[arg1:{arg1}, arg2:{arg2}, arg3:{arg3}]");
            Action<T1, T2, T3> tCallback = (Action<T1, T2, T3>)DicInterest[identifier];
            tCallback(arg1, arg2, arg3);
        }

        public bool IsEqual(object obj)
        {
            return NotifyContext.Equals(obj);
        }

        private ConcurrentDictionary<string, Delegate> DicInterest{ get; set; }
        private object NotifyContext { get; set; }
    }
}
