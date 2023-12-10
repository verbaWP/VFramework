
namespace Framework.Interfaces
{
    public interface IObserver
    {
        void NotifyObserver(string identifier, Action param);
        bool IsEqual(object obj);

        Dictionary<string, Delegate> dicInterest { set; }
        object NotifyContext { set; }
    }
}
