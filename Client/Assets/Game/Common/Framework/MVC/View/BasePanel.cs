// author:蓝涣
// create time:20231225

using System.Collections.Generic;
using UnityEngine;

namespace CNC.MVC.View
{
    public abstract class BasePanel: BaseView
    {
        protected bool _isPop;                // 标识是否为弹出界面(需在每个界面中设置该变量
        protected bool _popAutoClose;         // 作为弹出窗口是否自动关闭
        protected bool _isPersistent;         // 标识是否常驻不销毁（需配合BaseContainer一起使用
        protected bool _isDestroyImmediately; // 关闭界面是否立即销毁
        protected bool _isDestroyed;          // 标志界面是否已销毁

        protected BaseContainer _container;         // 作为子界面所依附的父界面（作为container时该属性为空
        protected IList<BaseSection> _listSections; // BaseSection

        protected BasePanel()
        {
            _listSections = new List<BaseSection>();
        }

        //-------------------------------------------------------------------------------------------------------
        // public function
        //-------------------------------------------------------------------------------------------------------

        public void Exit()
        {
            ViewManager.GetInstance().ExitPanel(GetType());
        }

        //-------------------------------------------------------------------------------------------------------
        // override public function
        //-------------------------------------------------------------------------------------------------------

        public override bool SetVisible(bool isVisible)
        {
            if (!base.SetVisible(isVisible)) return false;
            // 子section
            foreach (var item in _listSections)
                item.SetVisible(isVisible);
            // 自身
            if (isVisible) _OnEnable();
            else _OnDisable();

            return true;
        }

        //-------------------------------------------------------------------------------------------------------
        // public function（仅用于界面管理逻辑内部
        //-------------------------------------------------------------------------------------------------------

        // 显示界面
        public virtual void _DoShow()
        {
            Facade.RegisterView(this);
            _isDestroyed = false;
            _flagHide = false;
            if (_gameObject == null)
                _LoadPrefab();
            else
            {
                _OnPreShow();
                SetVisible(true);
                _OnShow();
            }
        }

        /// <summary>
        /// 关闭界面
        /// </summary>
        /// <param name="isDestroy">是否销毁</param>
        /// <param name="isShowPre">是否显示上个全屏界面。默认true，只对BasePanel有效</param>
        public virtual void _DoExit(bool isDestroy, bool isShowPre = true)
        {
            bool tDestroy = _isDestroyImmediately || (isDestroy && !_isPersistent);
            if(ViewManager.logLevel >= 1) Logger.Log($"------ [BasePanel] '_DoExit'>>>{this.GetType().Name}---dispose>>>{tDestroy}");
            Facade.RemoveView(this);
            // section处理
            foreach (var item in _listSections)
                item._DoExit(isDestroy);

            // 自身逻辑
            if (!SetVisible(false))
            {
                if (tDestroy && !_isDestroyed) _DestroyPrefab(true);
                return;
            }
            _OnPreExit(isShowPre);
            _OnExit(tDestroy);
            if (tDestroy && _gameObject) _DestroyPrefab(false);
            if (isDestroy) ViewManager.GetInstance()._CachePanel(this, false);
        }

        // 获取是否忽略界面销毁
        public bool _CheckIgnoreCache()
        {
            return _isPersistent || _isDestroyImmediately;
        }

        // 获取立即销毁标识
        public bool _GetFlagDispose()
        {
            return _isDestroyImmediately;
        }

        // 获取是不是pop界面
        public bool _IsPopPanel()
        {
            return _isPop;
        }

        // 重新打开本界面
        public void _Reopen()
        {
            // ViewManager.GetInstance().ShowPanel(this);
        }

        // 获取所属container界面
        public BaseContainer _GetOwnerContainer()
        {
            return _container;
        }

        // 设置所属BaseContainer界面
        public void _SetOwnerContainer(BaseContainer srcContainer)
        {
            if (srcContainer == _container) return;
            _container = srcContainer;
        }

        //-------------------------------------------------------------------------------------------------------
        // override protected function
        //-------------------------------------------------------------------------------------------------------

        protected override void _OnEnable()
        {
            base._OnEnable();
            _OpMainCamera();
        }

        protected override bool _OnLoadCompleteHandler(GameObject srcObj)
        {
            if (!base._OnLoadCompleteHandler(srcObj)) return false;
#if !RELEASE
            if (_isPop) _gameObject.name = $"{_gameObject.name}(pop)"; // rename
#endif
            _OnPreShow();
            _OnEnable();
            _OnShow();
            return true;
        }

        protected override GameObject _GetPrarentRoot()
        {
            return _container._GetRootObj();
        }
        //-------------------------------------------------------------------------------------------------------
        // protected function
        //-------------------------------------------------------------------------------------------------------

        protected void AddSection(BaseSection srcSection, GameObject rootParent)
        {
            if (_listSections.IndexOf(srcSection) != -1) return; // 避免重复添加
            srcSection._DoShow(rootParent);
            _listSections.Add(srcSection);
        }

        // 相机操作
        protected virtual void _OpMainCamera()
        {
            if(!_isPop) GameUtil.SetMainCameraEnabled(false);
        }

        // 界面打开时的一些逻辑处理
        protected virtual void _OnPreShow()
        {
            if (!_isPop)
            {
                // 隐藏上一个全屏界面
                var tPanel = _container._GetTopFullView();
                if (tPanel == this) tPanel = _container._GetTopFullView(-1);
                _container._CacheViewItem(this, true);
                _container._SetGroupPanelVisible(tPanel, false);
            }
            else
            {
                _container._CacheViewItem(this, true);
                GameUtil.AdjustPanelSorting(_gameObject, _container._GetPopSortingOrder(this));
            }
            _NotifyWhenShowOrExit(true);
        }

        /// 界面关闭时的一些逻辑处理
        /// <param name="isShowPre">是否显示上个全屏界面。默认true，只对BasePanel有效</param>
        protected virtual void _OnPreExit(bool isShowPre = true)
        {
            if (!_isPop)
            {
                BasePanel tPanel = _container._GetTopFullView();
                if (tPanel == this)
                {
                    _container._RemoveOpenedViewItem(this);
                    // 打开上一个全屏界面
                    if (isShowPre)
                    {
                        tPanel = _container._GetTopFullView();
                        if (tPanel != null)
                        {
                            _container._SetGroupPanelVisible(tPanel, true);
                        }
                    }
                }
                else
                    Logger.LogError("----- [BasePanel] '_OnPreExit' maybe has unexpect exception>>>");
            }
            else
                _container._RemoveOpenedViewItem(this);
            _NotifyWhenShowOrExit(false);
        }

        // 派发界面打开或关闭相关事件（异于onEnable方法
        protected void _NotifyWhenShowOrExit(bool isShow)
        {
            SendNotification(isShow ? ViewManager.panelOpen : ViewManager.panelExit, this);
        }
    }
}
