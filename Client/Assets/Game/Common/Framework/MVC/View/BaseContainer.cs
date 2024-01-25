// author:蓝涣
// create time:20231225

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.MVC.View
{
    public abstract class BaseContainer: BasePanel
    {
        protected IList<BasePanel> _helpList;        // 避免重复创建List
        protected IList<BasePanel> _cachedViews;     // 容器内界面缓存
        protected IList<BasePanel> _openedFullViews; // 缓存打开的非pop界面（分开以减少遍历
        protected IList<BasePanel> _openedPopViews;  // 缓存打开的pop界面
        protected GameObject _rootGameObject;        // container容器对象的根节点
        protected int _sortingOrderPop = 0;          // pop界面的绘制排序顺序
        protected bool _showCamera;                  // 隐藏场景相机

        protected BaseContainer()
        {
            _cachedViews = new List<BasePanel>();
            _openedFullViews = new List<BasePanel>();
            _openedPopViews = new List<BasePanel>();
            _helpList = new List<BasePanel>();
        }

        //-------------------------------------------------------------------------------------------------------
        // override public function
        //-------------------------------------------------------------------------------------------------------
        public override bool SetVisible(bool isVisible)
        {
            bool tIsInvalid = _isDestroyed || _CheckInvalid(isVisible);
            if (ViewManager.logLevel >= 3)
                Logger.Log($"--- [BaseView] '_SetVisible'>>>{isVisible}---panel>>>{this.GetType().Name}---isInvalid>>>{tIsInvalid}");
            if (tIsInvalid) return false;

            // 根节点
            var tRoot = _GetRootObj();
            GameUtil.SetActive(tRoot, isVisible);
            // 子界面
            bool tHasFull = false;
            if (isVisible)
                tHasFull = _ReopenChild();
            else
            {
                foreach (var item in _openedPopViews)
                    item.SetVisible(false);
                foreach (var item in _openedFullViews)
                    item.SetVisible(false);
            }
            // 自身逻辑
            if (!tHasFull && _gameObject)
            {
                // 子section
                foreach (var item in _listSections)
                    item.SetVisible(isVisible);
                // 自身
                GameUtil.SetActive(_gameObject, isVisible);
                if (isVisible) _OnEnable();
                else _OnDisable();
            }

            return true;
        }

        //-------------------------------------------------------------------------------------------------------
        // public function（仅用于界面管理逻辑内部
        //-------------------------------------------------------------------------------------------------------

        // 获取容器根对象
        public GameObject _GetRootObj()
        {
            if (!_rootGameObject)
            {
                var tObj = new GameObject(this.GetType().Name, typeof(RectTransform));
                GameUtil.SetParent(tObj, ViewManager.UIRootMain, true, true);
                _rootGameObject = tObj;
            }

            return _rootGameObject;
        }

        // 返回当前打开的最上层界面
        public BasePanel _GetTopView()
        {
            int tLenght = _openedPopViews.Count;
            if (tLenght > 0) return _openedPopViews[tLenght - 1];

            tLenght = _openedFullViews.Count;
            return tLenght > 0 ? _openedFullViews[tLenght - 1] : this;
        }

        /// <summary>
        /// 返回当前打开的非popBaseView实例
        /// </summary>
        /// <param name="offset">往下层的偏移量,只为负数。默认0</param>
        public BasePanel _GetTopFullView(int offset = 0)
        {
            int tLenght = _openedFullViews.Count, tIndex;
            if (tLenght > 0)
            {
                tIndex = tLenght + offset - 1;
                if (tIndex >= 0 && tIndex < tLenght) return _openedFullViews[tIndex];
            }
            return this;
        }

        // 管理缓存子界面（添加或删除）
        public BasePanel _CacheViewItem(BasePanel srcInstance, bool isAdd)
        {
            int tIndex = _cachedViews.IndexOf(srcInstance);
            if (tIndex >= 0) // 先删除
            {
                _cachedViews.RemoveAt(tIndex);
            }
            if (isAdd) // 添加
                _cachedViews.Add(srcInstance);
            else
                srcInstance._SetOwnerContainer(null);
            // 缓存的未关闭界面
            bool tIsPop = srcInstance._IsPopPanel();
            var tList = tIsPop ? _openedPopViews : _openedFullViews;
            tIndex = tList.IndexOf(srcInstance);
            if (tIndex >= 0 && !tIsPop) tList.RemoveAt(tIndex);           // 先删除
            if (isAdd && (!tIsPop || tIndex < 0)) tList.Add(srcInstance); // 添加
            if (tIsPop) _sortingOrderPop = 10 * _openedPopViews.Count;

            return srcInstance;
        }

        /// <summary>
        /// 获取缓存的指定界面
        /// </summary>
        /// <param name="srcType">待创建的BasePanel类型</param>
        /// <param name="isAutoCreate">没有实例时是否自动创建</param>
        /// <param name="srcList">代操作的缓存列表。默认_calchedViews</param>
        /// <returns></returns>
        public BasePanel _GetViewItem(Type srcType, bool isAutoCreate, out bool isNew, IList<BasePanel> srcList = null)
        {
            isNew = false;
            if (srcList == null) srcList = _cachedViews;
            foreach (var item in srcList)
            {
                if (srcType == item.GetType()) return item;
            }
            if (isAutoCreate)
            {
                isNew = true;
                BasePanel tPanel = Activator.CreateInstance(srcType) as BasePanel;
                tPanel._SetOwnerContainer(this);
                return tPanel;
            }
            return null;
        }

        // 删除缓存的已打开界面
        public void _RemoveOpenedViewItem(BasePanel srcInstance)
        {
            IList<BasePanel> tList = srcInstance._IsPopPanel() ? _openedPopViews : _openedFullViews;
            int tIndex = tList.IndexOf(srcInstance);
            if (tIndex >= 0) tList.RemoveAt(tIndex);
        }

        // 设置一堆界面是否可见（一堆此处指一个非pop界面和在其之后打开的若干pop界面）
        // @params viewInstance 目标非pop界面。（本接口不验证参数传入的界面是否为非pop
        public void _SetGroupPanelVisible(BasePanel srcInstance, bool isVisible)
        {
            if (srcInstance != this)
            {// 存在其他全屏界面
                srcInstance.SetVisible(isVisible);
                foreach (var item in _GetDividedPopPanel(srcInstance))
                    item.SetVisible(isVisible);
            }
            else
            {
                // container本身只需处理子pop界面
                foreach (var item in _openedPopViews)
                    item.SetVisible(isVisible);
                // 自身逻辑
                if (_gameObject)
                {
                    // 子section
                    foreach (var item in _listSections)
                        item.SetVisible(isVisible);
                    // 自身
                    GameUtil.SetActive(_gameObject, isVisible);
                    if (isVisible) _OnEnable();
                    else _OnDisable();
                }
            }
        }

        // 获取缓存的界面
        public IList<BasePanel> _GetCachedViews()
        {
            return _cachedViews;
        }

        // 获取pop界面的排序顺序
        public int _GetPopSortingOrder(BasePanel srcInstance)
        {
            int tResult = _sortingOrderPop;
            if (srcInstance != null)
            {
                var tIndex = _openedPopViews.IndexOf(srcInstance);
                if (tIndex != -1) tResult = 2000 + (tIndex + 1) * 10; // 待优化
            }

            return tResult;
        }

        // 控制打开的子界面激活状态
        public void _ExitOpenedViewItems(bool isDestroy, bool isOnlyPop)
        {
            foreach (var item in _openedPopViews)
                item._DoExit(isDestroy, false);
            if (isOnlyPop) return;

            foreach (var item in _openedFullViews)
                item._DoExit(isDestroy, false);
        }

        //-------------------------------------------------------------------------------------------------------
        // override public function（仅用于界面管理逻辑内部
        //-------------------------------------------------------------------------------------------------------

        public override void _DoExit(bool isDestroy, bool isShowPre = true)
        {
            bool tDestroy = _isDestroyImmediately || (isDestroy && !_isPersistent);
            if(ViewManager.logLevel >= 1) Logger.Log($"--------- [BaseContainer] '_DoExit'>>>{this.GetType().Name}---dispose>>>{tDestroy}");
            Facade.RemoveView(this);
            // 清理子类
            _ExitOpenedViewItems(isDestroy, false);

            // 清理本身
            if (!SetVisible(false))
            {
                if (tDestroy && !_isDestroyed) _DestroyPrefab(true);
                return;
            }
            _OnPreExit();
            _OnExit(tDestroy);
            if (tDestroy && _gameObject) _DestroyPrefab(false);
            var tRootObj = _GetRootObj();
            if (tRootObj)
            {
                if (tDestroy) GameObject.Destroy(tRootObj);
                else GameUtil.SetActive(tRootObj, false);
            }
            if (isDestroy) ViewManager.GetInstance()._CachePanel(this, false);
        }

        //-------------------------------------------------------------------------------------------------------
        // override protected function
        //-------------------------------------------------------------------------------------------------------
        protected override GameObject _GetPrarentRoot()
        {
            return _GetRootObj();
        }
        protected override void _OpMainCamera()
        {
            GameUtil.SetMainCameraEnabled(_showCamera);
        }
        protected override void _OnPreShow()
        {
            _NotifyWhenShowOrExit(true);
            // 隐藏上一个container
            var tContainer = ViewManager.GetInstance()._GetPreviousContainer(this);
            if (tContainer != null) tContainer.SetVisible(false);
        }
        protected override void _OnPreExit(bool isShowPre = true)
        {
            _NotifyWhenShowOrExit(false);
        }

        //-------------------------------------------------------------------------------------------------------
        // protected function
        //-------------------------------------------------------------------------------------------------------

        // 重新打开子界面。返回是否有全屏子界面
        protected bool _ReopenChild()
        {
            bool tResult = false;
            // 非pop界面
            BasePanel tPanel = null;
            var tLength = _openedFullViews.Count;
            if (tLength > 0)
            {
                tPanel = _openedFullViews[tLength - 1];
                if(!tPanel.SetVisible(true)) tPanel._Reopen();
                tResult = true;
            }
            // pop界面
            var tList = _GetDividedPopPanel(tPanel);
            foreach (var item in tList)
            {
                if(!item.SetVisible(true)) tPanel._Reopen();
            }

            return tResult;
        }

        /// <summary>
        /// 获取被指定全屏界面分割的pop界面列表
        /// </summary>
        /// <param name="fullView">指定的全屏界面，可为空</param>
        protected IList<BasePanel> _GetDividedPopPanel(BasePanel fullView)
        {
            BasePanel tItem, tPopPanel = null;
            if (fullView == null || fullView == this) return _openedPopViews; // 只有pop界面
            int tLenght = _openedPopViews.Count;
            int tIndex = _cachedViews.IndexOf(fullView);
            _helpList.Clear(); // 先清理
            // 获取最上层待排除的pop界面
            for (int i = tIndex - 1; i >= 0; i--)
            {
                tItem = _cachedViews[i];
                if (tItem._IsPopPanel())
                {
                    tPopPanel = tItem;
                    break;
                }
            }
            // 填充结果
            tIndex = (tPopPanel != null) ? _openedPopViews.IndexOf(tPopPanel) : -1;
            for (int i = tIndex + 1; i < tLenght; i++)
            {
                _helpList.Add(_openedPopViews[i]);
            }

            return _helpList;
        }
    }
}
