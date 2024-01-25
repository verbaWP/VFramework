// author:蓝涣
// create time:20231225

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using CNC.MVC.Base;
using CNC.MVC.Base.Interfaces;

namespace CNC.MVC
{
    public class Facade
    {
        protected static Facade _instance;

        protected ConcurrentDictionary<string, IController> _dicController;
        protected ConcurrentDictionary<string, IModel> _dicModel;
        protected ConcurrentDictionary<string, IView> _dicView;
        protected ConcurrentDictionary<string, IList<IObserver>> _dicObserver;

        public Facade()
        {
            if (_instance != null) throw new Exception("Facade Singleton already constructed!");
            _instance = this;
            _Init();
        }

        public static Facade GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Facade();
            }
            return _instance;
        }

        // ----------------------------------------------------------------------------------------------------------
        //
        // public method
        //
        // ----------------------------------------------------------------------------------------------------------
        public void Reset()
        {
            foreach (var item in _dicModel)
            {
                item.Value.ResetData();
            }
            _Init();
        }

        public void SendNotification(string identifier)
        {
            if (!_dicObserver.TryGetValue(identifier, out var values)) return;
            foreach (var item in values)
            {
                item.NotifyObserver(identifier);
            }
        }

        public void SendNotification<T1>(string identifier, T1 arg1)
        {
            if (!_dicObserver.TryGetValue(identifier, out var values)) return;
            foreach (var item in values)
            {
                item.NotifyObserver(identifier, arg1);
            }
        }

        public void SendNotification<T1, T2>(string identifier, T1 arg1, T2 arg2)
        {
            if (!_dicObserver.TryGetValue(identifier, out var values)) return;
            foreach (var item in values)
            {
                item.NotifyObserver(identifier, arg1, arg2);
            }
        }

        public void SendNotification<T1, T2, T3>(string identifier, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!_dicObserver.TryGetValue(identifier, out var values)) return;
            foreach (var item in values)
            {
                item.NotifyObserver(identifier, arg1, arg2, arg3);
            }
        }

        // ----------------------------------------------------------------------------------------------------------
        // control
        // ----------------------------------------------------------------------------------------------------------
        public void RegisterController(IController sInstance)
        {
            if (_dicController.TryAdd(sInstance.GetType().Name, sInstance))
            {
                _RegisterObserver(sInstance);
                Logger.Log($"---------- mvc test [Facade] RegisterController>>>{sInstance.GetType().Name}---length>>>{_dicController.Count}");
            }
        }

        public void RemoveController(IController sInstance)
        {
            // if (sInstance == null) return;
            if(_dicController.TryRemove(sInstance.GetType().Name, out var value))
            {
                _RemoveObserver(value);
                Logger.Log($"---------- mvc test [Facade] RemoveController>>>{sInstance.GetType().Name}---length>>>{_dicController.Count}");
            }
        }

        // ----------------------------------------------------------------------------------------------------------
        // model
        // ----------------------------------------------------------------------------------------------------------
        public void RegisterModel(IModel sInstance)
        {
            if (_dicModel.TryAdd(sInstance.GetType().Name, sInstance))
            {
                _RegisterObserver(sInstance);
                Logger.Log($"---------- mvc test [Facade] RegisterModel>>>{sInstance.GetType().Name}---length>>>{_dicModel.Count}");
            }
        }

        public void RemoveModel(IModel sInstance)
        {
            // if (sInstance == null) return;
            if(_dicModel.TryRemove(sInstance.GetType().Name, out var value))
            {
                _RemoveObserver(value);
                Logger.Log($"---------- mvc test [Facade] RemoveModel>>>{sInstance.GetType().Name}---length>>>{_dicModel.Count}");
            }
        }

        public IModel GetModel(string sName)
        {
            if(_dicModel.TryGetValue(sName, out var value))
            {
                return value;
            }
            return null;
        }

        // ----------------------------------------------------------------------------------------------------------
        // view
        // ----------------------------------------------------------------------------------------------------------
        public void RegisterView(IView sInstance)
        {
            if (_dicView.TryAdd(sInstance.GetType().Name, sInstance))
            {
                _RegisterObserver(sInstance);
                Logger.Log($"---------- mvc test [Facade] RegisterView>>>{sInstance.GetType().Name}---length>>>{_dicView.Count}");
            }
        }

        public void RemoveView(IView sInstance)
        {
            // if (sInstance == null) return;
            if(_dicView.TryRemove(sInstance.GetType().Name, out var value))
            {
                _RemoveObserver(value);
                Logger.Log($"---------- mvc test [Facade] RemoveView>>>{sInstance.GetType().Name}---length>>>{_dicView.Count}");
            }
        }

        // ----------------------------------------------------------------------------------------------------------
        // private method
        // ----------------------------------------------------------------------------------------------------------
        private void _Init()
        {
            _dicController = new ConcurrentDictionary<string, IController>();
            _dicModel = new ConcurrentDictionary<string, IModel>();
            _dicView = new ConcurrentDictionary<string, IView>();
            _dicObserver = new ConcurrentDictionary<string, IList<IObserver>>();
        }

        private void _RegisterObserver(INotifier sInstance)
        {
            var tDic = sInstance.ListNotification();
            if (tDic != null && tDic.Count > 0)
            {
                IObserver tObserver = new Observer(tDic, sInstance);
                foreach (var item in tDic)
                {
                    if (_dicObserver.TryGetValue(item.Key, out var values))
                        values.Append(tObserver);
                    else
                        _dicObserver.TryAdd(item.Key, new List<IObserver>{tObserver});
                }
            }
        }

        private void _RemoveObserver(INotifier sInstance)
        {
            var tDic = sInstance.ListNotification();
            if (tDic == null) return;
            foreach (var item in tDic)
            {
                if (!_dicObserver.TryGetValue(item.Key, out var values)) continue;
                for(int i = 0; i < values.Count; i++)
                {
                    if (values[i].IsEqual(sInstance))
                    {
                        values.RemoveAt(i);
                        break;
                    }
                }
                // delete item that observer list length falls to zero
                if (values.Count == 0) _dicObserver.TryRemove(item.Key, out _);
            }
        }
    }
}
