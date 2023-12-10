
using Framework.Interfaces;

namespace Framework.Base
{
    public class Observer
    {
        public Observer(Dictionary<string, Delegate> dicInterest, object notifyContext)
        {
            DicInterest = dicInterest;
            NotifyContext = notifyContext;
        }

        public void NotifyObserver(string identifier)
        {
            Action tCallback = (Action)DicInterest[identifier];
            tCallback();
        }

        public void NotifyObserver<T1>(string identifier, T1 arg1)
        {
            Action<T1> tCallback = (Action<T1>)DicInterest[identifier];
            tCallback(arg1);
        }

        public void NotifyObserver<T1, T2>(string identifier, T1 arg1, T2 arg2)
        {
            Action<T1, T2> tCallback = (Action<T1, T2>)DicInterest[identifier];
            tCallback(arg1, arg2);
        }

        public bool IsEqual(object obj)
        {
            return NotifyContext.Equals(obj);
        }

        public Dictionary<string, Delegate> DicInterest{ get; private set; }
        public object NotifyContext { get; private set; }


        public void test(int arg1, string arg2){}

        public Dictionary<string, Delegate> _test()
        {
            return new Dictionary<string, Delegate>()
            {
                {"test", IsEqual},
                {"test", test},
            };
        }
    }
}
