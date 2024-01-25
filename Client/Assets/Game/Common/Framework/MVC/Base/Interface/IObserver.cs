// author:蓝涣
// create time:20231225

namespace CNC.MVC.Base.Interfaces
{
    public interface IObserver
    {
        void NotifyObserver(string identifier);
        void NotifyObserver<T1>(string identifier, T1 arg1);
        void NotifyObserver<T1, T2>(string identifier, T1 arg1, T2 arg2);
        void NotifyObserver<T1, T2, T3>(string identifier, T1 arg1, T2 arg2, T3 arg3);
        bool IsEqual(object obj);
    }
}
