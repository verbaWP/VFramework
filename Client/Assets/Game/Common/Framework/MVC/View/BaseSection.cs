// author:蓝涣
// create time:20231225

using UnityEngine;

namespace CNC.MVC.View
{
    public abstract class BaseSection: BaseView
    {
        protected GameObject _rootParent; // 父节点

        //-------------------------------------------------------------------------------------------------------
        // override public function
        //-------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 设置是否可见
        /// </summary>
        /// <param name="isVisible">是否可见</param>
        /// <returns>返回是否有效操作。未加载完成或已销毁为无效</returns>
        public override bool SetVisible(bool isVisible)
        {
            if (!base.SetVisible(isVisible)) return false;
            if (isVisible) _OnEnable();
            else _OnDisable();

            return true;
        }

        //-------------------------------------------------------------------------------------------------------
        // public function（仅用于界面管理逻辑内部
        //-------------------------------------------------------------------------------------------------------

        // 显示界面
        public void _DoShow(GameObject srcObj)
        {
            _rootParent = srcObj;
            Facade.RegisterView(this);
            _isDestroyed = false;
            _flagHide = false;
            if (_gameObject == null)
                _LoadPrefab();
            else
            {
                SetVisible(true);
                _OnShow();
            }
        }

        /// <summary>
        /// 关闭界面
        /// </summary>
        /// <param name="isDestroy">是否可见</param>
        /// <returns>返回是否有效操作。未加载完成或已销毁为无效</returns>
        public void _DoExit(bool isDestroy)
        {
            bool tDestroy = isDestroy;
            if(ViewManager.logLevel >= 1) Logger.Log($"------ [BaseSection] '_DoExit'>>>{this.GetType().Name}---dispose>>>{tDestroy}");
            Facade.RemoveView(this);
            if (!SetVisible(false))
            {
                if (tDestroy && !_isDestroyed) _DestroyPrefab(true);
                return;
            }
            _OnExit(tDestroy);
            if (tDestroy && _gameObject) _DestroyPrefab(false);
        }

        //-------------------------------------------------------------------------------------------------------
        // override protected function
        //-------------------------------------------------------------------------------------------------------
        protected override bool _OnLoadCompleteHandler(GameObject srcObj)
        {
            if (!base._OnLoadCompleteHandler(srcObj)) return false;
            _OnEnable();
            _OnShow();
            return true;
        }

        protected override GameObject _GetPrarentRoot()
        {
            return _rootParent;
        }
    }
}
