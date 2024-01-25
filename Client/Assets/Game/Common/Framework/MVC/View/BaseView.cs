// author:蓝涣
// create time:20231225

using CNC.Asset;
using CNC.MVC.Base;
using CNC.MVC.Base.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CNC.MVC.View
{

    /// <summary>
    /// 所有界面的基类。定义生命周期函数及通用功能
    /// <para>生命周期函数：</para>
    /// _OnAwake
    /// _OnInit
    /// _OnEnable
    /// _OnShow
    /// _OnDisable
    /// _OnExit
    /// </summary>
    public abstract class BaseView: Notifer, IView
    {
        protected GameObject _gameObject;// 每个界面对应的prefab
        protected bool _isDestroyed;     // 标志界面是否已销毁
        protected bool _isLoading;       // 标识当前界面prefab是否在加载中
        protected bool _flagHide;        // 仅用于prefab还没load完成就隐藏的界面
        protected string _prefabPathDynamic; // 动态prefab路径（同一个cs文件使用不同prefab
        protected AsyncOperationHandle<GameObject> _loadHandler; // 资源加载句柄

        //-------------------------------------------------------------------------------------------------------
        // public function
        //-------------------------------------------------------------------------------------------------------

        public bool IsDestroyed()
        {
            return _isDestroyed;
        }

        public virtual bool SetVisible(bool isVisible)
        {
            bool tIsInvalid = _isDestroyed || _CheckInvalid(isVisible);
            if (ViewManager.logLevel >= 3)
                Logger.Log($"--- [BaseView] '_SetVisible'>>>{isVisible}---panel>>>{this.GetType().Name}---isInvalid>>>{tIsInvalid}");
            if (tIsInvalid) return false;

            GameUtil.SetActive(_gameObject, isVisible);
            return true;
        }

        //-------------------------------------------------------------------------------------------------------
        // public function(仅用于界面管理逻辑内部
        //-------------------------------------------------------------------------------------------------------

        /// 返回当前界面是否可见
        /// <param name="isStrict">是否严格意义上的可见。默认true</param>
        public bool _IsVisible(bool isStrict = true)
        {
            return _gameObject && (isStrict ? _gameObject.activeInHierarchy : _gameObject.activeSelf);
        }

        //-------------------------------------------------------------------------------------------------------
        // protected function
        //-------------------------------------------------------------------------------------------------------

        public virtual void _OnAwake()
        {
            if (ViewManager.logLevel >= 3) Logger.Log($"--- [BaseView] '_OnAwake'>>>{this.GetType().Name}");
        }
        public virtual void _OnAwake<T1>(T1 arg1)
        {
            if (ViewManager.logLevel >= 3) Logger.Log($"--- [BaseView] '_OnAwake'>>>{this.GetType().Name}");
        }
        public virtual void _OnAwake<T1, T2>(T1 arg1, T2 arg2)
        {
            if (ViewManager.logLevel >= 3) Logger.Log($"--- [BaseView] '_OnAwake'>>>{this.GetType().Name}");
        }
        public virtual void _OnAwake<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            if (ViewManager.logLevel >= 3) Logger.Log($"--- [BaseView] '_OnAwake'>>>{this.GetType().Name}");
        }

        protected virtual void _OnInit()
        {
            if (ViewManager.logLevel >= 3)
                Logger.Log($"--- [BaseView] '_OnInit'>>>{this.GetType().Name}");
        }

        protected virtual void _OnEnable()
        {
            if (ViewManager.logLevel >= 3)
                Logger.Log($"--- [BaseView] '_OnEnable'>>>{this.GetType().Name}");
        }

        protected virtual void _OnShow()
        {
            if (ViewManager.logLevel >= 3)
                Logger.Log($"--- [BaseView] '_OnShow'>>>{this.GetType().Name}");
        }

        protected virtual void _OnDisable()
        {
            if (ViewManager.logLevel >= 3)
                Logger.Log($"--- [BaseView] '_OnDisable'>>>{this.GetType().Name}");
        }

        protected virtual void _OnExit(bool isDestroy)
        {
            if (ViewManager.logLevel >= 3)
                Logger.Log($"--- [BaseView] '_OnExit'>>>{this.GetType().Name}");
        }

        // 判断是否无效。界面prefab未加载完成就操作界面视为无效，仅setVisible方法内调用
        protected bool _CheckInvalid(bool isVisible)
        {
            if (_flagHide || _isLoading)
            {
                if (!isVisible) _flagHide = true;
                return true;
            }
            return false;
        }

        protected void _DestroyPrefab(bool isIgnoreInstance)
        {
            if (!isIgnoreInstance) GameObject.Destroy(_gameObject);
            Addressables.Release(_loadHandler);// 释放加载句柄
            _isDestroyed = true;
        }
        protected void _LoadPrefab()
        {
            if (_isLoading) return;
            _isLoading = true;

            var tPath = string.IsNullOrEmpty(_prefabPathDynamic) ? ViewPathConfig.dicPrefab[this.GetType().Name] : _prefabPathDynamic;
            _loadHandler = AssetManager.Instance.LoadAssetAsync<GameObject>($"Assets/AssetsPackage/UI/PrefabsRuntime/{tPath}", (srcObj) =>
            {
                bool tValue = srcObj != null;
                if (ViewManager.logLevel >= 3)
                    Logger.Log($"--- [BaseView] '_LoadPrefab' callback>>>{this.GetType().Name}---result>>>{tValue}---flag>>>{_flagHide}");

                if (tValue) _OnLoadCompleteHandler(srcObj);
            });
        }

        protected virtual bool _OnLoadCompleteHandler(GameObject srcObj)
        {
            _isLoading = false;
            _gameObject = GameObject.Instantiate(srcObj, _GetPrarentRoot().transform, false);
            _OnInit();
            if (_flagHide)
            {
                GameUtil.SetActive(_gameObject, false);
                _flagHide = false;
                return false;
            }
#if !RELEASE
            _gameObject.name = _gameObject.name.Replace("(Clone)", ""); // rename
#endif
            return true;
        }

        protected abstract GameObject _GetPrarentRoot();
    }
}
