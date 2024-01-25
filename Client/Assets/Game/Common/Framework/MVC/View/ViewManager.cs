// author:蓝涣
// create time:20231225

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace CNC.MVC.View
{
    public class ViewManager
    {
        public static string panelOpen = "panelOpen"; // 打开界面事件
        public static string panelExit = "panelExit"; // 退出界面事件

        // 日志级别。0、关闭；1、界面打开、关闭信息；2、界面缓存信息；3、界面生命周期信息
        public static int logLevel = 3;

        protected IList<BaseContainer> _cachedContainers;     // 显示容器的缓存
        protected IList<BaseContainer> _openedContainerStack; // 当前打开的容器的堆栈

        // 互斥pop界面相关（优先显示优先级值小的界面
        protected ISet<BasePanel> _alternativeOpenedCache; // 已打开的互斥界面
        protected BasePanel _alternativeCurPanel;          // 当前打开的互斥界面
        protected ConcurrentDictionary<Type, int> _alternativeDicPops = new()
        {
            // [typeof(TestPopPanel)] = 1,
        };

        // 界面销毁相关
        protected ConcurrentDictionary<BasePanel, int> _dsCached; // 待销毁界面缓存
        protected int _dsLength = 6;    // 待销毁界面缓存最大个数
        protected int _dsFlagStart = 1; // 界面销毁权重

        protected static ViewManager _instance;
        public ViewManager()
        {
            if (_instance != null) throw new Exception("ViewManager Singleton already constructed!");
            _instance = this;
            _Init();
        }
        public static ViewManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ViewManager();
            }
            return _instance;
        }

        // 获取UI实例
        private BasePanel _DoShowPanelPre(Type classType, bool isContainer, out bool isNew)
        {
            return isContainer ? _PushContainer(classType, out isNew) : _GetRunningContainer()._GetViewItem(classType, true, out isNew);
        }

        // 打开界面具体逻辑。延时处理上一个界面
        private void _DoShowPanelEnd(BasePanel srcInstance, bool isContainer)
        {
            if (logLevel >= 1) Logger.Log($"------------------ [ViewManager] 'ShowPanel'>>>{srcInstance.GetType().Name}");
            if (isContainer)
            {
                srcInstance._DoShow();
                _ShowContainerLog(true);
            }
            else
            {
                // 互斥处理
                var tInnerPanel = _CheckAlternative(srcInstance, true);
                if (tInnerPanel == null)
                    srcInstance._DoShow();
                else
                {
                    if (_alternativeCurPanel == tInnerPanel) return;
                    if (_alternativeCurPanel != null) _alternativeCurPanel.SetVisible(false);
                    tInnerPanel._DoShow();
                    _alternativeCurPanel = tInnerPanel;
                }
            }
            // 销毁操作
            var tPanel = _CachePanel(srcInstance, true);
            if (tPanel != null) _DestroyPanel(tPanel, true);
        }

        //-------------------------------------------------------------------------------------------------------
        // public function
        //-------------------------------------------------------------------------------------------------------

        public void ShowPanel(Type classType)
        {
            bool tIsContainer = classType.IsSubclassOf(typeof(BaseContainer));
            BasePanel tPanel = _DoShowPanelPre(classType, tIsContainer, out var isNew);
            if(isNew) tPanel._OnAwake();
            _DoShowPanelEnd(tPanel, tIsContainer);
        }
        public void ShowPanel<T1>(Type classType, T1 arg1)
        {
            bool tIsContainer = classType.IsSubclassOf(typeof(BaseContainer));
            BasePanel tPanel = _DoShowPanelPre(classType, tIsContainer, out var isNew);
            if(isNew) tPanel._OnAwake(arg1);
            _DoShowPanelEnd(tPanel, tIsContainer);
        }
        public void ShowPanel<T1, T2>(Type classType, T1 arg1, T2 arg2)
        {
            bool tIsContainer = classType.IsSubclassOf(typeof(BaseContainer));
            BasePanel tPanel = _DoShowPanelPre(classType, tIsContainer, out var isNew);
            if(isNew) tPanel._OnAwake(arg1, arg2);
            _DoShowPanelEnd(tPanel, tIsContainer);
        }
        public void ShowPanel<T1, T2, T3>(Type classType, T1 arg1, T2 arg2, T3 arg3)
        {
            bool tIsContainer = classType.IsSubclassOf(typeof(BaseContainer));
            BasePanel tPanel = _DoShowPanelPre(classType, tIsContainer, out var isNew);
            if(isNew) tPanel._OnAwake(arg1, arg2, arg3);
            _DoShowPanelEnd(tPanel, tIsContainer);
        }

        public void ExitPanel(Type classType, bool isShowPre = true)
        {
            if (logLevel >= 1) Logger.Log($"------------------ [ViewManager] 'ExitPanel'>>>{classType.Name}");
            BasePanel tInstance = null;
            if (classType.IsSubclassOf(typeof(BaseContainer)))
            {
                var tLenght = _openedContainerStack.Count;
                if (tLenght < 1) return;
                var tIndex = _IndexOfContainer(_openedContainerStack, classType);
                if (tIndex == tLenght - 1)
                {// 当前界面
                    tInstance = _openedContainerStack[tLenght - 1];
                    _RemoveContainer(_openedContainerStack, tInstance as BaseContainer);
                    tInstance._DoExit(false);
                    // 显示上一个
                    if (isShowPre && tLenght > 1)
                    {
                        BaseContainer tPreCon = _openedContainerStack[tLenght - 2];
                        tPreCon.SetVisible(true);
                    }
                }
                else if(tIndex != -1)
                {
                    tInstance = _openedContainerStack[tIndex];
                    tInstance._DoExit(false);
                    _openedContainerStack.RemoveAt(tIndex);
                }
                _ShowContainerLog(true);
            }
            else
            {
                BaseContainer tContainer = _GetRunningContainer();
                tInstance = tContainer?._GetViewItem(classType, false, out var _);
                if (tInstance != null)
                {
                    tInstance._DoExit(false);
                    // 互斥处理
                    var tInnerPanel = _CheckAlternative(tInstance, false);
                    if (tInnerPanel != null)
                    {
                        tInnerPanel._DoShow();
                        _alternativeCurPanel = tInnerPanel;
                    }
                    if (tInstance == _alternativeCurPanel) _alternativeCurPanel = null;
                }
            }
            // 销毁操作
            if (tInstance != null && tInstance._GetFlagDispose()) _DestroyPanel(tInstance, false);
        }

        /// <summary>
        ///  获取当前最上层打开的界面
        /// </summary>
        /// <param name="isFilterPop">是否忽略pop界面。默认false</param>
        public BasePanel GetCurShownPanel(bool isFilterPop = false)
        {
            var tContainer = _GetRunningContainer();
            return !isFilterPop ? tContainer._GetTopView() : tContainer._GetTopFullView();
        }

        /// <summary>
        /// 退出最上层界面
        /// </summary>
        /// <param name="isContainer">是否为容器。单位容器时退出整个container。默认false</param>
        /// <param name="step">仅对 isContainer等于true时有用。表示回退几层container界面。目前仅支持0、-1、1，默认1
        /// <para>值为-1时，退到第一个Container并清空 </para>
        /// <para>值为0时，退到第一个Container </para>
        /// <para>值为1时，回退一层 </para>
        /// </param>
        public void ExitTopPanel(bool isContainer = false, int step = 1)
        {
            if (isContainer)
            {
                var tLength = _openedContainerStack.Count;
                if (tLength < 1) return;
                if (step == 1)
                {
                    if (tLength == 1) return;
                    ExitPanel(_openedContainerStack[tLength - 1].GetType());
                }
                else
                {
                    for (int i = tLength - 1; i > 0; i--)
                    {
                        var tContainer = _openedContainerStack[i];
                        ExitPanel(tContainer.GetType(), i == 1);
                    }
                    if (step == -1) _openedContainerStack[0]._ExitOpenedViewItems(false, false);
                }
            }
            else
                ExitPanel(GetCurShownPanel().GetType());
        }

        //-------------------------------------------------------------------------------------------------------
        // public function（仅用于界面管理逻辑内部
        //-------------------------------------------------------------------------------------------------------

        // 获取指定容器界面的上一个容器
        public BaseContainer _GetPreviousContainer(BaseContainer srcContainer)
        {
            int tIndex = _openedContainerStack.IndexOf(srcContainer);
            if (tIndex > 0) return _openedContainerStack[tIndex - 1];

            return null;
        }

        //-------------------------------------------------------------------------------------------------------
        // private function
        //-------------------------------------------------------------------------------------------------------

        // test
        public static GameObject UIRootMain;
        private void _Init()
        {
            UIRootMain = GameObject.Find("UIRoot/NormalLayer");
            _cachedContainers = new List<BaseContainer>();
            _openedContainerStack = new List<BaseContainer>();
            _alternativeOpenedCache = new HashSet<BasePanel>();
            _dsCached = new ConcurrentDictionary<BasePanel, int>();
        }

        private void _DestroyPanel(BasePanel srcInstance, bool callExit)
        {
            bool tIsContainer = srcInstance is BaseContainer;
            if (tIsContainer)
            {
                _RemoveContainer(_cachedContainers, srcInstance as BaseContainer);
                _RemoveContainer(_openedContainerStack, srcInstance as BaseContainer);
            }
            else srcInstance._GetOwnerContainer()._CacheViewItem(srcInstance, false);
            if (callExit) srcInstance._DoExit(true);

            _ShowContainerLog(true);
        }

        // 获取当前显示的容器界面
        private BaseContainer _GetRunningContainer()
        {
            BaseContainer tResult;
            int tLength = _openedContainerStack.Count;
            if (tLength > 0) return _openedContainerStack[tLength - 1];
            throw new Exception("----- [BaseContainer] '_GetRunningCOntainer' no running container>>>");
        }

        private int _IndexOfContainer(IList<BaseContainer> srcList, Type classType)
        {
            for (int i = 0; i < srcList.Count; i++)
            {
                if (srcList[i].GetType() == classType) return i;
            }
            return -1;
        }

        private BaseContainer _PushContainer(Type classType, out bool isNew)
        {
            BaseContainer tResult;
            isNew = false;
            var tIndex = _IndexOfContainer(_cachedContainers, classType);
            if (tIndex == _cachedContainers.Count) return null; // 当前正显示，避免重复调用
            // 处理缓存的container
            if (tIndex != -1) tResult = _cachedContainers[tIndex];
            else
            {
                tResult = Activator.CreateInstance(classType) as BaseContainer;
                isNew = true;
                _cachedContainers.Add(tResult);
            }
            // 处理当前打开的container
            _openedContainerStack.Remove(tResult);
            _openedContainerStack.Add(tResult);
            return tResult;
        }

        private void _RemoveContainer(IList<BaseContainer> srcList, BaseContainer srcInstance)
        {
            var tIndex = srcList.IndexOf(srcInstance);
            if(tIndex != -1) srcList.RemoveAt(tIndex);
        }

        private void _ShowContainerLog(bool isOpened)
        {
            if (logLevel < 1) return;
            var tList = isOpened ? _openedContainerStack : _cachedContainers;
            StringBuilder tBuilder = new StringBuilder();
            foreach (var item in tList)
            {
                tBuilder.Append(item.GetType().Name);
            }
            Logger.Log($"------- [ViewManager] {(isOpened ? "opening" : "opened")} container list>>>{tBuilder}");
        }

        //-------------------------------------------------------------------------------------------------------
        // 界面互斥相关
        //-------------------------------------------------------------------------------------------------------

        private BasePanel _CheckAlternative(BasePanel srcInatance, bool isAdd)
        {
            if (!srcInatance._IsPopPanel()) return null; // 当前只对pop界面生效
            var tType = srcInatance.GetType();
            BasePanel tPanel = null;
            if (_alternativeDicPops.TryGetValue(tType, out var value))
            {
                if (isAdd)
                    _alternativeOpenedCache.Add(srcInatance);
                else
                    _alternativeOpenedCache.Remove(srcInatance);
                // 优先显示优先级值小的界面
                int tMin = 0, tIndex = 0, tValue;
                foreach (var item in _alternativeOpenedCache)
                {
                    tValue = _alternativeDicPops[tType];
                    if (tValue < tMin || tIndex == 0)
                    {
                        tMin = tValue;
                        tPanel = item;
                    }
                    tIndex++;
                }
                return tPanel;
            }
            return null;
        }


        //-------------------------------------------------------------------------------------------------------
        // 界面销毁相关
        //-------------------------------------------------------------------------------------------------------

        public BasePanel _CachePanel(BasePanel srcInstance, bool isAdd)
        {
            if (!isAdd)
            {
                _dsCached.TryRemove(srcInstance, out _);
                return null;
            }
            if (srcInstance._CheckIgnoreCache()) return null;

            BasePanel tResult = null;
            _dsFlagStart++;
            if (_dsCached.ContainsKey(srcInstance)) // 已打开过
            {
                _dsCached[srcInstance] = _dsFlagStart;
                _CacheShowLog();
                return null;
            }

            if (_dsCached.Count >= _dsLength) tResult = _CacheFilter();
            _dsCached[srcInstance] = _dsFlagStart;
            _CacheShowLog();

            return tResult;
        }

        private BasePanel _CacheFilter()
        {
            BasePanel tResult = null, tView = null;
            int tIndex = 0, tMin = 0;
            foreach (var item in _dsCached)
            {
                if (item.Value < tMin || tIndex == 0)
                {
                    tMin = item.Value;
                    tView = item.Key;
                }
                tIndex++;
            }
            if(tView is BaseContainer) // 待销毁界面是容器
            {
                if (_GetRunningContainer() == tView)
                {// 命中当前打开的容器
                    _dsCached[tView] = ++_dsFlagStart;
                    tResult = _CacheFilter();
                }
                else
                {
                    // 优先找容器中打开的界面，没有则返回容器本身
                    var tList = (tView as BaseContainer)._GetCachedViews();
                    int tLength = tList.Count;
                    if (tLength < 1)
                        tResult = tView;
                    else
                    {
                        _dsCached[tView] = ++_dsFlagStart;
                        tResult = _CacheFilter();
                    }
                }
            }
            else
                tResult = tView;

            if (tResult == null) throw new Exception("call 'ViewManager._CacheFilter' error!");
            return tResult;
        }

        private void _CacheShowLog()
        {
            if (logLevel < 2) return;

        }
    }
}
